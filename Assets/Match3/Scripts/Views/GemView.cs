#nullable enable

using System;
using DG.Tweening;
using Match3.Configs;
using Match3.Core.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Match3.Views
{
    public sealed class GemView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image _image;
        private RectTransform _rectTransform;
        private Tweener?      _currentTween;

        public NodeType      NodeType  { get; private set; } = NodeType.None;
        public bool          IsEmpty   => NodeType == NodeType.None;
        public RectTransform RectTransform => _rectTransform;

        public event Action? OnClicked;

        private void Awake()
        {
            _rectTransform       = GetComponent<RectTransform>();
            _image.raycastTarget = true;
            if (_image != null)
                _image.color = Color.clear;
        }

        public void SetVisual(NodeType nodeType, GemVisualData visual)
        {
            NodeType      = nodeType;
            _image.sprite = visual.Sprite;
            _image.color  = visual.Color;
        }

        public void SetEmpty()
        {
            NodeType      = NodeType.None;
            _image.sprite = null;
            _image.color  = Color.clear;
        }

        // Обменивает визуал с другой ячейкой
        public void SwapWith(GemView other)
        {
            var tempNodeType = NodeType;
            var tempSprite   = _image.sprite;
            var tempColor    = _image.color;

            NodeType        = other.NodeType;
            _image.sprite   = other._image.sprite;
            _image.color    = other._image.color;

            other.NodeType      = tempNodeType;
            other._image.sprite = tempSprite;
            other._image.color  = tempColor;
        }

        // Копирует визуал из другой ячейки (без изменения other)
        public void CopyVisualFrom(GemView other)
        {
            NodeType      = other.NodeType;
            _image.sprite = other._image.sprite;
            _image.color  = other._image.color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (IsEmpty) return;
            Debug.LogWarning($"[GemView] Clicked: {NodeType} pos={_rectTransform.anchoredPosition}");
            OnClicked?.Invoke();
        }

        // Уничтожение — scale → 0, затем SetEmpty
        public void PlayDestroy(float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
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

        // Появление — scale 0 → 1
        public void PlaySpawn(float duration)
        {
            _currentTween?.Kill();
            _rectTransform.localScale = Vector3.zero;
            _currentTween = _rectTransform
                .DOScale(Vector3.one, duration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
        }

        // Пульс при свопе
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

        private void OnDestroy()
        {
            _currentTween?.Kill();
            OnClicked = null;
        }
    }
}
