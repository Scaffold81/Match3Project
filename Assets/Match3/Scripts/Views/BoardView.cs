#nullable enable

using System.Collections.Generic;
using Match3.Configs;
using Match3.Core.Enums;
using UnityEngine;

namespace Match3.Views
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private RectTransform _gemContainer = null!;
        [SerializeField] private RectTransform _cellContainer = null!;

        private BoardConfig _boardConfig = null!;
        private readonly Dictionary<Vector2Int, GemView> _gemViews = new();

        public RectTransform RectTransform => (RectTransform)transform;

        public void Initialize(BoardConfig boardConfig)
        {
            _boardConfig = boardConfig;
        }

        public Vector2 GetAnchoredPosition(int row, int col)
        {
            var step = _boardConfig.CellSize + _boardConfig.CellSpacing;
            return new Vector2(col * step, -row * step);
        }

        public void PlaceGem(Vector2Int cell, GemView gemView)
        {
            var rt = gemView.RectTransform;
            rt.SetParent(_gemContainer, false);
            rt.anchoredPosition = GetAnchoredPosition(cell.x, cell.y);
            rt.sizeDelta = new Vector2(_boardConfig.CellSize, _boardConfig.CellSize);
            _gemViews[cell] = gemView;
        }

        public void MoveGem(Vector2Int from, Vector2Int to)
        {
            if (!_gemViews.TryGetValue(from, out var gemView)) return;
            _gemViews.Remove(from);
            _gemViews[to] = gemView;
        }

        public GemView? GetGemView(Vector2Int cell) =>
            _gemViews.TryGetValue(cell, out var view) ? view : null;

        public void RemoveGem(Vector2Int cell)
        {
            if (!_gemViews.TryGetValue(cell, out var gemView)) return;
            _gemViews.Remove(cell);
            Destroy(gemView.gameObject);
        }

        public void ClearAll()
        {
            foreach (var gemView in _gemViews.Values)
                if (gemView != null)
                    Destroy(gemView.gameObject);
            _gemViews.Clear();
        }
    }
}
