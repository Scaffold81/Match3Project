#nullable enable

using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class StageNodeView : MonoBehaviour
    {
        [Header("Адрес на карте")]
        [SerializeField] public int countryIndex;
        [SerializeField] public int stageIndex;

        [Header("Основные элементы")]
        [SerializeField] private Button     _button      = null!;
        [SerializeField] private Image      _icon        = null!;
        [SerializeField] private GameObject _lockOverlay = null!;

        [Header("Звёзды (3 штуки)")]
        [SerializeField] private Image[] _stars = Array.Empty<Image>();

        [Header("Цвета")]
        [SerializeField] private Color _unlockedColor = Color.white;
        [SerializeField] private Color _lockedColor   = new(0.4f, 0.4f, 0.4f, 1f);

        private readonly Subject<StageNodeView> _onClicked = new();
        public Observable<StageNodeView> OnClicked => _onClicked;

        public bool IsUnlocked { get; private set; }

        private void Awake() =>
            _button.onClick.AddListener(() =>
            {
                if (IsUnlocked) _onClicked.OnNext(this);
            });

        private void OnDestroy() => _onClicked.Dispose();

        /// <summary>
        /// Обновляет визуальное состояние ноды.
        /// Icon и позиция задаются в Editor — здесь не трогаем.
        /// </summary>
        public void Refresh(int totalStars, bool isUnlocked)
        {
            IsUnlocked = isUnlocked;

            _icon.color          = isUnlocked ? _unlockedColor : _lockedColor;
            _lockOverlay.SetActive(!isUnlocked);
            _button.interactable = isUnlocked;

            RefreshStars(totalStars);
        }

        private void RefreshStars(int count)
        {
            for (var i = 0; i < _stars.Length; i++)
                _stars[i].enabled = i < count;
        }
    }
}
