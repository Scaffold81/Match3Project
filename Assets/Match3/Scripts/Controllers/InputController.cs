#nullable enable

using Match3.Configs;
using Match3.Presenters;
using Match3.Views;
using UnityEngine;
using Zenject;

namespace Match3.Controllers
{
    public sealed class InputController : MonoBehaviour
    {
        private SwapPresenter _swapPresenter = null!;
        private BoardView _boardView = null!;
        private BoardConfig _boardConfig = null!;
        private Canvas _canvas = null!;

        [Inject]
        public void Construct(
            SwapPresenter swapPresenter,
            BoardView boardView,
            BoardConfig boardConfig)
        {
            _swapPresenter = swapPresenter;
            _boardView = boardView;
            _boardConfig = boardConfig;
        }

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _boardView.RectTransform,
                    Input.mousePosition,
                    _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera,
                    out var localPoint)) return;

            var cell = LocalPointToCell(localPoint);
            if (cell.HasValue)
                _swapPresenter.OnCellTapped(cell.Value);
        }

        private Vector2Int? LocalPointToCell(Vector2 localPoint)
        {
            var step = _boardConfig.CellSize + _boardConfig.CellSpacing;
            if (step <= 0f) return null;

            var col = Mathf.RoundToInt(localPoint.x / step);
            var row = Mathf.RoundToInt(-localPoint.y / step);

            if (row < 0 || col < 0) return null;

            return new Vector2Int(row, col);
        }
    }
}
