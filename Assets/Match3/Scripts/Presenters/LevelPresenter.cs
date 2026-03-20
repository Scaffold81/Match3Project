#nullable enable

using System;
using Match3.Services.Level;
using Match3.Services.MoveCounter;
using Match3.Views;
using R3;
using Zenject;

namespace Match3.Presenters
{
    public sealed class LevelPresenter : IInitializable, IDisposable
    {
        private readonly LevelService _levelService;
        private readonly MoveCounterService _moveCounterService;
        private readonly LevelResultView _levelResultView;
        private readonly MoveCounterView _moveCounterView;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LevelPresenter(
            LevelService levelService,
            MoveCounterService moveCounterService,
            LevelResultView levelResultView,
            MoveCounterView moveCounterView)
        {
            _levelService = levelService;
            _moveCounterService = moveCounterService;
            _levelResultView = levelResultView;
            _moveCounterView = moveCounterView;
        }

        public void Initialize()
        {
            _levelService.State
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case LevelState.Won:
                            _levelResultView.ShowWin();
                            break;
                        case LevelState.Lost:
                            _levelResultView.ShowLose();
                            break;
                        case LevelState.Playing:
                            _levelResultView.Hide();
                            break;
                    }
                })
                .AddTo(_disposables);

            _moveCounterService.MovesLeft
                .Subscribe(movesLeft =>
                {
                    if (_moveCounterService.IsLimited)
                        _moveCounterView.UpdateMovesLeft(movesLeft);
                })
                .AddTo(_disposables);
        }

        public void SetupMoveCounter()
        {
            if (_moveCounterService.IsLimited)
                _moveCounterView.SetLimited(_moveCounterService.MovesLeft.CurrentValue);
            else
                _moveCounterView.SetUnlimited();
        }

        public void Dispose() => _disposables.Dispose();
    }
}
