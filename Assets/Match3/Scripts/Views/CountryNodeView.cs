#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Заголовок страны на карте — не кликабелен, только отображение.
    /// countryIndex назначается Editor-скриптом (StageMapViewEditor).
    /// </summary>
    public sealed class CountryNodeView : MonoBehaviour
    {
        [Header("Адрес на карте")]
        [SerializeField] public int countryIndex;

        [Header("Элементы")]
        [SerializeField] private Image      _icon        = null!;
        [SerializeField] private Text       _nameLabel   = null!;
        [SerializeField] private GameObject _lockOverlay = null!;
        [SerializeField] private Image      _background  = null!;

        /// <summary>
        /// Обновляет визуальное состояние заголовка страны.
        /// </summary>
        public void Refresh(Sprite icon, string countryName, Color sectionColor, bool isUnlocked)
        {
            _icon.sprite      = isUnlocked ? icon : null;
            _nameLabel.text   = isUnlocked ? countryName : "???";
            _background.color = sectionColor;
            _lockOverlay.SetActive(!isUnlocked);
        }
    }
}
