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
using Match3.Services.Boost;
using Match3.Services.Hint;
using Match3.Services.Inventory;
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
        private readonly HintService           _hintService;
        private readonly BoostService          _boostService;
        private readonly InventoryService      _inventoryService;
        private readonly BoardPresenter        _boardPresenter;
        private readonly LayerPresenter        _layerPresenter;
        private readonly BoardInputHandler     _inputHandler;
        private readonly LevelConfigRepository _levelRepository;

        private readonly CompositeDisposable     _disposables = new();
        private readonly CancellationTokenSource _cts         = new();

        private GemView? _hintGemA;
        private GemView? _hintGemB;

        [Inject]
        public GameLoopController(
            BoardService          boardService,
            SwapService           swapService,
            LayerService          layerService,
            LevelService          levelService,
            HintService           hintService,
            BoostService          boostService,
            InventoryService      inventoryService,
            BoardPresenter        boardPresenter,
            LayerPresenter        layerPresenter,
            BoardInputHandler     inputHandler,
            LevelConfigRepository levelRepository)
        {
            _boardService     = boardService;
            _swapService      = swapService;
            _layerService     = layerService;
            _levelService     = levelService;
            _hintService      = hintService;
            _boostService     = boostService;
            _inventoryService = inventoryService;
            _boardPresenter   = boardPresenter;
            _layerPresenter   = layerPresenter;
            _inputHandler     = inputHandler;
            _levelRepository  = levelRepository;
        }

        // ── IInitializable ───────────────────────────────────────────────────

        void IInitializable.Initialize()
        {
            var config = _levelRepository.First
                ?? throw new InvalidOperationException("No level config found");

            // ⚠️ Временно — удалить когда появится реальный источник наград
            _inventoryService.AddDebugStarterPack();

            _levelService.StartLevel(config);
            _boardPresenter.InitializeLayout();

            var initialGems = _boardService.GenerateInitialGems(config.AllowedNodeTypes);
            _boardPresenter.CreateGems(initialGems);

            _layerPresenter.RenderLayers(_boardService.Rows, _boardService.Columns);

            _inputHandler.OnCellClicked += OnCellClicked;

            _swapService.OnSwapRequested
                .Subscribe(swap => HandleSwapAsync(swap.from, swap.to, _cts.Token).Forget())
                .AddTo(_disposables);

            // Подсказка из BoostService
            _boostService.OnHintApplied
                .Subscribe(hint => ShowHint(hint.from, hint.to))
                .AddTo(_disposables);

            // Shuffle из BoostService
            _boostService.OnShuffleApplied
                .Subscribe(_ => ShuffleBoardAsync(_cts.Token).Forget())
                .AddTo(_disposables);

            // Буст применён на ячейку → применяем супер-фишку
            _boostService.OnBoostApplied
                .Subscribe(data => ApplyBoostAtAsync(data.boost, data.pos, _cts.Token).Forget())
                .AddTo(_disposables);

            _inputHandler.SetInputEnabled(true);
            Debug.LogWarning("[GameLoop] Инициализирован. Доска готова.");
        }

        // ── Input ────────────────────────────────────────────────────────────

        private void OnCellClicked(Vector2Int pos)
        {
            Debug.LogWarning($"[GameLoop] Клик по ячейке: {pos}");

            // Если активен буст-суперфишка — применяем буст, не делаем своп
            if (_boostService.HasActiveBoost)
            {
                _boostService.TryApplyBoostAt(pos);
                return;
            }

            ClearHint();
            _swapService.TrySelect(pos);
        }

        // ── Boost применение ─────────────────────────────────────────────────

        private async UniTaskVoid ApplyBoostAtAsync(BoostType boost, Vector2Int pos, CancellationToken ct)
        {
            if (!_boardService.IsNormalCell(pos))
            {
                Debug.LogWarning($"[GameLoop] ApplyBoost: {pos} не нормальная ячейка");
                return;
            }

            Debug.LogWarning($"[GameLoop] ApplyBoost: {boost} в {pos}");
            _swapService.Lock();
            _inputHandler.SetInputEnabled(false);

            try
            {
                var superType = BoostTypeToSuperGemType(boost);
                var gem       = _boardService.GetGem(pos);
                var nodeType  = gem?.GemType ?? NodeType.Red;

                var explosionCells = GetExplosionCells(pos, superType, nodeType);

                if (explosionCells.Count > 0)
                {
                    await _boardPresenter.AnimateDestroyCellsAsync(explosionCells, ct);

                    var fallMoves = _boardService.ComputeAndApplyFalls();
                    if (fallMoves.Count > 0)
                        await _boardPresenter.AnimateFallsAsync(fallMoves, ct);

                    var spawnList = _boardService.GetSpawnList();
                    if (spawnList.Count > 0)
                        await _boardPresenter.AnimateSpawnAsync(spawnList, ct);

                    var allCells = CollectAllNormalCells();
                    var matches  = _boardService.FindAndCreateMatches(allCells);
                    if (matches.Count > 0)
                    {
                        _levelService.UseMove();
                        await ResolveAsync(matches, ct);
                        _levelService.ProcessTurnResult();
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[GameLoop] ApplyBoost exception: {e.Message}");
            }
            finally
            {
                _swapService.Unlock();
                _inputHandler.SetInputEnabled(true);
            }
        }

        private static SuperGemType BoostTypeToSuperGemType(BoostType boost) => boost switch
        {
            BoostType.HorizontalArrow => SuperGemType.HorizontalArrow,
            BoostType.VerticalArrow   => SuperGemType.VerticalArrow,
            BoostType.ColorBomb       => SuperGemType.ColorBomb,
            BoostType.Bomb            => SuperGemType.Bomb,
            BoostType.MegaBomb        => SuperGemType.MegaBomb,
            _                         => SuperGemType.None,
        };

        // ── Подсказка ────────────────────────────────────────────────────────

        private void ShowHint(Vector2Int from, Vector2Int to)
        {
            ClearHint();
            _hintGemA = _boardService.GetGem(from) as GemView;
            _hintGemB = _boardService.GetGem(to)   as GemView;
            _hintGemA?.PlayHint();
            _hintGemB?.PlayHint();
            Debug.LogWarning($"[GameLoop] Подсказка: {from} ↔ {to}");
        }

        private void ClearHint()
        {
            _hintGemA?.StopHint();
            _hintGemB?.StopHint();
            _hintGemA = null;
            _hintGemB = null;
        }

        // ── Shuffle ──────────────────────────────────────────────────────────

        private async UniTaskVoid ShuffleBoardAsync(CancellationToken ct)
        {
            _swapService.Lock();
            _inputHandler.SetInputEnabled(false);
            ClearHint();

            try
            {
                var newLayout = _hintService.Shuffle();
                if (newLayout.Count > 0)
                    await _boardPresenter.AnimateShuffleAsync(newLayout, ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[GameLoop] Shuffle exception: {e.Message}");
            }
            finally
            {
                _swapService.Unlock();
                _inputHandler.SetInputEnabled(true);
            }
        }

        // ── Swap flow ────────────────────────────────────────────────────────

        private async UniTaskVoid HandleSwapAsync(Vector2Int from, Vector2Int to, CancellationToken ct)
        {
            Debug.LogWarning($"[GameLoop] HandleSwap {from} → {to}");
            _swapService.Lock();
            _inputHandler.SetInputEnabled(false);
            ClearHint();

            try
            {
                var gemFrom = _boardService.GetGem(from);
                var gemTo   = _boardService.GetGem(to);

                if (gemFrom == null || gemTo == null) return;

                _boardService.ExchangeGems(from, to);
                await _boardPresenter.AnimateSwapAsync(from, to, gemFrom, gemTo, ct);

                _boardService.LockCell(from, false);
                _boardService.LockCell(to,   false);

                var matches = _boardService.FindAndCreateMatches(new[] { from, to });
                Debug.LogWarning($"[GameLoop] Матчей: {matches.Count}");

                if (matches.Count == 0)
                {
                    var retFrom = _boardService.GetGem(from)!;
                    var retTo   = _boardService.GetGem(to)!;
                    _boardService.ExchangeGems(from, to);
                    await _boardPresenter.AnimateReturnSwapAsync(from, to, retFrom, retTo, ct);
                    Debug.LogWarning("[GameLoop] Матча нет — отмена");
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
                Debug.LogWarning("[GameLoop] HandleSwap завершён");
            }
        }

        // ── Resolve loop ─────────────────────────────────────────────────────

        private async UniTask ResolveAsync(List<GemMatch> matches, CancellationToken ct)
        {
            while (matches.Count > 0)
            {
                foreach (var match in matches) match.ComputeSuperGem();
                foreach (var match in matches)
                {
                    _levelService.RegisterMatch(match);
                    _layerService.ProcessMatches(match.MatchingCells);
                }

                var explosionCells = CollectExplosionCells(matches);
                var destroyTasks   = new List<UniTask>(matches.Count + 1);

                foreach (var match in matches)
                    destroyTasks.Add(_boardPresenter.AnimateDestroyMatchAsync(match, ct));

                if (explosionCells.Count > 0)
                    destroyTasks.Add(_boardPresenter.AnimateDestroyCellsAsync(explosionCells, ct));

                await UniTask.WhenAll(destroyTasks).AttachExternalCancellation(ct);

                SpawnSuperGems(matches);

                var fallMoves = _boardService.ComputeAndApplyFalls();
                if (fallMoves.Count > 0)
                    await _boardPresenter.AnimateFallsAsync(fallMoves, ct);

                var spawnList = _boardService.GetSpawnList();
                if (spawnList.Count > 0)
                    await _boardPresenter.AnimateSpawnAsync(spawnList, ct);

                var allCells = CollectAllNormalCells();
                matches = _boardService.FindAndCreateMatches(allCells);
                Debug.LogWarning($"[GameLoop] Каскад: {matches.Count}");
            }
        }

        // ── Super gem helpers ────────────────────────────────────────────────

        private List<Vector2Int> CollectExplosionCells(List<GemMatch> matches)
        {
            var inMatch = new HashSet<Vector2Int>();
            foreach (var match in matches)
                foreach (var cell in match.MatchingCells)
                    inMatch.Add(cell);

            var result = new HashSet<Vector2Int>();
            foreach (var match in matches)
                foreach (var gem in match.MatchedGems)
                {
                    if (gem.SuperGemType == SuperGemType.None) continue;
                    foreach (var cell in GetExplosionCells(gem.CurrentIndex, gem.SuperGemType, gem.GemType))
                        if (!inMatch.Contains(cell))
                            result.Add(cell);
                }

            return new List<Vector2Int>(result);
        }

        private List<Vector2Int> GetExplosionCells(Vector2Int pos, SuperGemType type, NodeType nodeType)
        {
            var cells = new List<Vector2Int>();
            switch (type)
            {
                case SuperGemType.HorizontalArrow:
                    for (var col = 0; col < _boardService.Columns; col++)
                        TryAddCell(new Vector2Int(pos.x, col), cells);
                    break;
                case SuperGemType.VerticalArrow:
                    for (var row = 0; row < _boardService.Rows; row++)
                        TryAddCell(new Vector2Int(row, pos.y), cells);
                    break;
                case SuperGemType.ColorBomb:
                    for (var row = 0; row < _boardService.Rows; row++)
                    for (var col = 0; col < _boardService.Columns; col++)
                    {
                        var cell = new Vector2Int(row, col);
                        var gem  = _boardService.GetGem(cell);
                        if (gem != null && gem.GemType == nodeType) cells.Add(cell);
                    }
                    break;
                case SuperGemType.Bomb:     AddSquareCells(pos, 1, cells); break;
                case SuperGemType.MegaBomb: AddSquareCells(pos, 2, cells); break;
            }
            return cells;
        }

        private void TryAddCell(Vector2Int cell, List<Vector2Int> cells)
        {
            if (_boardService.IsNormalCell(cell) && _boardService.GetGem(cell) != null)
                cells.Add(cell);
        }

        private void AddSquareCells(Vector2Int center, int radius, List<Vector2Int> cells)
        {
            for (var dr = -radius; dr <= radius; dr++)
            for (var dc = -radius; dc <= radius; dc++)
                TryAddCell(new Vector2Int(center.x + dr, center.y + dc), cells);
        }

        private void SpawnSuperGems(List<GemMatch> matches)
        {
            foreach (var match in matches)
            {
                if (!match.HasSuperGemSpawn) continue;
                var pos = match.SuperGemSpawnPos;
                if (!_boardService.IsNormalCell(pos) || _boardService.GetGem(pos) != null) continue;
                _boardPresenter.CreateSuperGemAt(pos, match.MatchNodeType, match.SuperGemToSpawn);
            }
        }

        private List<Vector2Int> CollectAllNormalCells()
        {
            var result = new List<Vector2Int>(_boardService.Rows * _boardService.Columns);
            for (var row = 0; row < _boardService.Rows; row++)
            for (var col = 0; col < _boardService.Columns; col++)
            {
                var pos = new Vector2Int(row, col);
                if (_boardService.IsNormalCell(pos)) result.Add(pos);
            }
            return result;
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
        {
            _inputHandler.OnCellClicked -= OnCellClicked;
            ClearHint();
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
        }
    }
}
