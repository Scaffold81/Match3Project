#nullable enable

using TMPro;
using UnityEngine;

namespace Match3.Views
{
    public sealed class MoveCounterView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _movesLeftText = null!;
        [SerializeField] private GameObject _unlimitedIndicator = null!;

        public void SetLimited(int movesLeft)
        {
            _unlimitedIndicator.SetActive(false);
            _movesLeftText.gameObject.SetActive(true);
            _movesLeftText.text = movesLeft.ToString();
        }

        public void SetUnlimited()
        {
            _unlimitedIndicator.SetActive(true);
            _movesLeftText.gameObject.SetActive(false);
        }

        public void UpdateMovesLeft(int movesLeft)
        {
            _movesLeftText.text = movesLeft.ToString();
        }
    }
}
