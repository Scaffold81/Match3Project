#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Services.Board;
using Match3.Services.Layer;
using Match3.Services.Match;
using Match3.Services.MoveCounter;
using Match3.Services.Objective;
using Match3.Services.Spawn;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services.Level
{
    public enum LevelState { Idle, Playing, Won, Lost }

    public sealed class LevelService : IDisposable
    {
        private readonly BoardService       _boardService;
        private readonly MatchService       _matchService;
        private readonly SpawnService       _spawnService;
        private readonly LayerService       _layerService;
        private readonly ObjectiveService   _objectiveService;
        private readonly MoveCounterService _moveCounterService;

        private readonly ReactiveProperty<LevelState> _state = new(LevelState.Idle);
        private readonly Subject<Unit> _onLevelWon  = new();
        private readonly Subject<Unit> _onLevelLost = new();
        private readonly CompositeDisposable _disposables = new();

        public ReadOnlyReactiveProperty<LevelState> State => _state;
        public Observable<Unit> OnLevelWon  => _onLevelWon;
        public Observable<Unit> OnLevelLost => _onLevelLost;
        public LevelConfig? CurrentConfig { get; private set; }

        [Inject]
        public LevelService(
            BoardService       boardService,
            MatchService       matchService,
            SpawnService       spawnService,
            LayerService       layerService,
            ObjectiveService   objectiveService,
            MoveCounterService moveCounterService)
        {
            _boardService       = boardService;
            _matchService       = matchService;
            _spawnService       = spawnService;
            _layerService       = layerService;
            _objectiveService   = objectiveService;
            _moveCounterService = moveCounterService;
        }

        public void StartLevel(LevelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            CurrentConfig = config;

            // ── Сначала инициализируем все системы ──────────────────────
            _boardService.Initialize(config);
            _layerService.Initialize(config);
            _objectiveService.Initialize(config);
            _moveCounterService.Initialize(config.MoveLimit);
            _spawnService.Initialize(config);
            _spawnService.SpawnMissing();

            SubscribeToEvents();

            // ── Только потом переключаем состояние ──────────────────────
            // R3 вызывает подписчиков синхронно — OnLevelStarted сработает
            // здесь, когда доска уже полностью готова
            _state.Value = LevelState.Playing;
        }

        public void ProcessTurnResult()
        {
            if (_state.Value != LevelState.Playing) return;
            CheckWinCondition();
            if (_state.Value != LevelState.Playing) return;
            CheckLoseCondition();
        }

        private void CheckWinCondition()
        {
            var objectivesComplete = _objectiveService.IsAllCompleted;
            var layersComplete     = _layerService.TotalLayerCells == 0 || _layerService.IsAllCleared;
            if (objectivesComplete && layersComplete) Win();
        }

        private void CheckLoseCondition()
        {
            if (_moveCounterService.IsLimited && _moveCounterService.IsExhausted)
                Lose();
        }

        private void Win()
        {
            _state.Value = LevelState.Won;
            _onLevelWon.OnNext(Unit.Default);
        }

        private void Lose()
        {
            _state.Value = LevelState.Lost;
            _onLevelLost.OnNext(Unit.Default);
        }

        private void SubscribeToEvents()
        {
            _disposables.Clear();

            _objectiveService.OnAllObjectivesCompleted
                .Subscribe(_ => CheckWinCondition())
                .AddTo(_disposables);

            _layerService.OnAllLayersCleared
                .Subscribe(_ => CheckWinCondition())
                .AddTo(_disposables);

            _moveCounterService.OnMovesExhausted
                .Subscribe(_ => CheckLoseCondition())
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _state.Dispose();
            _onLevelWon.Dispose();
            _onLevelLost.Dispose();
        }
    }
}
