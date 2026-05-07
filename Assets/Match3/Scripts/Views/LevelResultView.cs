#nullable enable

using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Показывает панель поражения с кнопками Restart и Back to Map.
    /// Победа обрабатывается через GameFlowService (попапы задания / награды).
    /// </summary>
    public sealed class LevelResultView : MonoBehaviour
    {
        [SerializeField] private GameObject _losePanel       = null!;
        [SerializeField] private Button     _restartButton   = null!;
        [SerializeField] private Button     _backToMapButton = null!;

        private readonly Subject<Unit> _onRestartClicked   = new();
        private readonly Subject<Unit> _onBackToMapClicked = new();

        public Observable<Unit> OnRestartClicked   => _onRestartClicked;
        public Observable<Unit> OnBackToMapClicked => _onBackToMapClicked;

        private void Awake()
        {
            _losePanel.SetActive(false);
            _restartButton.onClick.AddListener(()   => _onRestartClicked.OnNext(Unit.Default));
            _backToMapButton.onClick.AddListener(() => _onBackToMapClicked.OnNext(Unit.Default));
        }

        private void OnDestroy()
        {
            _onRestartClicked.Dispose();
            _onBackToMapClicked.Dispose();
        }

        public void ShowLose() => _losePanel.SetActive(true);

        public void Hide() => _losePanel.SetActive(false);
    }
}
