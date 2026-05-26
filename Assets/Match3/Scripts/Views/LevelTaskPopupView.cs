#nullable enable

using System.Collections.Generic;
using DG.Tweening;
using Match3.Core.Models;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Попап с заданием уровня.
    /// Закрывается по кнопке Play или по тапу на любой части попапа.
    /// </summary>
    public sealed class LevelTaskPopupView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Корень попапа")]
        [SerializeField] private GameObject  _root        = null!;
        [SerializeField] private CanvasGroup _canvasGroup = null!;

        [Header("Шапка")]
        [SerializeField] private TMP_Text _levelLabel     = null!;
        [SerializeField] private Image    _characterImage = null!;

        [Header("История")]
        [SerializeField] private GameObject _storyPanel = null!;
        [SerializeField] private Image      _storyImage = null!;
        [SerializeField] private TMP_Text   _storyText  = null!;

        [Header("Цели")]
        [SerializeField] private Transform         _objectiveContainer = null!;
        [SerializeField] private ObjectiveItemView _objectivePrefab    = null!;

        [Header("Кнопка")]
        [SerializeField] private Button _playButton = null!;

        private readonly Subject<Unit> _onPlayClicked = new();
        public Observable<Unit> OnPlayClicked => _onPlayClicked;

        private readonly List<ObjectiveItemView> _spawnedObjectives = new();

        private Tween? _tween;
        private bool   _interactable;

        private void Awake()
        {
            _playButton.onClick.AddListener(TriggerPlay);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _onPlayClicked.Dispose();
        }

        // ── IPointerClickHandler — тап в любом месте попапа ──────────────────

        public void OnPointerClick(PointerEventData eventData)
        {
            TriggerPlay();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(
            string          levelTitle,
            Sprite?         characterSprite,
            ObjectiveData[] objectives,
            Sprite?[]       objectiveIcons,
            StorySlide?     storySlide = null)
        {
            _levelLabel.text        = levelTitle;
            _characterImage.sprite  = characterSprite;
            _characterImage.enabled = characterSprite != null;

            ApplyStory(storySlide);
            SpawnObjectives(objectives, objectiveIcons);

            _interactable      = false;
            _root.SetActive(true);
            _canvasGroup.alpha = 0f;

            _tween?.Kill();
            _tween = _canvasGroup
                .DOFade(1f, 0.25f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnComplete(() => _interactable = true);
        }

        public void Hide()
        {
            _interactable = false;
            _tween?.Kill();
            _tween = _canvasGroup
                .DOFade(0f, 0.2f)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .OnComplete(() => _root.SetActive(false));
        }

        // ── Story ─────────────────────────────────────────────────────────────

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
            var text        = slide.FallbackText ?? string.Empty;
            _storyText.text    = text;
            _storyText.enabled = !string.IsNullOrEmpty(text);
        }

        // ── Приватное ─────────────────────────────────────────────────────────

        private void TriggerPlay()
        {
            if (!_interactable) return;
            _interactable = false;
            _onPlayClicked.OnNext(Unit.Default);
        }

        private void SpawnObjectives(ObjectiveData[] objectives, Sprite?[] icons)
        {
            ClearSpawned(_spawnedObjectives);

            for (var i = 0; i < objectives.Length; i++)
            {
                var item = Instantiate(_objectivePrefab, _objectiveContainer);
                var icon = i < icons.Length ? icons[i] : null;
                item.Setup(objectives[i], icon);
                _spawnedObjectives.Add(item);
            }
        }

        private static void ClearSpawned<T>(List<T> list) where T : MonoBehaviour
        {
            foreach (var item in list)
                if (item != null) Destroy(item.gameObject);
            list.Clear();
        }
    }
}
