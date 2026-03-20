#nullable enable

using System;
using DG.Tweening;
using Match3.Configs;
using Match3.Core.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    [RequireComponent(typeof(Image))]
    public sealed class GemView : MonoBehaviour
    {
        private Image _image = null!;
        private RectTransform _rectTransform = null!;
        private Tweener? _currentTween;

        public NodeType NodeType { get; private set; }
        public RectTransform RectTransform => _rectTransform;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _rectTransform = GetComponent<RectTransform>();
        }

        public void Setup(NodeType nodeType, GemVisualData visualData)
        {
            NodeType = nodeType;
            _image.sprite = visualData.Sprite;
            _image.color = visualData.Color;
        }

        public void PlaySwap(Vector2 targetPos, float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            _currentTween = _rectTransform
                .DOAnchorPos(targetPos, duration)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayReturn(Vector2 originalPos, float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            _currentTween = _rectTransform
                .DOAnchorPos(originalPos, duration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayFall(Vector2 targetPos, float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            _currentTween = _rectTransform
                .DOAnchorPos(targetPos, duration)
                .SetEase(Ease.InQuad)
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke());
        }

        public void PlayDestroy(float duration, Action? onComplete = null)
        {
            _currentTween?.Kill();
            _currentTween = _rectTransform
                .DOScale(Vector3.zero, duration)
                .SetEase(Ease.InBack)
                .SetLink(gameObject)
                .OnComplete(() => onComplete?.Invoke());
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

        private void OnDestroy() => _currentTween?.Kill();
    }
}
