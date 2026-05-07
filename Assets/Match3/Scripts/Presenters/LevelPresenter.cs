#nullable enable

using System;
using Match3.Services.Level;
using Match3.Views;
using R3;
using Zenject;

namespace Match3.Presenters
{
    /// <summary>
    /// Отвечает только за HUD: счётчик ходов и цели уровня.
    /// Игровой цикл (победа / поражение / награды / навигация) — в GameFlowService.
    /// </summary>
    public sealed class LevelPresenter : IInitializable, IDisposable
    {
        private readonly LevelService       _levelService;
        private readonly ObjectivePresenter _objectivePresenter;
        private readonly MoveCounterView    _moveCounterView;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public LevelPresenter(
            LevelService       levelService,
            ObjectivePresenter objectivePresenter,
            MoveCounterView    moveCounterView)
        {
            _levelService       = levelService;
            _objectivePresenter = objectivePresenter;
            _moveCounterView    = moveCounterView;
        }

        public void Initialize()
        {
            if (_levelService.IsMoveLimited)
                _moveCounterView.SetLimited(_levelService.MovesLeft.CurrentValue);
            else
                _moveCounterView.SetUnlimited();

            _objectivePresenter.RenderObjectives(_levelService.Progress.CurrentValue);

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
