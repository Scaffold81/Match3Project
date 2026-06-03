#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class GameBackgroundView : MonoBehaviour
    {
        [SerializeField] private Image _background = null!;

        public void SetBackground(Sprite? sprite)
        {
            if (sprite == null)
            {
                _background.enabled = false;
                return;
            }

            _background.sprite  = sprite;
            _background.enabled = true;
        }
    }
}
