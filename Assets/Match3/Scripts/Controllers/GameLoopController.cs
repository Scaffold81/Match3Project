#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Match3.Core.Models;
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
        private readonly LevelService           _levelService;
        private readonly BoardService           _boardService;
        private readonly MatchService           _matchService;
        private readonly SwapService            _swapService;
        private readonly GravityService         _gravityService;
        private readonly SpawnService           _spawnService;
        private readonly LayerService           _layerService;
        private readonly ObjectiveService       _objectiveService;
        private readonly MoveCounterService     _moveCounterService;
        private readonly BoardPresenter         _boardPresenter;
        private readonly BoardView              _boardView;
        private readonly LayerPresenter         _layerPresenter;
        private readonly LevelPresenter         _levelPresenter;
        private readonly ObjectivePresenter     _objectivePresenter;
        private readonly AnimationConfig        _animationConfig;
        private readonly LevelConfigRepository  _levelRepository;

        private readonly CompositeDisposable     _disposables = new();
        private readonly CancellationTokenSource _cts         = new();
        private bool _isProcessing;

        [Inject]
        public GameLoopController(
            LevelService            levelService,
            BoardService            boardService,
            MatchService            matchService,
            SwapService             swapService,
            GravityService          gravityService,
            SpawnService            spawnService,
            LayerService            layerService,
            ObjectiveService        objectiveService,
            MoveCounterService      moveCounterService,
            BoardPresenter          boardPresenter,
            BoardView               boardView,
            LayerPresenter          layerPresenter,
            LevelPresenter          levelPresenter,
            ObjectivePresenter      objectivePresenter,
            AnimationConfig         animationConfig,
            LevelConfigRepository   levelRepository)
        {
            _levelService       = levelService;
            _boardService       = boardService;
            _matchService       = matchService;
            _swapService        = swapService;
            _gravityService     = gravityService;
            _spawnService       = spawnService;
            _layerService       = layerService;
            _objectiveService   = objectiveService;
            _moveCounterService = moveCounterService;
            _boardPresenter     = boardPresenter;
            _boardView          = boardView;
            _layerPresenter     = layerPresenter;
            _levelPresenter     = levelPresenter;
            _objectivePresenter = objectivePresenter;
            _animationConfig    = animationConfig;
            _levelRepository    = levelRepository;
        }

        public void Initialize()
        {
            // Если Levels[] пуст — берём первый из репозитория (если есть) или создаём тестовый
            var levelConfigs = _levelRepository.Levels;
            
            if (levelConfigs.Length == 0)
            {
                Debug.LogWarning("[GameLoopController] LevelConfigRepository.Levels is empty, using fallback test level");
                levelConfigs = new[] { CreateTestLevel() };
            }

            var levelConfig = _levelRepository.First ?? levelConfigs[0];

            _swapService.OnSwapSuccess
                .Subscribe(data => OnSwapSucceeded().Forget())
                .AddTo(_disposables);

            _levelService.State
                .Where(state => state == LevelState.Playing)
                .Take(1)
                .Subscribe(_ => OnLevelStarted())
                .AddTo(_disposables);

            _levelService.StartLevel(levelConfig);
        }

        private void OnLevelStarted()
        {
            _boardPresenter.RenderBoard();
            _layerPresenter.RenderLayers(_boardService.Rows, _boardService.Columns);
            _objectivePresenter.RenderObjectives(_objectiveService.Progress.CurrentValue);
            _levelPresenter.SetupMoveCounter();
        }

        private async UniTaskVoid OnSwapSucceeded()
        {
            if (_isProcessing) return;
            _isProcessing = true;
            _swapService.Lock();

            try
            {
                // Ждём завершения анимации свопа
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
            var board   = _boardService.Board.CurrentValue;
            var matches = _matchService.FindMatches(board, _boardService.Rows, _boardService.Columns);

            while (matches.Count > 0)
            {
                var matchedCells  = _matchService.GetAllMatchedCells(matches);
                var boardSnapshot = CopyBoard(board);

                _objectiveService.RegisterMatch(matchedCells, boardSnapshot);
                _layerService.ProcessMatches(matchedCells);

                // Анимируем уничтожение — PlayDestroy внутри уже делает SetEmpty
                await AnimateDestroyAsync(matchedCells, ct);

                // Обновляем логику
                foreach (var cell in matchedCells)
                    _boardService.RemoveNode(cell.x, cell.y);

                await UniTask.Yield(ct);

                // Гравитация — сдвигаем визуал вниз
                var falls = _gravityService.ApplyGravity();
                ApplyFallsVisual(falls);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(_animationConfig.FallDuration),
                    cancellationToken: ct);

                // Спаун — ставим визуал + анимация появления
                var spawned = _spawnService.SpawnMissing();
                await AnimateSpawnAsync(spawned, ct);

                board   = _boardService.Board.CurrentValue;
                matches = _matchService.FindMatches(board, _boardService.Rows, _boardService.Columns);
            }
        }

        private async UniTask AnimateDestroyAsync(List<Vector2Int> cells, CancellationToken ct)
        {
            if (cells.Count == 0) return;

            var pending = cells.Count;
            var tcs     = new UniTaskCompletionSource();

            foreach (var cell in cells)
            {
                var gemView = _boardView.GetGemView(cell);
                if (gemView == null || gemView.IsEmpty)
                {
                    pending--;
                    if (pending == 0) tcs.TrySetResult();
                    continue;
                }

                // PlayDestroy сам делает SetEmpty внутри коллбэка
                gemView.PlayDestroy(_animationConfig.MatchDestroyDuration, () =>
                {
                    pending--;
                    if (pending == 0) tcs.TrySetResult();
                });
            }

            await tcs.Task.AttachExternalCancellation(ct);
        }

        // Гравитация — визуал перетекает вниз без анимации позиции
        private void ApplyFallsVisual(List<(Vector2Int from, Vector2Int to)> falls)
        {
            foreach (var (from, to) in falls)
            {
                var fromView = _boardView.GetGemView(from);
                var toView   = _boardView.GetGemView(to);
                if (fromView == null || toView == null) continue;

                toView.CopyVisualFrom(fromView);
                fromView.SetEmpty();
            }
        }

        private async UniTask AnimateSpawnAsync(
            List<(Vector2Int position, NodeType nodeType)> spawned, CancellationToken ct)
        {
            if (spawned.Count == 0) return;

            foreach (var (cell, nodeType) in spawned)
            {
                _boardPresenter.SetCellVisual(cell, nodeType);
                _boardView.GetGemView(cell)?.PlaySpawn(_animationConfig.FallDuration);
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

        // Тестовый уровень для быстрого старта (без LevelConfig asset)
        private LevelConfig CreateTestLevel()
        {
            var rows = 5;
            var cols = 7;
            
            // Явный список всех NodeTypes
            var nodeTypes = new[] { NodeType.Red, NodeType.Blue, NodeType.Green, NodeType.Yellow, NodeType.Purple, NodeType.Orange };

            // Создаём Grid[] по структуре LevelConfig через конструкторы
            var grid = new CellRow[rows];
            for (var r = 0; r < rows; r++)
            {
                var rowCells = new CellData[cols];
                for (var c = 0; c < cols; c++)
                {
                    var randomType = nodeTypes[UnityEngine.Random.Range(0, nodeTypes.Length)];
                    rowCells[c] = new CellData(CellType.Normal, randomType, false);
                }
                grid[r] = new CellRow();
                grid[r].Cells = rowCells;
            }

            return new LevelConfig(grid, 0, Array.Empty<ObjectiveData>());
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
        }
    }
}
