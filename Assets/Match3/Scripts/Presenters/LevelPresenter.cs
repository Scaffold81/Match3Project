#nullable enable

using System;
using Match3.Services.Level;
using Match3.Views;
using R3;
using Zenject;

namespace Match3.Presenters
{
    public sealed class LevelPresenter : IInitializable, IDisposable
    {
        private readonly LevelService       _levelService;
        private readonly ObjectivePresenter _objectivePresenter;
        private readonly LevelResultView    _levelResultView;
        private readonly MoveCounterView    _moveCounterView;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LevelPresenter(
            LevelService       levelService,
            ObjectivePresenter objectivePresenter,
            LevelResultView    levelResultView,
            MoveCounterView    moveCounterView)
        {
            _levelService       = levelService;
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
                .Subscribe(state =>
                {
                    switch (state)
                    {
                        case LevelState.Won:     _levelResultView.ShowWin();  break;
                        case LevelState.Lost:    _levelResultView.ShowLose(); break;
                        case LevelState.Playing: _levelResultView.Hide();     break;
                    }
                })
                .AddTo(_disposables);

            _levelService.MovesLeft
                .Subscribe(movesLeft =>
                {
                    if (_levelService.IsMoveLimited)
                        _moveCounterView.UpdateMovesLeft(movesLeft);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
