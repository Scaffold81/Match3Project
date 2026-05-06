#nullable enable

using System;
using Match3.Core.Models;
using Match3.Services;
using Match3.Services.Level;
using Match3.Views;
using R3;
using Zenject;

namespace Match3.Presenters
{
    public sealed class LevelPresenter : IInitializable, IDisposable
    {
        private readonly LevelService       _levelService;
        private readonly ProgressService    _progressService;
        private readonly RewardService      _rewardService;
        private readonly ObjectivePresenter _objectivePresenter;
        private readonly LevelResultView    _levelResultView;
        private readonly MoveCounterView    _moveCounterView;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LevelPresenter(
            LevelService       levelService,
            ProgressService    progressService,
            RewardService      rewardService,
            ObjectivePresenter objectivePresenter,
            LevelResultView    levelResultView,
            MoveCounterView    moveCounterView)
        {
            _levelService       = levelService;
            _progressService    = progressService;
            _rewardService      = rewardService;
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
            GrantRewards();
            _levelResultView.ShowWin();
        }

        // ── Прогресс и награды ────────────────────────────────────────────────

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

        private void GrantRewards()
        {
            var config = _levelService.CurrentConfig;
            if (config == null || config.Rewards.Length == 0) return;

            _rewardService.GrantAll(config.Rewards);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
