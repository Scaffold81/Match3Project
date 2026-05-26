#nullable enable

using System.Collections.Generic;
using DG.Tweening;
using Match3.Core.Enums;
using Match3.Core.Models;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class LevelSelectPopupView : MonoBehaviour
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
        [SerializeField] private Transform          _objectiveContainer = null!;
        [SerializeField] private ObjectiveItemView  _objectivePrefab    = null!;

        [Header("Награды этапа")]
        [SerializeField] private Transform      _rewardContainer = null!;
        [SerializeField] private RewardItemView _rewardPrefab    = null!;

        [Header("Кнопки")]
        [SerializeField] private Button _playButton  = null!;
        [SerializeField] private Button _closeButton = null!;

        private readonly Subject<Unit> _onPlayClicked  = new();
        private readonly Subject<Unit> _onCloseClicked = new();

        public Observable<Unit> OnPlayClicked  => _onPlayClicked;
        public Observable<Unit> OnCloseClicked => _onCloseClicked;

        private readonly List<ObjectiveItemView> _spawnedObjectives = new();
        private readonly List<RewardItemView>    _spawnedRewards    = new();

        private Tween? _tween;

        private void Awake()
        {
            _playButton.onClick.AddListener(()  => _onPlayClicked.OnNext(Unit.Default));
            _closeButton.onClick.AddListener(() => _onCloseClicked.OnNext(Unit.Default));
            _root.SetActive(false);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _onPlayClicked.Dispose();
            _onCloseClicked.Dispose();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(
            string          levelTitle,
            Sprite?         characterSprite,
            ObjectiveData[] objectives,
            Sprite?[]       objectiveIcons,
            RewardData[]    stageRewards,
            Sprite?[]       rewardIcons,
            StorySlide?     storySlide = null)
        {
            _levelLabel.text        = levelTitle;
            _characterImage.sprite  = characterSprite;
            _characterImage.enabled = characterSprite != null;

            ApplyStory(storySlide);
            SpawnObjectives(objectives, objectiveIcons);
            SpawnRewards(stageRewards, rewardIcons);

            _root.SetActive(true);
            _canvasGroup.alpha = 0f;

            _tween?.Kill();
            _tween = _canvasGroup
                .DOFade(1f, 0.25f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        public void Hide()
        {
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

        // ── Spawn ─────────────────────────────────────────────────────────────

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

        private void SpawnRewards(RewardData[] rewards, Sprite?[] icons)
        {
            ClearSpawned(_spawnedRewards);

            for (var i = 0; i < rewards.Length; i++)
            {
                var item = Instantiate(_rewardPrefab, _rewardContainer);
                var icon = i < icons.Length ? icons[i] : null;
                item.Setup(rewards[i], icon);
                _spawnedRewards.Add(item);
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
