#nullable enable

using Match3.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Один элемент списка наград: иконка + количество.
    /// Используется в StageRewardPopupView, LevelSelectPopupView,
    /// CountryCompletePopupView и ResourcePopupView.
    /// </summary>
    public sealed class RewardItemView : MonoBehaviour
    {
        [SerializeField] private Image    _icon       = null!;
        [SerializeField] private TMP_Text _amountText = null!;

        public void Setup(RewardData reward, Sprite? icon)
        {
            _icon.sprite     = icon;
            _icon.enabled    = icon != null;
            _amountText.text = $"+{reward.Amount}";
        }
    }
}
