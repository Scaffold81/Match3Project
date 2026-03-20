#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Presenters;
using Match3.Services.Board;
using Match3.Services.Gravity;
using Match3.Services.Layer;
using Match3.Services.Level;
using Match3.Services.Match;
using Match3.Services.MoveCounter;
using Match3.Services.Objective;
using Match3.Services.Spawn;
using Match3.Services.Swap;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Controllers
{
    public sealed class GameLoopController : IInitializable, IDisposable
    {
        private readonly LevelService _levelService;
        private readonly BoardService _boardService;
        private readonly MatchService _matchService;
        private readonly SwapService _swapService;
        private readonly GravityService _gravityService;
        private readonly SpawnService _spawnService;
        private readonly LayerService _layerService;
        private readonly ObjectiveService _objectiveService;
        private readonly MoveCounterService _moveCounterService;
        private readonly BoardPresenter _boardPresenter;
        private readonly BoardView _boardView;
        private readonly LayerPresenter _layerPresenter;
        private readonly LevelPresenter _levelPresenter;
        private readonly ObjectivePresenter _objectivePresenter;
        private readonly AnimationConfig _animationConfig;
        private readonly LevelConfig _startLevelConfig;

        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _cts = new();
        private bool _isProcessing;

        [Inject]
        public GameLoopController(
            LevelService levelService,
            BoardService boardService,
            MatchService matchService,
            SwapService swapService,
            GravityService gravityService,
            SpawnService spawnService,
            LayerService layerService,
            ObjectiveService objectiveService,
            MoveCounterService moveCounterService,
            BoardPresenter boardPresenter,
            BoardView boardView,
            LayerPresenter layerPresenter,
            LevelPresenter levelPresenter,
            ObjectivePresenter objectivePresenter,
            AnimationConfig animationConfig,
            LevelConfig startLevelConfig)
        {
            _levelService = levelService;
            _boardService = boardService;
            _matchService = matchService;
            _swapService = swapService;
            _gravityService = gravityService;
            _spawnService = spawnService;
            _layerService = layerService;
            _objectiveService = objectiveService;
            _moveCounterService = moveCounterService;
            _boardPresenter = boardPresenter;
            _boardView = boardView;
            _layerPresenter = layerPresenter;
            _levelPresenter = levelPresenter;
            _objectivePresenter = objectivePresenter;
            _animationConfig = animationConfig;
            _startLevelConfig = startLevelConfig;
        }

        public void Initialize()
        {
            _swapService.OnSwapSuccess
                .Subscribe(data => OnSwapSucceeded(data.from, data.to).Forget())
                .AddTo(_disposables);

            _levelService.State
                .Where(state => state == LevelState.Playing)
                .Take(1)
                .Subscribe(_ => OnLevelStarted())
                .AddTo(_disposables);

            _levelService.StartLevel(_startLevelConfig);
        }

        private void OnLevelStarted()
        {
            _boardPresenter.RenderBoard();
            _layerPresenter.RenderLayers(_boardService.Rows, _boardService.Columns);
            _objectivePresenter.RenderObjectives(_objectiveService.Progress.CurrentValue);
            _levelPresenter.SetupMoveCounter();
        }

        private async UniTaskVoid OnSwapSucceeded(Vector2Int from, Vector2Int to)
        {
            if (_isProcessing) return;
            _isProcessing = true;
            _swapService.Lock();

            try
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_animationConfig.SwapDuration),
                    cancellationToken: _cts.Token);

                await ProcessMatchesAsync(_cts.Token);

                _moveCounterService.UseMove();
                _levelService.ProcessTurnResult();
            }
            catch (OperationCanceledException) { }
            finally
            {
                _isProcessing = false;
                if (_levelService.State.CurrentValue == LevelState.Playing)
                    _swapService.Unlock();
            }
        }

        private async UniTask ProcessMatchesAsync(CancellationToken ct)
        {
            var board = _boardService.Board.CurrentValue;
            var matches = _matchService.FindMatches(board, _boardService.Rows, _boardService.Columns);

            while (matches.Count > 0)
            {
                var matchedCells = _matchService.GetAllMatchedCells(matches);
                var boardSnapshot = CopyBoard(board);

                _objectiveService.RegisterMatch(matchedCells, boardSnapshot);
                _layerService.ProcessMatches(matchedCells);

                await AnimateDestroyAsync(matchedCells, ct);

                foreach (var cell in matchedCells)
                    _boardService.RemoveNode(cell.x, cell.y);

                await UniTask.Yield(ct);

                var falls = _gravityService.ApplyGravity();
                await AnimateFallsAsync(falls, ct);

                var spawned = _spawnService.SpawnMissing();
                await AnimateSpawnAsync(spawned, ct);

                board = _boardService.Board.CurrentValue;
                matches = _matchService.FindMatches(board, _boardService.Rows, _boardService.Columns);
            }
        }

        private async UniTask AnimateDestroyAsync(
            List<Vector2Int> cells,
            CancellationToken ct)
        {
            var pending = cells.Count;
            if (pending == 0) return;

            var tcs = new UniTaskCompletionSource();

            foreach (var cell in cells)
            {
                var gemView = _boardView.GetGemView(cell);
                if (gemView == null)
                {
                    pending--;
                    if (pending == 0) tcs.TrySetResult();
                    continue;
                }

                gemView.PlayDestroy(_animationConfig.MatchDestroyDuration, () =>
                {
                    _boardView.RemoveGem(cell);
                    pending--;
                    if (pending == 0) tcs.TrySetResult();
                });
            }

            await tcs.Task.AttachExternalCancellation(ct);
        }

        private async UniTask AnimateFallsAsync(
            List<(Vector2Int from, Vector2Int to)> falls,
            CancellationToken ct)
        {
            if (falls.Count == 0) return;

            var pending = falls.Count;
            var tcs = new UniTaskCompletionSource();

            foreach (var (from, to) in falls)
            {
                var gemView = _boardView.GetGemView(from);
                if (gemView == null)
                {
                    pending--;
                    if (pending == 0) tcs.TrySetResult();
                    continue;
                }

                _boardView.MoveGem(from, to);
                var targetPos = _boardView.GetAnchoredPosition(to.x, to.y);

                gemView.PlayFall(targetPos, _animationConfig.FallDuration, () =>
                {
                    pending--;
                    if (pending == 0) tcs.TrySetResult();
                });
            }

            await tcs.Task.AttachExternalCancellation(ct);
        }

        private async UniTask AnimateSpawnAsync(
            List<(Vector2Int position, NodeType nodeType)> spawned,
            CancellationToken ct)
        {
            if (spawned.Count == 0) return;

            foreach (var (cell, nodeType) in spawned)
            {
                _boardPresenter.SpawnGemView(cell, nodeType);
                var gemView = _boardView.GetGemView(cell);
                gemView?.PlaySpawn(_animationConfig.FallDuration);
            }

            await UniTask.Delay(
                TimeSpan.FromSeconds(_animationConfig.FallDuration),
                cancellationToken: ct);
        }

        private NodeType[,] CopyBoard(NodeType[,] source)
        {
            var rows = source.GetLength(0);
            var cols = source.GetLength(1);
            var copy = new NodeType[rows, cols];

            for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                copy[r, c] = source[r, c];

            return copy;
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
        }
    }
}
