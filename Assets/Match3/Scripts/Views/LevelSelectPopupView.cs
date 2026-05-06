#nullable enable

using System;
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

        [Header("Цели")]
        [SerializeField] private ObjectiveEntryUI[] _objectiveEntries = Array.Empty<ObjectiveEntryUI>();

        [Header("Награды этапа")]
        [SerializeField] private RewardEntryUI[] _rewardEntries = Array.Empty<RewardEntryUI>();

        [Header("Кнопки")]
        [SerializeField] private Button _playButton  = null!;
        [SerializeField] private Button _closeButton = null!;

        private readonly Subject<Unit> _onPlayClicked  = new();
        private readonly Subject<Unit> _onCloseClicked = new();

        public Observable<Unit> OnPlayClicked  => _onPlayClicked;
        public Observable<Unit> OnCloseClicked => _onCloseClicked;

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
            Sprite?[]       rewardIcons)
        {
            _levelLabel.text        = levelTitle;
            _characterImage.sprite  = characterSprite;
            _characterImage.enabled = characterSprite != null;

            SetupObjectives(objectives, objectiveIcons);
            SetupRewards(stageRewards, rewardIcons);

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

        // ── Private helpers ───────────────────────────────────────────────────

        private void SetupObjectives(ObjectiveData[] objectives, Sprite?[] icons)
        {
            for (var i = 0; i < _objectiveEntries.Length; i++)
            {
                if (i < objectives.Length)
                {
                    _objectiveEntries[i].gameObject.SetActive(true);
                    _objectiveEntries[i].Setup(icons[i], objectives[i].count);
                }
                else
                {
                    _objectiveEntries[i].gameObject.SetActive(false);
                }
            }
        }

        private void SetupRewards(RewardData[] rewards, Sprite?[] icons)
        {
            for (var i = 0; i < _rewardEntries.Length; i++)
            {
                if (i < rewards.Length)
                {
                    _rewardEntries[i].gameObject.SetActive(true);
                    _rewardEntries[i].Setup(rewards[i], i < icons.Length ? icons[i] : null);
                }
                else
                {
                    _rewardEntries[i].gameObject.SetActive(false);
                }
            }
        }
    }

    // ── Вложенные классы для Inspector ────────────────────────────────────────

    [Serializable]
    public sealed class ObjectiveEntryUI
    {
        [SerializeField] private GameObject _root      = null!;
        [SerializeField] private Image      _icon      = null!;
        [SerializeField] private TMP_Text   _countText = null!;

        public GameObject gameObject => _root;

        public void Setup(Sprite? icon, int count)
        {
            _icon.sprite    = icon;
            _icon.enabled   = icon != null;
            _countText.text = count.ToString();
        }
    }

    [Serializable]
    public sealed class RewardEntryUI
    {
        [SerializeField] private GameObject _root      = null!;
        [SerializeField] private Image      _icon      = null!;
        [SerializeField] private TMP_Text   _nameText  = null!;
        [SerializeField] private TMP_Text   _countText = null!;

        public GameObject gameObject => _root;

        public void Setup(RewardData reward, Sprite? icon)
        {
            var hasIcon       = icon != null;
            _icon.sprite      = icon;
            _icon.enabled     = hasIcon;
            _nameText.text    = hasIcon ? string.Empty : GetLabel(reward);
            _nameText.enabled = !hasIcon;
            _countText.text   = $"x{reward.Amount}";
        }

        private static string GetLabel(RewardData reward) =>
            reward.Type == RewardType.Boost
                ? reward.Boost.ToString()
                : reward.Type.ToString();
    }
}
