#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using Match3.Core;
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

                // Меняем данные и запускаем анимацию свопа
                _boardService.ExchangeGems(from, to);
                await _boardPresenter.AnimateSwapAsync(from, to, gemFrom, gemTo, ct);

                // ✅ Разблокируем ДО проверки матча — иначе CanMatch() = false
                _boardService.LockCell(from, false);
                _boardService.LockCell(to,   false);

                var matches = _boardService.FindAndCreateMatches(new[] { from, to });
                Debug.LogWarning($"[GameLoop] Матчей найдено: {matches.Count}");

                if (matches.Count == 0)
                {
                    // Нет матча — возвращаем данные и анимируем обратно
                    var returnFrom = _boardService.GetGem(from)!;
                    var returnTo   = _boardService.GetGem(to)!;

                    _boardService.ExchangeGems(from, to);
                    await _boardPresenter.AnimateReturnSwapAsync(from, to, returnFrom, returnTo, ct);

                    Debug.LogWarning("[GameLoop] Матча нет — своп отменён");
                    return;
                }

                // Есть матч — засчитываем ход и запускаем resolve
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
                Debug.LogWarning($"[GameLoop] ResolveAsync — уничтожаем {matches.Count} матч(ей)");

                foreach (var match in matches)
                {
                    _levelService.RegisterMatch(match);
                    _layerService.ProcessMatches(match.MatchingCells);
                }

                await DestroyMatchesAsync(matches, ct);

                var fallMoves = _boardService.ComputeAndApplyFalls();
                Debug.LogWarning($"[GameLoop] Падений: {fallMoves.Count}");

                if (fallMoves.Count > 0)
                    await _boardPresenter.AnimateFallsAsync(fallMoves, ct);

                var spawnList = _boardService.GetSpawnList();
                Debug.LogWarning($"[GameLoop] Спавн: {spawnList.Count}");

                if (spawnList.Count > 0)
                    await _boardPresenter.AnimateSpawnAsync(spawnList, ct);

                // Каскад — проверяем всю доску
                var allCells = CollectAllNormalCells();
                matches = _boardService.FindAndCreateMatches(allCells);
                Debug.LogWarning($"[GameLoop] Каскадных матчей: {matches.Count}");
            }
        }

        private async UniTask DestroyMatchesAsync(List<GemMatch> matches, CancellationToken ct)
        {
            var tasks = new List<UniTask>(matches.Count);
            foreach (var match in matches)
                tasks.Add(_boardPresenter.AnimateDestroyMatchAsync(match, ct));

            await UniTask.WhenAll(tasks).AttachExternalCancellation(ct);
        }

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
