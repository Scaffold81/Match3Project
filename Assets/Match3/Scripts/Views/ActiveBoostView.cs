#nullable enable

using System;
using DG.Tweening;
using Match3.Core.Enums;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Шапка — показывает активный буст.
    /// Иконка вылетает из рюкзака сюда анимацией DOTween.
    /// Нажатие на иконку отменяет буст.
    /// </summary>
    public sealed class ActiveBoostView : MonoBehaviour
    {
        [SerializeField] private GameObject  _container  = null!;
        [SerializeField] private Image       _icon       = null!;
        [SerializeField] private Button      _cancelButton = null!;
        [SerializeField] private CanvasGroup _canvasGroup  = null!;

        private readonly Subject<Unit> _onCancelClicked = new();
        public Observable<Unit> OnCancelClicked => _onCancelClicked;

        private Tween? _tween;

        private void Awake()
        {
            _cancelButton.onClick.AddListener(() => _onCancelClicked.OnNext(Unit.Default));
            _container.SetActive(false);
        }

        private void OnDestroy()
        {
            _tween?.Kill();
            _onCancelClicked.Dispose();
        }

        /// <summary>
        /// Показывает иконку с анимацией вылета из позиции рюкзака.
        /// </summary>
        public void ShowBoost(Sprite icon, Vector3 fromWorldPos)
        {
            _icon.sprite = icon;
            _container.SetActive(true);
            _canvasGroup.alpha = 0f;

            var targetPos = _container.transform.position;
            _container.transform.position = fromWorldPos;

            _tween?.Kill();
            _tween = DOTween.Sequence()
                .Append(_container.transform.DOMove(targetPos, 0.35f).SetEase(Ease.OutBack))
                .Join(_canvasGroup.DOFade(1f, 0.25f))
                .SetLink(gameObject);
        }

        /// <summary>
        /// Скрывает иконку с анимацией исчезновения.
        /// </summary>
        public void HideBoost()
        {
            _tween?.Kill();
            _tween = _canvasGroup
                .DOFade(0f, 0.2f)
                .SetLink(gameObject)
                .OnComplete(() => _container.SetActive(false));
        }
    }
}
