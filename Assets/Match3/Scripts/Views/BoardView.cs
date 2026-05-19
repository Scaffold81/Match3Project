#nullable enable

using System;
using Match3.Configs;
using UnityEngine;

namespace Match3.Views
{
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField] private RectTransform _gemContainer  = null!;
        [SerializeField] private RectTransform _cellContainer = null!;
        [SerializeField] private RectTransform _dragLayer     = null!;

        private BoardConfig _boardConfig = null!;

        private float _cellSize;
        private int   _rows;
        private int   _cols;
        private bool  _layoutReady;

        public RectTransform RectTransform => (RectTransform)transform;
        public RectTransform GemContainer  => _gemContainer;

        public float CellSize    => _cellSize;
        public float CellSpacing => _boardConfig.CellSpacing;
        public int   Rows        => _rows;
        public int   Columns     => _cols;

        // ── Инициализация ────────────────────────────────────────────────────

        public void Initialize(BoardConfig boardConfig)
        {
            _boardConfig = boardConfig;
        }

        public void InitializeLayout(int rows, int cols)
        {
            _rows = rows;
            _cols = cols;

            var boardRect   = RectTransform.rect;
            var boardWidth  = boardRect.width;
            var boardHeight = boardRect.height;
            var padding     = _boardConfig.BoardPadding;
            var spacing     = _boardConfig.CellSpacing;

            var cellByW = (boardWidth  - padding * 2f - spacing * (cols - 1)) / cols;
            var cellByH = (boardHeight - padding * 2f - spacing * (rows - 1)) / rows;
            _cellSize = Mathf.Max(Mathf.Min(cellByW, cellByH), 1f);

            var totalWidth  = cols * _cellSize + spacing * (cols - 1);
            var totalHeight = rows * _cellSize + spacing * (rows - 1);

            var offsetX      = (boardWidth  - totalWidth)  * 0.5f;
            var offsetY      = (boardHeight - totalHeight) * 0.5f;
            var containerPos = new Vector2(offsetX, -offsetY);

            ApplyContainer(_gemContainer,  totalWidth, totalHeight, containerPos);
            ApplyContainer(_cellContainer, totalWidth, totalHeight, containerPos);

            _layoutReady = true;

            Debug.LogWarning(
                $"[BoardView] board={boardWidth:F0}×{boardHeight:F0} " +
                $"cellSize={_cellSize:F1} spacing={spacing} gemPad={_boardConfig.GemPadding} " +
                $"total={totalWidth:F0}×{totalHeight:F0}");
        }

        private static void ApplyContainer(
            RectTransform rt,
            float totalWidth, float totalHeight, Vector2 pos)
        {
            if (rt == null) return;
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.sizeDelta        = new Vector2(totalWidth, totalHeight);
            rt.anchoredPosition = pos;
        }

        // ── Позиционирование фишки ────────────────────────────────────────────

        public void PositionGem(RectTransform rt, Vector2Int slot)
        {
            if (!_layoutReady)
                throw new InvalidOperationException("Call InitializeLayout before PositionGem");

            ApplySlotTransform(rt, slot);
        }

        // ── Позиции ───────────────────────────────────────────────────────────

        public Vector2 GetAnchoredPosition(int row, int col)
        {
            if (!_layoutReady)
                throw new InvalidOperationException("Call InitializeLayout before GetAnchoredPosition");

            var step = _cellSize + _boardConfig.CellSpacing;
            return new Vector2(
                col * step + _boardConfig.GemPadding,
               -row * step - _boardConfig.GemPadding
            );
        }

        public Vector2 GetAnchoredPosition(Vector2Int cell) =>
            GetAnchoredPosition(cell.x, cell.y);

        public Vector3 GetSlotWorldPosition(Vector2Int cell)
        {
            var ap       = GetAnchoredPosition(cell);
            var rect     = _gemContainer.rect;
            var localPos = new Vector3(rect.xMin + ap.x, rect.yMax + ap.y, 0f);
            return _gemContainer.TransformPoint(localPos);
        }

        // ── Управление родителем ──────────────────────────────────────────────

        public void ReparentToOverlay(GemView gem)
        {
            gem.RectTransform.SetParent(_dragLayer, worldPositionStays: true);
        }

        public void ReparentToContainer(GemView gem, Vector2Int slot)
        {
            gem.RectTransform.SetParent(_gemContainer, worldPositionStays: true);
            ApplySlotTransform(gem.RectTransform, slot);
        }

        // ── Вспомогательное ───────────────────────────────────────────────────

        private void ApplySlotTransform(RectTransform rt, Vector2Int slot)
        {
            var gemSize = _cellSize - _boardConfig.GemPadding * 2f;

            rt.pivot            = new Vector2(0f, 1f);
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.anchoredPosition = GetAnchoredPosition(slot);
            rt.sizeDelta        = new Vector2(gemSize, gemSize);
            rt.localScale       = Vector3.one;
        }
    }
}
