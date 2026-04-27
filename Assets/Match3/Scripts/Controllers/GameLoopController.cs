#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using Match3.Core;
using Match3.Core.Enums;
using Match3.Presenters;
using Match3.Services.Board;
using Match3.Services.Layer;
using Match3.Services.Level;
using Match3.Services.Swap;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Controllers
{
    public sealed class GameLoopController : IInitializable, IDisposable
    {
        private readonly BoardService          _boardService;
        private readonly SwapService           _swapService;
        private readonly LayerService          _layerService;
        private readonly LevelService          _levelService;
        private readonly BoardPresenter        _boardPresenter;
        private readonly LayerPresenter        _layerPresenter;
        private readonly BoardInputHandler     _inputHandler;
        private readonly LevelConfigRepository _levelRepository;

        private readonly CompositeDisposable     _disposables = new();
        private readonly CancellationTokenSource _cts         = new();

        [Inject]
        public GameLoopController(
            BoardService          boardService,
            SwapService           swapService,
            LayerService          layerService,
            LevelService          levelService,
            BoardPresenter        boardPresenter,
            LayerPresenter        layerPresenter,
            BoardInputHandler     inputHandler,
            LevelConfigRepository levelRepository)
        {
            _boardService    = boardService;
            _swapService     = swapService;
            _layerService    = layerService;
            _levelService    = levelService;
            _boardPresenter  = boardPresenter;
            _layerPresenter  = layerPresenter;
            _inputHandler    = inputHandler;
            _levelRepository = levelRepository;
        }

        // ── IInitializable ───────────────────────────────────────────────────

        void IInitializable.Initialize()
        {
            var config = _levelRepository.First
                ?? throw new InvalidOperationException("No level config found");

            _levelService.StartLevel(config);
            _boardPresenter.InitializeLayout();

            var initialGems = _boardService.GenerateInitialGems(config.AllowedNodeTypes);
            _boardPresenter.CreateGems(initialGems);

            _layerPresenter.RenderLayers(_boardService.Rows, _boardService.Columns);

            _inputHandler.OnCellClicked += OnCellClicked;

            _swapService.OnSwapRequested
                .Subscribe(swap => HandleSwapAsync(swap.from, swap.to, _cts.Token).Forget())
                .AddTo(_disposables);

            _inputHandler.SetInputEnabled(true);
            Debug.LogWarning("[GameLoop] Инициализирован. Доска готова.");
        }

        // ── Input ────────────────────────────────────────────────────────────

        private void OnCellClicked(Vector2Int pos)
        {
            Debug.LogWarning($"[GameLoop] Клик по ячейке: {pos}");
            _swapService.TrySelect(pos);
        }

        // ── Swap flow ────────────────────────────────────────────────────────

        private async UniTaskVoid HandleSwapAsync(Vector2Int from, Vector2Int to, CancellationToken ct)
        {
            Debug.LogWarning($"[GameLoop] HandleSwap start {from} → {to}");
            _swapService.Lock();
            _inputHandler.SetInputEnabled(false);

            try
            {
                var gemFrom = _boardService.GetGem(from);
                var gemTo   = _boardService.GetGem(to);

                if (gemFrom == null || gemTo == null)
                {
                    Debug.LogWarning($"[GameLoop] Gem is null: from={gemFrom} to={gemTo}");
                    return;
                }

                _boardService.ExchangeGems(from, to);
                await _boardPresenter.AnimateSwapAsync(from, to, gemFrom, gemTo, ct);

                _boardService.LockCell(from, false);
                _boardService.LockCell(to,   false);

                var matches = _boardService.FindAndCreateMatches(new[] { from, to });
                Debug.LogWarning($"[GameLoop] Матчей найдено: {matches.Count}");

                if (matches.Count == 0)
                {
                    var returnFrom = _boardService.GetGem(from)!;
                    var returnTo   = _boardService.GetGem(to)!;
                    _boardService.ExchangeGems(from, to);
                    await _boardPresenter.AnimateReturnSwapAsync(from, to, returnFrom, returnTo, ct);
                    Debug.LogWarning("[GameLoop] Матча нет — своп отменён");
                    return;
                }

                _levelService.UseMove();
                await ResolveAsync(matches, ct);
                _levelService.ProcessTurnResult();
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[GameLoop] HandleSwap exception: {e}");
            }
            finally
            {
                _swapService.Unlock();
                _inputHandler.SetInputEnabled(true);
                Debug.LogWarning("[GameLoop] HandleSwap завершён, ввод разблокирован");
            }
        }

        // ── Resolve loop ─────────────────────────────────────────────────────

        private async UniTask ResolveAsync(List<GemMatch> matches, CancellationToken ct)
        {
            while (matches.Count > 0)
            {
                Debug.LogWarning($"[GameLoop] ResolveAsync — {matches.Count} матч(ей)");

                // 1. Вычисляем форму каждого матча → определяем супер-фишки
                foreach (var match in matches)
                    match.ComputeSuperGem();

                // 2. Регистрируем в сервисах
                foreach (var match in matches)
                {
                    _levelService.RegisterMatch(match);
                    _layerService.ProcessMatches(match.MatchingCells);
                }

                // 3. Собираем взрывы от супер-фишек внутри матчей
                var explosionCells = CollectExplosionCells(matches);

                // 4. Уничтожаем матчи + взрывы параллельно
                var destroyTasks = new List<UniTask>(matches.Count + 1);
                foreach (var match in matches)
                    destroyTasks.Add(_boardPresenter.AnimateDestroyMatchAsync(match, ct));

                if (explosionCells.Count > 0)
                {
                    Debug.LogWarning($"[GameLoop] Взрывы супер-фишек: {explosionCells.Count} клеток");
                    destroyTasks.Add(_boardPresenter.AnimateDestroyCellsAsync(explosionCells, ct));
                }

                await UniTask.WhenAll(destroyTasks).AttachExternalCancellation(ct);

                // 5. Спавним супер-фишки на позиции матчей (до гравитации)
                SpawnSuperGems(matches);

                // 6. Гравитация + спавн обычных
                var fallMoves = _boardService.ComputeAndApplyFalls();
                Debug.LogWarning($"[GameLoop] Падений: {fallMoves.Count}");

                if (fallMoves.Count > 0)
                    await _boardPresenter.AnimateFallsAsync(fallMoves, ct);

                var spawnList = _boardService.GetSpawnList();
                Debug.LogWarning($"[GameLoop] Спавн: {spawnList.Count}");

                if (spawnList.Count > 0)
                    await _boardPresenter.AnimateSpawnAsync(spawnList, ct);

                // 7. Каскад
                var allCells = CollectAllNormalCells();
                matches = _boardService.FindAndCreateMatches(allCells);
                Debug.LogWarning($"[GameLoop] Каскадных матчей: {matches.Count}");
            }
        }

        // ── Super gem helpers ────────────────────────────────────────────────

        /// <summary>
        /// Собирает клетки взрывов от супер-фишек, которые попали в матчи.
        /// Исключает клетки уже входящие в матч (они и так уничтожатся).
        /// </summary>
        private List<Vector2Int> CollectExplosionCells(List<GemMatch> matches)
        {
            var alreadyInMatch = new HashSet<Vector2Int>();
            foreach (var match in matches)
                foreach (var cell in match.MatchingCells)
                    alreadyInMatch.Add(cell);

            var result = new HashSet<Vector2Int>();

            foreach (var match in matches)
            {
                foreach (var gem in match.MatchedGems)
                {
                    if (gem.SuperGemType == SuperGemType.None) continue;
                    var cells = GetExplosionCells(gem.CurrentIndex, gem.SuperGemType, gem.GemType);
                    foreach (var cell in cells)
                        if (!alreadyInMatch.Contains(cell))
                            result.Add(cell);
                }
            }

            return new List<Vector2Int>(result);
        }

        /// <summary>
        /// Возвращает список клеток которые должна уничтожить супер-фишка.
        /// </summary>
        private List<Vector2Int> GetExplosionCells(
            Vector2Int   pos,
            SuperGemType superGemType,
            NodeType     nodeType)
        {
            var cells = new List<Vector2Int>();

            switch (superGemType)
            {
                case SuperGemType.HorizontalArrow:
                    for (var col = 0; col < _boardService.Columns; col++)
                    {
                        var cell = new Vector2Int(pos.x, col);
                        if (_boardService.IsNormalCell(cell) && _boardService.GetGem(cell) != null)
                            cells.Add(cell);
                    }
                    Debug.LogWarning($"[GameLoop] HorizontalArrow взрыв: строка {pos.x}, {cells.Count} клеток");
                    break;

                case SuperGemType.VerticalArrow:
                    for (var row = 0; row < _boardService.Rows; row++)
                    {
                        var cell = new Vector2Int(row, pos.y);
                        if (_boardService.IsNormalCell(cell) && _boardService.GetGem(cell) != null)
                            cells.Add(cell);
                    }
                    Debug.LogWarning($"[GameLoop] VerticalArrow взрыв: колонка {pos.y}, {cells.Count} клеток");
                    break;

                case SuperGemType.ColorBomb:
                    for (var row = 0; row < _boardService.Rows; row++)
                    for (var col = 0; col < _boardService.Columns; col++)
                    {
                        var cell = new Vector2Int(row, col);
                        var gem  = _boardService.GetGem(cell);
                        if (gem != null && gem.GemType == nodeType)
                            cells.Add(cell);
                    }
                    Debug.LogWarning($"[GameLoop] ColorBomb взрыв: цвет {nodeType}, {cells.Count} клеток");
                    break;

                case SuperGemType.Bomb:
                    AddSquareCells(pos, radius: 1, cells);
                    Debug.LogWarning($"[GameLoop] Bomb взрыв: 3×3 вокруг {pos}, {cells.Count} клеток");
                    break;

                case SuperGemType.MegaBomb:
                    AddSquareCells(pos, radius: 2, cells);
                    Debug.LogWarning($"[GameLoop] MegaBomb взрыв: 5×5 вокруг {pos}, {cells.Count} клеток");
                    break;
            }

            return cells;
        }

        private void AddSquareCells(Vector2Int center, int radius, List<Vector2Int> cells)
        {
            for (var dr = -radius; dr <= radius; dr++)
            for (var dc = -radius; dc <= radius; dc++)
            {
                var cell = new Vector2Int(center.x + dr, center.y + dc);
                if (_boardService.IsNormalCell(cell) && _boardService.GetGem(cell) != null)
                    cells.Add(cell);
            }
        }

        /// <summary>
        /// Спавнит супер-фишки для всех матчей у которых есть HasSuperGemSpawn.
        /// Позиция должна быть пустой (гем уже уничтожен).
        /// </summary>
        private void SpawnSuperGems(List<GemMatch> matches)
        {
            foreach (var match in matches)
            {
                if (!match.HasSuperGemSpawn) continue;

                var pos = match.SuperGemSpawnPos;

                // Позиция должна быть пустой — если туда уже что-то упало, пропускаем
                if (!_boardService.IsNormalCell(pos) || _boardService.GetGem(pos) != null)
                {
                    Debug.LogWarning($"[GameLoop] Супер-фишка: позиция {pos} занята — пропускаем");
                    continue;
                }

                _boardPresenter.CreateSuperGemAt(pos, match.MatchNodeType, match.SuperGemToSpawn);
            }
        }

        // ── Utils ─────────────────────────────────────────────────────────────

        private List<Vector2Int> CollectAllNormalCells()
        {
            var result = new List<Vector2Int>(_boardService.Rows * _boardService.Columns);
            for (var row = 0; row < _boardService.Rows; row++)
            for (var col = 0; col < _boardService.Columns; col++)
            {
                var pos = new Vector2Int(row, col);
                if (_boardService.IsNormalCell(pos))
                    result.Add(pos);
            }
            return result;
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
        {
            _inputHandler.OnCellClicked -= OnCellClicked;
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
        }
    }
}
