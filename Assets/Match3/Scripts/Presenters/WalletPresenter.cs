#nullable enable

using System;
using Match3.Services;
using Match3.Views;
using R3;
using Zenject;

namespace Match3.Presenters
{
    /// <summary>
    /// Связывает CoinService, LivesService и WalletView.
    /// Живёт в ProjectContext — один на всю игру.
    /// </summary>
    public sealed class WalletPresenter : IInitializable, IDisposable
    {
        private readonly CoinService  _coinService;
        private readonly LivesService _livesService;
        private readonly WalletView   _view;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public WalletPresenter(
            CoinService  coinService,
            LivesService livesService,
            WalletView   view)
        {
            _coinService  = coinService;
            _livesService = livesService;
            _view         = view;
        }

        public void Initialize()
        {
            _coinService.Coins
                .Subscribe(amount => _view.SetCoins(amount))
                .AddTo(_disposables);

            _livesService.Lives
                .Subscribe(current => _view.SetLives(current, _livesService.MaxLives))
                .AddTo(_disposables);

            _livesService.TimeUntilNextLife
                .Subscribe(remaining =>
                {
                    if (remaining == TimeSpan.Zero)
                        _view.HideTimer();
                    else
                        _view.ShowTimer(remaining);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
