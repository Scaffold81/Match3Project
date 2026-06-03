#nullable enable

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Match3.Views
{
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class BoardInputHandler : MonoBehaviour, IPointerClickHandler
    {
        public event Action<Vector2Int>? OnCellClicked;

        private BoardView _boardView    = null!;
        private Canvas    _canvas       = null!;
        private bool      _inputEnabled = true;

        [UnityEngine.Scripting.Preserve]
        [Zenject.Inject]
        public void Construct(BoardView boardView)
        {
            _boardView = boardView;
            _canvas    = GetComponentInParent<Canvas>();
        }

        public void SetInputEnabled(bool enabled) => _inputEnabled = enabled;

        public void OnPointerClick(PointerEventData e)
        {
            if (!_inputEnabled) return;

            if (!ScreenToCell(e.position, out var cell))
                return;

            OnCellClicked?.Invoke(cell);
        }

        private bool ScreenToCell(Vector2 screenPos, out Vector2Int cell)
        {
            cell = default;

            if (_boardView == null || _boardView.GemContainer == null)
                return false;

            Camera? cam = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _boardView.RectTransform, screenPos, cam, out var localInBoard))
                return false;

            var boardRect    = _boardView.RectTransform.rect;
            var topLeft      = new Vector2(boardRect.xMin, boardRect.yMax);
            var relToTopLeft = localInBoard - topLeft;

            var containerOffset  = _boardView.GemContainer.anchoredPosition;
            var localInContainer = relToTopLeft - containerOffset;

            var step = _boardView.CellSize + _boardView.CellSpacing;
            if (step <= 0f) return false;

            var col = Mathf.FloorToInt(localInContainer.x  / step);
            var row = Mathf.FloorToInt(-localInContainer.y / step);

            if (col < 0 || col >= _boardView.Columns) return false;
            if (row < 0 || row >= _boardView.Rows)    return false;

            cell = new Vector2Int(row, col);
            return true;
        }
    }
}
