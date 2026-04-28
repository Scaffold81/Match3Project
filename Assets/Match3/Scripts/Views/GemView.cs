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
        private GemConfig?    _gemConfig;
        private Tween?        _currentTween;
        private Tween?        _hintTween;

        // ── IGemView ──────────────────────────────────────────────────────────
        public NodeType     GemType      { get; private set; } = NodeType.None;
        public SuperGemType SuperGemType { get; private set; } = SuperGemType.None;
        public Vector2Int   CurrentIndex { get; private set; }
        public GemMatch?    CurrentMatch { get; set; }
        public GemState     CurrentState { get; private set; } = GemState.Still;
        public bool         CanMove      => CurrentState == GemState.Still;

        public void SetConfig(GemConfig gemConfig) => _gemConfig = gemConfig;

        public void Init(Vector2Int index, NodeType type)
        {
            CurrentIndex = index;
            GemType      = type;
            SuperGemType = SuperGemType.None;
            CurrentState = GemState.Still;
            CurrentMatch = null;
            StopHint();
            HideSuperIcon();
        }

        public void MoveTo(Vector2Int newIndex) => CurrentIndex = newIndex;
        public void SetBusy()       => CurrentState = GemState.Busy;
        public void SetStill()      => CurrentState = GemState.Still;
        public void MarkDestroyed() => CurrentState = GemState.Disappearing;

        public void SetSuperGemType(SuperGemType superGemType) =>
            SuperGemType = superGemType;

        /// <summary>
        /// Меняет тип и визуал фишки без пересоздания GameObject — используется при shuffle.
        /// Восстанавливает _image.enabled и CurrentState после PlayDestroy/SetEmpty.
        /// </summary>
        public void SetGemType(NodeType type)
        {
            GemType      = type;
            CurrentState = GemState.Still; // сбрасываем Disappearing после PlayDestroy
            if (_gemConfig == null) return;
            var visual = _gemConfig.GetVisual(type);
            if (visual == null) return;
            _image.sprite  = visual.Sprite;
            _image.color   = visual.Color;
            _image.enabled = true;         // восстанавливаем после SetEmpty()
        }

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
            StopHint();
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

        /// <summary>
        /// Анимация сжатия для shuffle — не вызывает SetEmpty/DestroyGem,
        /// только масштабирует до нуля. Восстановление через SetGemType + PlaySpawn.
        /// </summary>
        public void PlayFold(float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            StopHint();
            _rectTransform.localScale = Vector3.one;
            _currentTween = _rectTransform
                .DOScale(Vector3.zero, duration)
                .SetEase(Ease.InBack)
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayDestroy(float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            StopHint();
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

        public void PlayHint()
        {
            StopHint();
            _hintTween = _rectTransform
                .DOScale(1.15f, 0.45f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        public void StopHint()
        {
            _hintTween?.Kill();
            _hintTween = null;
            _rectTransform.localScale = Vector3.one;
        }

        public void ResetScale()
        {
            _currentTween?.Kill();
            _rectTransform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            _currentTween?.Kill();
            _hintTween?.Kill();
        }
    }
}
