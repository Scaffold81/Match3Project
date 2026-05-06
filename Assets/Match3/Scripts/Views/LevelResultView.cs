#nullable enable

using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class LevelResultView : MonoBehaviour
    {
        [SerializeField] private GameObject _winPanel       = null!;
        [SerializeField] private GameObject _losePanel      = null!;
        [SerializeField] private Button     _restartButton  = null!;
        [SerializeField] private Button     _nextLevelButton = null!;

        private readonly Subject<Unit> _onRestartClicked   = new();
        private readonly Subject<Unit> _onNextLevelClicked = new();

        public Observable<Unit> OnRestartClicked   => _onRestartClicked;
        public Observable<Unit> OnNextLevelClicked => _onNextLevelClicked;

        private void Awake()
        {
            _winPanel.SetActive(false);
            _losePanel.SetActive(false);

            _restartButton.onClick.AddListener(()   => _onRestartClicked.OnNext(Unit.Default));
            _nextLevelButton.onClick.AddListener(() => _onNextLevelClicked.OnNext(Unit.Default));
        }

        private void OnDestroy()
        {
            _onRestartClicked.Dispose();
            _onNextLevelClicked.Dispose();
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
