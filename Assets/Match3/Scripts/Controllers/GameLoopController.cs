#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using Match3.Core;
using Match3.Core.Enums;
using Match3.Presenters;
using Match3.Services;
using Match3.Services.Board;
using Match3.Services.Boost;
using Match3.Services.Hint;
using Match3.Services.Inventory;
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
        private readonly BoardService      _boardService;
        private readonly SwapService       _swapService;
        private readonly LevelService      _levelService;
        private readonly HintService       _hintService;
        private readonly BoostService      _boostService;
        private readonly InventoryService  _inventoryService;
        private readonly BoardPresenter    _boardPresenter;
        private readonly LayerPresenter    _layerPresenter;
        private readonly BoardInputHandler _inputHandler;
        private readonly WorldMapConfig    _worldMapConfig;
        private readonly ProgressService   _progressService;

        private readonly CompositeDisposable     _disposables = new();
        private readonly CancellationTokenSource _cts         = new();

        private GemView? _hintGemA;
        private GemView? _hintGemB;

        [Inject]
        public GameLoopController(
            BoardService      boardService,
            SwapService       swapService,
            LevelService      levelService,
            HintService       hintService,
            BoostService      boostService,
            InventoryService  inventoryService,
            BoardPresenter    boardPresenter,
            LayerPresenter    layerPresenter,
            BoardInputHandler inputHandler,
            WorldMapConfig    worldMapConfig,
            ProgressService   progressService)
        {
            _boardService     = boardService;
            _swapService      = swapService;
            _levelService     = levelService;
            _hintService      = hintService;
            _boostService     = boostService;
            _inventoryService = inventoryService;
            _boardPresenter   = boardPresenter;
            _layerPresenter   = layerPresenter;
            _inputHandler     = inputHandler;
            _worldMapConfig   = worldMapConfig;
            _progressService  = progressService;
        }

        // ── IInitializable ───────────────────────────────────────────────────

        void IInitializable.Initialize()
        {
            var config = ResolveCurrentLevelConfig();
            if (config == null)
            {
                Debug.LogError("[GameLoop] LevelConfig не найден — проверь CurrentAddress в ProgressService");
                return;
            }

            // ⚠️ Временно — удалить когда появится реальная выдача наград
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

            _boostService.OnHintApplied
                .Subscribe(hint => ShowHint(hint.from, hint.to))
                .AddTo(_disposables);

            _boostService.OnShuffleApplied
                .Subscribe(_ => ShuffleBoardAsync(_cts.Token).Forget())
                .AddTo(_disposables);

            _boostService.OnBoostApplied
                .Subscribe(data => ApplyBoostAtAsync(data.boost, data.pos, _cts.Token).Forget())
                .AddTo(_disposables);

            _boostService.OnBoostSelected
                .Subscribe(_ => _swapService.ClearSelection())
                .AddTo(_disposables);

            _boostService.OnBoostCancelled
                .Subscribe(_ => _swapService.ClearSelection())
                .AddTo(_disposables);

            // Ввод намеренно НЕ включается здесь.
            // GameFlowService покажет попап задания и вызовет EnableInput() после закрытия.
            _inputHandler.SetInputEnabled(false);
        }

        /// <summary>
        /// Разрешает ввод. Вызывается GameFlowService после закрытия попапа задания.
        /// </summary>
        public void EnableInput() => _inputHandler.SetInputEnabled(true);

        // ── Загрузка конфига уровня ───────────────────────────────────────────

        private LevelConfig? ResolveCurrentLevelConfig()
        {
            var address = _progressService.CurrentAddress.CurrentValue;

            var stage = _worldMapConfig.GetStage(address.CountryIndex, address.StageIndex);
            if (stage == null)
            {
                Debug.LogError($"[GameLoop] Stage не найден: country={address.CountryIndex} " +
                               $"stage={address.StageIndex}");
                return null;
            }

            var config = stage.GetLevel(address.LevelIndex);
            if (config == null)
            {
                Debug.LogError($"[GameLoop] LevelConfig не найден: level={address.LevelIndex} " +
                               $"в stage={stage.StageName}");
                return null;
            }

            return config;
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private void OnCellClicked(Vector2Int pos)
        {
            if (_boostService.HasActiveBoost)
            {
                _boostService.TryApplyBoostAt(pos);
                return;
            }

            ClearHint();
            _swapService.TrySelect(pos);
        }

        // ── Boost ─────────────────────────────────────────────────────────────

        private async UniTaskVoid ApplyBoostAtAsync(BoostType boost, Vector2Int pos, CancellationToken ct)
        {
            if (!_boardService.IsNormalCell(pos)) return;

            _swapService.Lock();
            _inputHandler.SetInputEnabled(false);

            try
            {
                var superType      = boost.ToSuperGemType();
                var gem            = _boardService.GetGem(pos);
                var nodeType       = gem?.GemType ?? NodeType.Red;
                var explosionCells = GetExplosionCells(pos, superType, nodeType);

                if (explosionCells.Count > 0)
                {
                    // Собираем гемы ДО анимации: после AnimateDestroyCellsAsync
                    // они удаляются с доски и ссылки станут недействительны.
                    var destroyedGems = CollectGemsAt(explosionCells);

                    // Регистрируем ДО анимации: после AnimateDestroyCellsAsync
                    // GemView уничтожаются и gem.GemType становится недействительным.
                    _levelService.RegisterDestroyedCells(destroyedGems);

                    await _boardPresenter.AnimateDestroyCellsAsync(explosionCells, ct);

                    // Прямой удар по препятствиям в зоне взрыва бустера
                    _boardService.HitObstaclesDirectly(explosionCells);

                    var fallMoves = _boardService.ComputeAndApplyFalls();
                    if (fallMoves.Count > 0)
                        await _boardPresenter.AnimateFallsAsync(fallMoves, ct);

                    var spawnList = _boardService.GetSpawnList();
                    if (spawnList.Count > 0)
                        await _boardPresenter.AnimateSpawnAsync(spawnList, ct);

                    var matches = _boardService.FindAndCreateMatches(CollectAllNormalCells());
                    if (matches.Count > 0)
                    {
                        _levelService.UseMove();
                        await ResolveAsync(matches, ct);
                    }
                }

                // Всегда проверяем победу/поражение после буста —
                // даже если цепных матчей не возникло, буст мог закрыть последние цели.
                _levelService.ProcessTurnResult();
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogError($"[GameLoop] ApplyBoost: {e.Message}"); }
            finally
            {
                _swapService.Unlock();
                _inputHandler.SetInputEnabled(true);
            }
        }

        private IReadOnlyList<IGemView> CollectGemsAt(IEnumerable<Vector2Int> cells)
        {
            var result = new List<IGemView>();
            foreach (var p in cells)
            {
                var g = _boardService.GetGem(p);
                if (g != null) result.Add(g);
            }
            return result;
        }

        // ── Hint / Shuffle ────────────────────────────────────────────────────

        private void ShowHint(Vector2Int from, Vector2Int to)
        {
            ClearHint();
            _hintGemA = _boardService.GetGem(from) as GemView;
            _hintGemB = _boardService.GetGem(to)   as GemView;
            _hintGemA?.PlayHint();
            _hintGemB?.PlayHint();
        }

        private void ClearHint()
        {
            _hintGemA?.StopHint();
            _hintGemB?.StopHint();
            _hintGemA = _hintGemB = null;
        }

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
            catch (Exception e) { Debug.LogError($"[GameLoop] Shuffle: {e.Message}"); }
            finally
            {
                _swapService.Unlock();
                _inputHandler.SetInputEnabled(true);
            }
        }

        // ── Swap ──────────────────────────────────────────────────────────────

        private async UniTaskVoid HandleSwapAsync(Vector2Int from, Vector2Int to, CancellationToken ct)
        {
            _swapService.Lock();
            _inputHandler.SetInputEnabled(false);
            ClearHint();

            try
            {
                // Разблокируем ячейки немедленно: ввод уже отключён,
                // поэтому дополнительная блокировка на время анимации избыточна.
                // Без этого при раннем return ячейки оставались залоченными навсегда.
                _boardService.LockCell(from, false);
                _boardService.LockCell(to,   false);

                var gemFrom = _boardService.GetGem(from);
                var gemTo   = _boardService.GetGem(to);
                if (gemFrom == null || gemTo == null) return;

                _boardService.ExchangeGems(from, to);
                await _boardPresenter.AnimateSwapAsync(from, to, gemFrom, gemTo, ct);

                var matches = _boardService.FindAndCreateMatches(new[] { from, to });

                if (matches.Count == 0)
                {
                    _boardService.ExchangeGems(from, to);
                    // Передаём to/from в обратном порядке: после undo-свапа
                    // gemA визуально в to-слоте (нужно в from), gemB — в from-слоте (нужно в to).
                    // AnimateReturnSwapAsync использует worldTo=slotPos(2-й парам)
                    // и worldFrom=slotPos(1-й парам), поэтому переставляем их.
                    await _boardPresenter.AnimateReturnSwapAsync(
                        to, from, _boardService.GetGem(from)!, _boardService.GetGem(to)!, ct);
                    return;
                }

                _levelService.UseMove();
                await ResolveAsync(matches, ct);
                _levelService.ProcessTurnResult();
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { Debug.LogError($"[GameLoop] HandleSwap: {e}"); }
            finally
            {
                _swapService.Unlock();
                _inputHandler.SetInputEnabled(true);
            }
        }

        // ── Resolve loop ──────────────────────────────────────────────────────

        private async UniTask ResolveAsync(List<GemMatch> matches, CancellationToken ct)
        {
            while (matches.Count > 0)
            {
                foreach (var match in matches) match.ComputeSuperGem();
                foreach (var match in matches)
                {
                    _levelService.RegisterMatch(match);
                    // Правила удара по препятствиям: Ice/Box смежные, Chain HP=1 прямой матч
                    _boardService.ProcessObstaclesFromMatch(match.MatchingCells);
                }

                var explosionCells = CollectExplosionCells(matches);
                var tasks          = new List<UniTask>(matches.Count + 1);

                foreach (var match in matches)
                    tasks.Add(_boardPresenter.AnimateDestroyMatchAsync(match, ct));

                if (explosionCells.Count > 0)
                    tasks.Add(_boardPresenter.AnimateDestroyCellsAsync(explosionCells, ct));

                await UniTask.WhenAll(tasks).AttachExternalCancellation(ct);

                // Прямой удар по препятствиям в зоне взрыва супер-фишек
                if (explosionCells.Count > 0)
                    _boardService.HitObstaclesDirectly(explosionCells);

                SpawnSuperGems(matches);

                var fallMoves = _boardService.ComputeAndApplyFalls();
                if (fallMoves.Count > 0)
                    await _boardPresenter.AnimateFallsAsync(fallMoves, ct);

                var spawnList = _boardService.GetSpawnList();
                if (spawnList.Count > 0)
                    await _boardPresenter.AnimateSpawnAsync(spawnList, ct);

                matches = _boardService.FindAndCreateMatches(CollectAllNormalCells());
            }
        }

        // ── Super gem helpers ─────────────────────────────────────────────────

        private List<Vector2Int> CollectExplosionCells(List<GemMatch> matches)
        {
            var inMatch = new HashSet<Vector2Int>();
            foreach (var m in matches)
                foreach (var cell in m.MatchingCells)
                    inMatch.Add(cell);

            var result = new HashSet<Vector2Int>();
            foreach (var m in matches)
                foreach (var gem in m.MatchedGems)
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
                        if (_boardService.GetGem(cell)?.GemType == nodeType)
                            cells.Add(cell);
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

        // ── IDisposable ───────────────────────────────────────────────────────

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
