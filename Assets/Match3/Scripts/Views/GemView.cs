#nullable enable

using System;
using DG.Tweening;
using Match3.Configs;
using Match3.Core;
using Match3.Core.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class GemView : MonoBehaviour, IGemView
    {
        [SerializeField] private Image _image     = null!;
        [SerializeField] private Image _superIcon = null!;

        private RectTransform _rectTransform = null!;
        private Tween?        _currentTween; // Tween — базовый класс для Tweener и Sequence

        // ── IGemView ──────────────────────────────────────────────────────────
        public NodeType     GemType      { get; private set; } = NodeType.None;
        public SuperGemType SuperGemType { get; private set; } = SuperGemType.None;
        public Vector2Int   CurrentIndex { get; private set; }
        public GemMatch?    CurrentMatch { get; set; }
        public GemState     CurrentState { get; private set; } = GemState.Still;
        public bool         CanMove      => CurrentState == GemState.Still;

        public void Init(Vector2Int index, NodeType type)
        {
            CurrentIndex = index;
            GemType      = type;
            SuperGemType = SuperGemType.None;
            CurrentState = GemState.Still;
            CurrentMatch = null;
            HideSuperIcon();
        }

        public void MoveTo(Vector2Int newIndex) => CurrentIndex = newIndex;
        public void SetBusy()       => CurrentState = GemState.Busy;
        public void SetStill()      => CurrentState = GemState.Still;
        public void MarkDestroyed() => CurrentState = GemState.Disappearing;

        public void SetSuperGemType(SuperGemType superGemType) =>
            SuperGemType = superGemType;

        // ── Visual ────────────────────────────────────────────────────────────
        public bool          IsEmpty       => GemType == NodeType.None;
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            _rectTransform       = GetComponent<RectTransform>();
            _image.raycastTarget = false;
            _image.enabled       = false;
            HideSuperIcon();
        }

        public void SetVisual(NodeType nodeType, GemVisualData visual)
        {
            GemType        = nodeType;
            _image.sprite  = visual.Sprite;
            _image.color   = visual.Color;
            _image.enabled = true;
        }

        public void SetSuperIcon(SuperGemIconData iconData)
        {
            SuperGemType       = iconData.SuperGemType;
            _superIcon.sprite  = iconData.Icon;
            _superIcon.color   = iconData.Tint;
            _superIcon.enabled = true;
        }

        private void HideSuperIcon()
        {
            if (_superIcon != null)
                _superIcon.enabled = false;
        }

        public void SetEmpty()
        {
            GemType        = NodeType.None;
            SuperGemType   = SuperGemType.None;
            _image.sprite  = null;
            _image.enabled = false;
            HideSuperIcon();
        }

        // ── Анимации ──────────────────────────────────────────────────────────

        public void PlayMoveTo(Vector2 targetAnchoredPos, float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            SetBusy();
            _currentTween = _rectTransform
                .DOAnchorPos(targetAnchoredPos, duration)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject)
                .OnComplete(() => { SetStill(); onComplete?.Invoke(); });
        }

        public void PlayMoveToWorldPos(Vector3 worldPos, float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            SetBusy();
            _currentTween = _rectTransform
                .DOMove(worldPos, duration)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject)
                .OnComplete(() => { SetStill(); onComplete?.Invoke(); });
        }

        public void PlayFallToWorldPos(Vector3 worldPos, float duration, Action? onLanded = null)
        {
            _currentTween?.Kill();
            SetBusy();
            _currentTween = _rectTransform
                .DOMove(worldPos, duration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .OnComplete(() => { SetStill(); onLanded?.Invoke(); });
        }

        public void PlaySpawn(float duration)
        {
            _currentTween?.Kill();
            _rectTransform.localScale = Vector3.zero;
            _currentTween = _rectTransform
                .DOScale(Vector3.one, duration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        public void PlayDestroy(float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            MarkDestroyed();
            _rectTransform.localScale = Vector3.one;
            _currentTween = _rectTransform
                .DOScale(Vector3.zero, duration)
                .SetEase(Ease.InBack)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    SetEmpty();
                    ResetScale();
                    onComplete?.Invoke();
                });
        }

        public void PlaySuperSpawn(float duration)
        {
            _currentTween?.Kill();
            _rectTransform.localScale = Vector3.zero;
            _currentTween = DOTween.Sequence()
                .Append(_rectTransform.DOScale(1.3f, duration * 0.6f).SetEase(Ease.OutBack))
                .Append(_rectTransform.DOScale(1f,   duration * 0.4f).SetEase(Ease.InOutQuad))
                .SetLink(gameObject);
        }

        public void ResetScale()
        {
            _currentTween?.Kill();
            _rectTransform.localScale = Vector3.one;
        }

        private void OnDestroy() => _currentTween?.Kill();
    }
}
