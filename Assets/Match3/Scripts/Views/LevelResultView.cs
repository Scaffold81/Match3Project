#nullable enable

using System;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class LevelResultView : MonoBehaviour
    {
        [SerializeField] private GameObject _winPanel = null!;
        [SerializeField] private GameObject _losePanel = null!;
        [SerializeField] private Button _restartButton = null!;
        [SerializeField] private Button _nextLevelButton = null!;

        public event Action? OnRestartClicked;
        public event Action? OnNextLevelClicked;

        private void Awake()
        {
            _winPanel.SetActive(false);
            _losePanel.SetActive(false);

            _restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
            _nextLevelButton.onClick.AddListener(() => OnNextLevelClicked?.Invoke());
        }

        public void ShowWin()
        {
            _winPanel.SetActive(true);
            _losePanel.SetActive(false);
        }

        public void ShowLose()
        {
            _losePanel.SetActive(true);
            _winPanel.SetActive(false);
        }

        public void Hide()
        {
            _winPanel.SetActive(false);
            _losePanel.SetActive(false);
        }
    }
}
