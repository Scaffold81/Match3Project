#nullable enable

using System;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class LevelSelectPopupView : MonoBehaviour
    {
        [SerializeField] private GameObject  _root           = null!;
        [SerializeField] private CanvasGroup _canvasGroup    = null!;
        [SerializeField] private TMP_Text    _stageNameLabel = null!;
        [SerializeField] private Button      _closeButton    = null!;

        [SerializeField] private LevelButtonEntry[] _levelEntries = Array.Empty<LevelButtonEntry>();

        private readonly Subject<int>  _onLevelClicked = new();
        private readonly Subject<Unit> _onCloseClicked = new();

        public Observable<int>  OnLevelClicked => _onLevelClicked;
        public Observable<Unit> OnCloseClicked => _onCloseClicked;

        private Tween? _tween;

        private void Awake()
        {
            _closeButton.onClick.AddListener(() => _onCloseClicked.OnNext(Unit.Default));

            for (var i = 0; i < _levelEntries.Length; i++)
            {
                var levelIndex = i;
                _levelEntries[i].Button.onClick.AddListener(() =>
                    _onLevelClicked.OnNext(levelIndex));
            }

            _root.SetActive(false);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _onLevelClicked.Dispose();
            _onCloseClicked.Dispose();
        }

        public void Show(string stageName, int[] starsPerLevel, bool[] isUnlocked)
        {
            _stageNameLabel.text = stageName;

            for (var i = 0; i < _levelEntries.Length; i++)
            {
                var unlocked = i < isUnlocked.Length && isUnlocked[i];
                var stars    = i < starsPerLevel.Length ? starsPerLevel[i] : 0;
                _levelEntries[i].Setup(i + 1, stars, unlocked);
            }

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
    }

    [Serializable]
    public sealed class LevelButtonEntry
    {
        [SerializeField] public Button     Button      = null!;
        [SerializeField] public TMP_Text   LevelLabel  = null!;
        [SerializeField] public Image[]    Stars       = Array.Empty<Image>();
        [SerializeField] public GameObject LockOverlay = null!;

        public void Setup(int levelNumber, int stars, bool isUnlocked)
        {
            if (LevelLabel  != null) LevelLabel.text      = $"Уровень {levelNumber}";
            if (Button      != null) Button.interactable  = isUnlocked;
            if (LockOverlay != null) LockOverlay.SetActive(!isUnlocked);

            for (var i = 0; i < Stars.Length; i++)
                if (Stars[i] != null) Stars[i].enabled = i < stars;
        }
    }
}
