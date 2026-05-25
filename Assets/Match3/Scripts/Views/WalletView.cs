#nullable enable

using System;
using Match3.Services;
using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Match3.Views
{
    public sealed class WalletView : MonoBehaviour
    {
        [SerializeField] private TMP_Text   _coinsText      = null!;
        [SerializeField] private TMP_Text   _livesText      = null!;
        [SerializeField] private TMP_Text   _timerText      = null!;
        [SerializeField] private GameObject _timerContainer = null!;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(CoinService coinService, LivesService livesService)
        {
            coinService.Coins
                .Subscribe(amount => _coinsText.text = amount.ToString())
                .AddTo(_disposables);

            livesService.Lives
                .Subscribe(current => _livesText.text = $"{current}/{livesService.MaxLives}")
                .AddTo(_disposables);

            livesService.TimeUntilNextLife
                .Subscribe(remaining =>
                {
                    if (remaining == TimeSpan.Zero)
                    {
                        _timerContainer.SetActive(false);
                    }
                    else
                    {
                        _timerContainer.SetActive(true);
                        _timerText.text = FormatTime(remaining);
                    }
                })
                .AddTo(_disposables);
        }

        private void OnDestroy() => _disposables.Dispose();

        private static string FormatTime(TimeSpan t) =>
            t.TotalHours >= 1
                ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes:D2}:{t.Seconds:D2}";
    }
}
