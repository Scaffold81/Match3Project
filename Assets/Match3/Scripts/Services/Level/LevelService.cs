#nullable enable

using System;
using System.Collections.Generic;
using Match3.Configs;
using Match3.Core;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services.Board;
using Match3.Services.Layer;
using R3;
using Zenject;

namespace Match3.Services.Level
{
    public enum LevelState { Idle, Playing, Won, Lost }

    public sealed class LevelService : IDisposable
    {
        private readonly BoardService _boardService;
        private readonly LayerService _layerService;

        // ── Level ────────────────────────────────────────────────────────────
        private readonly ReactiveProperty<LevelState> _state       = new(LevelState.Idle);
        private readonly Subject<Unit>                _onLevelWon  = new();
        private readonly Subject<Unit>                _onLevelLost = new();

        public ReadOnlyReactiveProperty<LevelState> State       => _state;
        public Observable<Unit>                     OnLevelWon  => _onLevelWon;
        public Observable<Unit>                     OnLevelLost => _onLevelLost;
        public LevelConfig?                         CurrentConfig { get; private set; }

        // ── Objectives ───────────────────────────────────────────────────────
        private readonly ReactiveProperty<ObjectiveProgress[]> _progress                 = new(Array.Empty<ObjectiveProgress>());
        private readonly Subject<NodeType>                     _onObjectiveCompleted      = new();
        private readonly Subject<Unit>                         _onAllObjectivesCompleted  = new();

        public ReadOnlyReactiveProperty<ObjectiveProgress[]> Progress                 => _progress;
        public Observable<NodeType>                          OnObjectiveCompleted      => _onObjectiveCompleted;
        public Observable<Unit>                              OnAllObjectivesCompleted  => _onAllObjectivesCompleted;
        public bool                                          IsAllObjectivesCompleted  { get; private set; }

        // ── Move counter ─────────────────────────────────────────────────────
        private readonly ReactiveProperty<int> _movesUsed        = new(0);
        private readonly ReactiveProperty<int> _movesLeft        = new(0);
        private readonly Subject<Unit>         _onMovesExhausted = new();

        public ReadOnlyReactiveProperty<int> MovesUsed        => _movesUsed;
        public ReadOnlyReactiveProperty<int> MovesLeft        => _movesLeft;
        public Observable<Unit>              OnMovesExhausted => _onMovesExhausted;
        public bool                          IsMoveLimited     { get; private set; }
        public bool                          IsMovesExhausted  => IsMoveLimited && _movesLeft.Value <= 0;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LevelService(BoardService boardService, LayerService layerService)
        {
            _boardService = boardService;
            _layerService = layerService;
        }

        // ── Startup ──────────────────────────────────────────────────────────

        public void StartLevel(LevelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            CurrentConfig = config;

            _boardService.Initialize(config);
            _layerService.Initialize(config);

            InitializeObjectives(config);
            InitializeMoveCounter(config.MoveLimit);
            SubscribeToEvents();

            _state.Value = LevelState.Playing;
        }

        // ── Objectives ───────────────────────────────────────────────────────

        private void InitializeObjectives(LevelConfig config)
        {
            IsAllObjectivesCompleted = false;
            var list = new ObjectiveProgress[config.Objectives.Length];
            for (var i = 0; i < config.Objectives.Length; i++)
                list[i] = new ObjectiveProgress(config.Objectives[i].nodeType, config.Objectives[i].count);
            _progress.Value = list;
        }

        public void RegisterMatch(GemMatch match)
        {
            if (match == null)
                throw new ArgumentNullException(nameof(match));

            var countByType = new Dictionary<NodeType, int>();
            foreach (var gem in match.MatchedGems)
            {
                if (gem.GemType == NodeType.None) continue;
                countByType.TryGetValue(gem.GemType, out var prev);
                countByType[gem.GemType] = prev + 1;
            }

            foreach (var kvp in countByType)
                RegisterCollected(kvp.Key, kvp.Value);
        }

        private void RegisterCollected(NodeType nodeType, int count)
        {
            var progress = _progress.Value;
            var changed  = false;

            for (var i = 0; i < progress.Length; i++)
            {
                if (progress[i].NodeType != nodeType) continue;
                if (progress[i].IsCompleted) continue;
                progress[i].AddCollected(count);
                changed = true;
                if (progress[i].IsCompleted)
                    _onObjectiveCompleted.OnNext(nodeType);
            }

            if (!changed) return;
            _progress.ForceNotify();
            CheckAllObjectivesCompleted();
        }

        private void CheckAllObjectivesCompleted()
        {
            if (IsAllObjectivesCompleted) return;
            foreach (var p in _progress.Value)
                if (!p.IsCompleted) return;
            IsAllObjectivesCompleted = true;
            _onAllObjectivesCompleted.OnNext(Unit.Default);
        }

        // ── Move counter ─────────────────────────────────────────────────────

        private void InitializeMoveCounter(int moveLimit)
        {
            if (moveLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(moveLimit));
            _movesUsed.Value = 0;
            IsMoveLimited    = moveLimit > 0;
            _movesLeft.Value = moveLimit;
        }

        public bool UseMove()
        {
            if (IsMoveLimited && IsMovesExhausted) return false;
            _movesUsed.Value++;
            if (IsMoveLimited)
            {
                _movesLeft.Value--;
                if (_movesLeft.Value <= 0)
                    _onMovesExhausted.OnNext(Unit.Default);
            }
            return true;
        }

        // ── Win / Lose ───────────────────────────────────────────────────────

        public void ProcessTurnResult()
        {
            CheckWinCondition();
            if (_state.Value != LevelState.Playing) return;
            CheckLoseCondition();
        }

        private void CheckWinCondition()
        {
            if (_state.Value != LevelState.Playing) return;
            var objectivesOk = IsAllObjectivesCompleted;
            var layersOk     = _layerService.TotalLayerCells == 0 || _layerService.IsAllCleared;
            if (objectivesOk && layersOk) Win();
        }

        private void CheckLoseCondition()
        {
            if (_state.Value != LevelState.Playing) return;
            if (IsMoveLimited && IsMovesExhausted) Lose();
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

            _onAllObjectivesCompleted
                .Subscribe(_ => CheckWinCondition())
                .AddTo(_disposables);

            _layerService.OnAllLayersCleared
                .Subscribe(_ => CheckWinCondition())
                .AddTo(_disposables);

            _onMovesExhausted
                .Subscribe(_ => CheckLoseCondition())
                .AddTo(_disposables);
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
        {
            _disposables.Dispose();
            _state.Dispose();
            _onLevelWon.Dispose();
            _onLevelLost.Dispose();
            _progress.Dispose();
            _onObjectiveCompleted.Dispose();
            _onAllObjectivesCompleted.Dispose();
            _movesUsed.Dispose();
            _movesLeft.Dispose();
            _onMovesExhausted.Dispose();
        }
    }

    // ── ObjectiveProgress ─────────────────────────────────────────────────────
    public sealed class ObjectiveProgress
    {
        public NodeType NodeType    { get; }
        public int      Required    { get; }
        public int      Collected   { get; private set; }
        public bool     IsCompleted => Collected >= Required;

        public ObjectiveProgress(NodeType nodeType, int required)
        {
            NodeType  = nodeType;
            Required  = required;
            Collected = 0;
        }

        public void AddCollected(int count) =>
            Collected = Math.Min(Collected + count, Required);
    }
}
