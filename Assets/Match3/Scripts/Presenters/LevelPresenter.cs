#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Models;
using Match3.Services;
using Match3.Services.Level;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class LevelPresenter : IInitializable, IDisposable
    {
        private readonly LevelService       _levelService;
        private readonly ProgressService    _progressService;
        private readonly RewardService      _rewardService;
        private readonly WorldMapConfig     _worldMapConfig;
        private readonly ObjectivePresenter _objectivePresenter;
        private readonly LevelResultView    _levelResultView;
        private readonly MoveCounterView    _moveCounterView;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LevelPresenter(
            LevelService       levelService,
            ProgressService    progressService,
            RewardService      rewardService,
            WorldMapConfig     worldMapConfig,
            ObjectivePresenter objectivePresenter,
            LevelResultView    levelResultView,
            MoveCounterView    moveCounterView)
        {
            _levelService       = levelService;
            _progressService    = progressService;
            _rewardService      = rewardService;
            _worldMapConfig     = worldMapConfig;
            _objectivePresenter = objectivePresenter;
            _levelResultView    = levelResultView;
            _moveCounterView    = moveCounterView;
        }

        public void Initialize()
        {
            _levelResultView.Hide();

            if (_levelService.IsMoveLimited)
                _moveCounterView.SetLimited(_levelService.MovesLeft.CurrentValue);
            else
                _moveCounterView.SetUnlimited();

            _objectivePresenter.RenderObjectives(_levelService.Progress.CurrentValue);

            _levelService.State
                .Subscribe(OnStateChanged)
                .AddTo(_disposables);

            _levelService.MovesLeft
                .Subscribe(movesLeft =>
                {
                    if (_levelService.IsMoveLimited)
                        _moveCounterView.UpdateMovesLeft(movesLeft);
                })
                .AddTo(_disposables);
        }

        // ── Состояние уровня ──────────────────────────────────────────────────

        private void OnStateChanged(LevelState state)
        {
            switch (state)
            {
                case LevelState.Won:
                    OnWon();
                    break;
                case LevelState.Lost:
                    _levelResultView.ShowLose();
                    break;
                case LevelState.Playing:
                    _levelResultView.Hide();
                    break;
            }
        }

        private void OnWon()
        {
            SaveProgress();
            TryGrantStageRewards();
            _levelResultView.ShowWin();
        }

        // ── Прогресс ─────────────────────────────────────────────────────────

        private void SaveProgress()
        {
            var config = _levelService.CurrentConfig;
            if (config == null) return;

            var address = _progressService.CurrentAddress.CurrentValue;
            var stars   = StarCalculator.Calculate(
                _levelService.MovesLeft.CurrentValue,
                config.MoveLimit);

            _progressService.SetStars(address, stars);
        }

        // ── Награды этапа ─────────────────────────────────────────────────────

        /// <summary>
        /// Выдаёт StageRewards если только что был пройден последний уровень этапа.
        /// Проверяем ПОСЛЕ сохранения прогресса.
        /// </summary>
        private void TryGrantStageRewards()
        {
            var address = _progressService.CurrentAddress.CurrentValue;
            var stage   = _worldMapConfig.GetStage(address.CountryIndex, address.StageIndex);
            if (stage == null || stage.StageRewards.Length == 0) return;

            // Проверяем что все 3 уровня этапа теперь пройдены
            if (!_progressService.IsStageCompleted(address.CountryIndex, address.StageIndex)) return;

            // Проверяем что именно сейчас закрылся последний уровень (LevelIndex == 2)
            if (address.LevelIndex != stage.LevelCount - 1) return;

            _rewardService.GrantAll(stage.StageRewards);
            Debug.LogWarning($"[LevelPresenter] Этап завершён — выдано {stage.StageRewards.Length} наград");
        }

        public void Dispose() => _disposables.Dispose();
    }
}
