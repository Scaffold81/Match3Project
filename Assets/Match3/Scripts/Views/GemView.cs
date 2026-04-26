#nullable enable

using System;
using DG.Tweening;
using Match3.Core;
using Match3.Core.Enums;
using Match3.Configs;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class GemView : MonoBehaviour, IGemView
    {
        [SerializeField] private Image _image = null!;

        private RectTransform _rectTransform = null!;
        private Tweener?      _currentTween;

        // ── IGemView ─────────────────────────────────────────────────────────
        public NodeType   GemType      { get; private set; } = NodeType.None;
        public Vector2Int CurrentIndex { get; private set; }
        public GemMatch?  CurrentMatch { get; set; }
        public GemState   CurrentState { get; private set; } = GemState.Still;
        public bool       CanMove      => CurrentState == GemState.Still;

        public void Init(Vector2Int index, NodeType type)
        {
            CurrentIndex = index;
            GemType      = type;
            CurrentState = GemState.Still;
            CurrentMatch = null;
        }

        public void MoveTo(Vector2Int newIndex) => CurrentIndex = newIndex;
        public void SetBusy()       => CurrentState = GemState.Busy;
        public void SetStill()      => CurrentState = GemState.Still;
        public void MarkDestroyed() => CurrentState = GemState.Disappearing;

        // ── Visual ───────────────────────────────────────────────────────────
        public bool          IsEmpty       => GemType == NodeType.None;
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            _rectTransform       = GetComponent<RectTransform>();
            _image.raycastTarget = false;
            _image.enabled       = false;
        }

        public void SetVisual(NodeType nodeType, GemVisualData visual)
        {
            GemType        = nodeType;
            _image.sprite  = visual.Sprite;
            _image.color   = visual.Color;
            _image.enabled = true;
        }

        public void SetEmpty()
        {
            GemType        = NodeType.None;
            _image.sprite  = null;
            _image.enabled = false;
        }

        // ── Анимации ──────────────────────────────────────────────────────────

        /// <summary>
        /// Перемещение по anchoredPosition (когда гем находится в GemContainer).
        /// </summary>
        public void PlayMoveTo(Vector2 targetAnchoredPos, float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            SetBusy();
            _currentTween = _rectTransform
                .DOAnchorPos(targetAnchoredPos, duration)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    SetStill();
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// Перемещение в мировых координатах — используется когда гем
        /// находится в DragLayer (оверлей поверх всего).
        /// </summary>
        public void PlayMoveToWorldPos(Vector3 worldPos, float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            SetBusy();
            _currentTween = _rectTransform
                .DOMove(worldPos, duration)
                .SetEase(Ease.InOutQuad)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    SetStill();
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// Падение в мировых координатах — используется когда гем
        /// находится в DragLayer во время гравитации.
        /// </summary>
        public void PlayFallToWorldPos(Vector3 worldPos, float duration, Action? onLanded = null)
        {
            _currentTween?.Kill();
            SetBusy();
            _currentTween = _rectTransform
                .DOMove(worldPos, duration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    SetStill();
                    onLanded?.Invoke();
                });
        }

        public void PlayBounce(float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            SetBusy();
            _currentTween = _rectTransform
                .DOPunchPosition(Vector3.up * 8f, duration, 1, 0.5f)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    SetStill();
                    onComplete?.Invoke();
                });
        }

        public void PlaySelect(float duration, float targetScale)
        {
            _currentTween?.Kill();
            _currentTween = _rectTransform
                .DOScale(targetScale, duration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        public void PlayDeselect(float duration)
        {
            _currentTween?.Kill();
            _currentTween = _rectTransform
                .DOScale(Vector3.one, duration)
                .SetEase(Ease.OutCubic)
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

        public void PlaySpawn(float duration)
        {
            _currentTween?.Kill();
            _rectTransform.localScale = Vector3.zero;
            _currentTween = _rectTransform
                .DOScale(Vector3.one, duration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        public void PlaySwapPulse(float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            _currentTween = _rectTransform
                .DOPunchScale(Vector3.one * 0.12f, duration, 1, 0.5f)
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void ResetScale()
        {
            _currentTween?.Kill();
            _rectTransform.localScale = Vector3.one;
        }

        private void OnDestroy() => _currentTween?.Kill();
    }
}
