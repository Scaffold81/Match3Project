#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Match3.Core.Models;
using Match3.Services;
using Match3.Services.Ads;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Match3.Views
{
    /// <summary>
    /// Универсальный попап получения ресурса за рекламу или монеты.
    /// Автономен — подписывается на ResourcePopupService.OnRequest сам.
    ///
    /// Структура:
    ///   — Персонаж (иконка + диалоговая реплика)
    ///   — Заголовок
    ///   — Список наград: иконка + количество (динамически из RewardItemView[])
    ///   — Кнопка рекламы
    ///   — Кнопка монет (скрыта если CoinPrice == null)
    ///   — Кнопка закрыть
    /// </summary>
    public sealed class ResourcePopupView : MonoBehaviour
    {
        [Header("Анимация")]
        [SerializeField] private CanvasGroup _canvasGroup = null!;

        [Header("Персонаж")]
        [SerializeField] private GameObject _characterBlock = null!;
        [SerializeField] private Image      _characterImage = null!;
        [SerializeField] private TMP_Text   _dialogText     = null!;

        [Header("Награды")]
        [SerializeField] private Transform        _rewardsContainer = null!;
        [SerializeField] private RewardItemView   _rewardItemPrefab = null!;

        [Header("Кнопки")]
        [SerializeField] private Button   _watchAdButton  = null!;
        [SerializeField] private TMP_Text _watchAdLabel   = null!;
        [SerializeField] private Button   _buyCoinsButton = null!;
        [SerializeField] private TMP_Text _buyCoinsLabel  = null!;
        [SerializeField] private Button   _closeButton    = null!;

        private AdService     _adService     = null!;
        private CoinService   _coinService   = null!;
        private RewardService _rewardService = null!;

        private readonly CompositeDisposable _disposables = new();
        private ResourcePopupRequest?        _currentRequest;
        private Tween?                       _tween;

        [Inject]
        public void Construct(
            ResourcePopupService resourcePopupService,
            AdService            adService,
            CoinService          coinService,
            RewardService        rewardService)
        {
            _adService     = adService;
            _coinService   = coinService;
            _rewardService = rewardService;

            resourcePopupService.OnRequest
                .Subscribe(Show)
                .AddTo(_disposables);
        }

        private void Awake()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            _watchAdButton.onClick.AddListener(OnWatchAdClicked);
            _buyCoinsButton.onClick.AddListener(OnBuyCoinsClicked);
            _closeButton.onClick.AddListener(Hide);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _disposables.Dispose();
        }

        private void Show(ResourcePopupRequest request)
        {
            _currentRequest = request;

            ApplyCharacter(request);
            ApplyRewards(request);

            _watchAdLabel.text = request.AdButtonLabel;

            _buyCoinsButton.gameObject.SetActive(request.CoinPrice.HasValue);
            if (request.CoinPrice.HasValue)
                _buyCoinsLabel.text = request.CoinButtonLabel;

            _tween?.Kill();
            _tween = _canvasGroup
                .DOFade(1f, 0.25f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _canvasGroup.interactable   = true;
                    _canvasGroup.blocksRaycasts = true;
                });
        }

        private void ApplyCharacter(ResourcePopupRequest request)
        {
            var hasCharacter = request.CharacterSprite != null;
            _characterBlock.SetActive(hasCharacter);

            if (!hasCharacter) return;

            _characterImage.sprite = request.CharacterSprite;

            // TODO: при подключении локализации читать по request.DialogLocaleId
            _dialogText.text    = request.CharacterDialog;
            _dialogText.enabled = !string.IsNullOrEmpty(request.CharacterDialog);
        }

        private void ApplyRewards(ResourcePopupRequest request)
        {
            // Очищаем предыдущие элементы
            foreach (Transform child in _rewardsContainer)
                Destroy(child.gameObject);

            for (var i = 0; i < request.Rewards.Length; i++)
            {
                var item   = Instantiate(_rewardItemPrefab, _rewardsContainer);
                var icon   = i < request.RewardIcons.Length ? request.RewardIcons[i] : null;
                item.Setup(request.Rewards[i], icon);
            }
        }

        private void Hide()
        {
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            _tween?.Kill();
            _tween = _canvasGroup
                .DOFade(0f, 0.2f)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject);

            _currentRequest = null;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _watchAdButton.interactable  = interactable;
            _buyCoinsButton.interactable = interactable;
            _closeButton.interactable    = interactable;
        }

        private void OnWatchAdClicked()
        {
            WatchAdAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid WatchAdAsync(CancellationToken ct)
        {
            if (_currentRequest == null) return;

            SetButtonsInteractable(false);

            var result = await _adService.ShowRewardedAsync(_currentRequest.AdPlacementId, ct);

            if (result.IsRewarded)
            {
                _currentRequest.NotifySuccess();
                Hide();
                return;
            }

            SetButtonsInteractable(true);

            if (result.FailReason != AdFailReason.None)
                Debug.LogError($"[ResourcePopupView] Ad failed: {result.FailReason}");
        }

        private void OnBuyCoinsClicked()
        {
            if (_currentRequest?.CoinPrice == null) return;

            if (!_coinService.TrySpend(_currentRequest.CoinPrice.Value))
                return;

            _rewardService.GrantAll(_currentRequest.Rewards);
            _currentRequest.NotifySuccess();
            Hide();
        }
    }
}
