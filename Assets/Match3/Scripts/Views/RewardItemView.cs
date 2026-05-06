#nullable enable

using Match3.Core.Enums;
using Match3.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class RewardItemView : MonoBehaviour
    {
        [SerializeField] private Image    _icon      = null!;
        [SerializeField] private TMP_Text _nameText  = null!;
        [SerializeField] private TMP_Text _countText = null!;

        public void Setup(RewardData reward, Sprite? icon)
        {
            var hasIcon       = icon != null;
            _icon.sprite      = icon;
            _icon.enabled     = hasIcon;
            _nameText.text    = hasIcon ? string.Empty : GetLabel(reward);
            _nameText.enabled = !hasIcon;
            _countText.text   = $"x{reward.Amount}";
        }

        private static string GetLabel(RewardData reward) =>
            reward.Type == RewardType.Boost
                ? reward.Boost.ToString()
                : reward.Type.ToString();
    }
}
