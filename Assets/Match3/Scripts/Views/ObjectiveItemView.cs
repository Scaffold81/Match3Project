#nullable enable

using Match3.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class ObjectiveItemView : MonoBehaviour
    {
        [SerializeField] private Image    _icon      = null!;
        [SerializeField] private TMP_Text _countText = null!;

        public void Setup(ObjectiveData objective, Sprite? icon)
        {
            _icon.sprite    = icon;
            _icon.enabled   = icon != null;
            _countText.text = objective.count.ToString();
        }
    }
}
