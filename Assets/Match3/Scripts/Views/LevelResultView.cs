#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services;
using Match3.Services.Ads;
using Match3.Services.SceneManagement;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Match3.Views
{
    /// <summary>
    /// Панель поражения. Живёт в сцене — биндинг не нужен, Zenject инжектирует через Construct().
    ///
    /// Содержимое (референс Pirate Treasures):
    ///   — Персонаж (грустный)
    ///   — Текст "Не хватило ходов!"
    ///   — Количество оставшихся жизней
    ///   — Кнопка "Попробовать ещё" → TrySpendLife() + перезапуск       (видна когда жизней > 0)
    ///   — Кнопка "👁 Смотреть рекламу" → реклама → жизни + перезапуск  (видна когда жизней = 0)
    ///   — Кнопка "На карту"        → выход (жизнь НЕ тратится)
    /// </summary>
    public sealed class LevelResultView : MonoBehaviour
    {
        [Header("Анимация")]
        [SerializeField] private CanvasGroup _canvasGroup = null!;

        [Header("Контент")]
        [SerializeField] private Image    _characterImage = null!;
        [SerializeField] private TMP_Text _titleText      = null!;
        [SerializeField] private TMP_Text _livesText      = null!;

        [Header("История")]
        [SerializeField] private GameObject _storyPanel = null!;
        [SerializeField] private Image      _storyImage = null!;
        [SerializeField] private TMP_Text   _storyText  = null!;

        [Header("Кнопки")]
        [SerializeField] private Button _restartButton   = null!;
        [SerializeField] private Button _watchAdButton   = null!;
        [SerializeField] private Button _backToMapButton = null!;

        private LivesService         _livesService        = null!;
        private AdService            _adService           = null!;
        private ISceneManagerService _sceneManagerService = null!;

        private readonly CompositeDisposable _disposables = new();

        private Tween? _tween;

        [Inject]
        public void Construct(
            GameFlowService      gameFlowService,
            LivesService         livesService,
            AdService            adService,
            ISceneManagerService sceneManagerService)
        {
            _livesService        = livesService;
            _adService           = adService;
            _sceneManagerService = sceneManagerService;

            gameFlowService.OnLevelLost
                .Take(1)
                .Subscribe(payload => Show(payload.CharacterSprite, payload.Story))
                .AddTo(_disposables);

            livesService.Lives
                .Subscribe(lives => RefreshButtons(lives))
                .AddTo(_disposables);
        }

        private void Awake()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            _titleText.text = "Не хватило ходов!";

            _restartButton.onClick.AddListener(OnRestartClicked);
            _watchAdButton.onClick.AddListener(OnWatchAdClicked);
            _backToMapButton.onClick.AddListener(OnBackToMapClicked);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _disposables.Dispose();
        }

        // ── Приватное ─────────────────────────────────────────────────────────

        private void Show(Sprite? characterSprite, StorySlide? storySlide)
        {
            _characterImage.sprite  = characterSprite;
            _characterImage.enabled = characterSprite != null;

            ApplyStory(storySlide);
            RefreshButtons(_livesService.Lives.CurrentValue);

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

        private void ApplyStory(StorySlide? slide)
        {
            if (slide == null || !slide.HasContent)
            {
                _storyPanel.SetActive(false);
                return;
            }

            _storyPanel.SetActive(true);
            _storyImage.sprite  = slide.Image;
            _storyImage.enabled = slide.Image != null;

            // TODO: когда подключится локализация — читать по slide.LocalizationId
            var text           = slide.FallbackText ?? string.Empty;
            _storyText.text    = text;
            _storyText.enabled = !string.IsNullOrEmpty(text);
        }

        private void RefreshButtons(int lives)
        {
            var hasLives = lives > 0;
            _restartButton.gameObject.SetActive(hasLives);
            _watchAdButton.gameObject.SetActive(!hasLives);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            _restartButton.interactable   = interactable;
            _watchAdButton.interactable   = interactable;
            _backToMapButton.interactable = interactable;
        }

        // ── Обработчики ───────────────────────────────────────────────────────

        private void OnRestartClicked()
        {
            _livesService.TrySpendLife();
            _sceneManagerService.LoadSceneAsync(SceneId.Game);
        }

        private void OnWatchAdClicked()
        {
            WatchAdAndRestartAsync(this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid WatchAdAndRestartAsync(CancellationToken ct)
        {
            SetButtonsInteractable(false);

            var result = await _adService.ShowRewardedAsync(AdPlacementId.RewardedLives, ct);

            if (result.IsRewarded)
            {
                _sceneManagerService.LoadSceneAsync(SceneId.Game);
                return;
            }

            SetButtonsInteractable(true);
        }

        private void OnBackToMapClicked()
        {
            _sceneManagerService.LoadSceneAsync(SceneId.StageMap);
        }
    }
}
