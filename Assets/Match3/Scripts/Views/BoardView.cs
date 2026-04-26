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

        /// <summary>
        /// Оверлей для гемов во время анимации.
        /// Должен лежать выше GemContainer в иерархии Canvas,
        /// чтобы летящий гем рисовался поверх всех остальных.
        /// </summary>
        [SerializeField] private RectTransform _dragLayer = null!;

        private BoardConfig _boardConfig   = null!;
        private GameObject  _gemViewPrefab = null!;

        private float _cellSize;
        private int   _rows;
        private int   _cols;
        private bool  _layoutReady;

        public RectTransform RectTransform => (RectTransform)transform;
        public RectTransform GemContainer  => _gemContainer;

        public float CellSize    => _cellSize;
        public float CellSpacing => _boardConfig != null ? _boardConfig.CellSpacing : 0f;
        public int   Rows        => _rows;
        public int   Columns     => _cols;

        // ── Инициализация ────────────────────────────────────────────────────

        public void Initialize(BoardConfig boardConfig, GameObject gemViewPrefab)
        {
            _boardConfig   = boardConfig;
            _gemViewPrefab = gemViewPrefab;
        }

        public void InitializeLayout(int rows, int cols)
        {
            _rows = rows;
            _cols = cols;

            var boardWidth  = RectTransform.rect.width;
            var usableWidth = boardWidth - _boardConfig.BoardPadding * 2f;
            _cellSize = (usableWidth - _boardConfig.CellSpacing * (cols - 1)) / cols;

            var totalWidth  = cols * _cellSize + _boardConfig.CellSpacing * (cols - 1);
            var totalHeight = rows * _cellSize + _boardConfig.CellSpacing * (rows - 1);

            _gemContainer.sizeDelta        = new Vector2(totalWidth, totalHeight);
            _gemContainer.anchoredPosition = new Vector2(_boardConfig.BoardPadding, 0f);

            if (_cellContainer != null)
            {
                _cellContainer.sizeDelta        = new Vector2(totalWidth, totalHeight);
                _cellContainer.anchoredPosition = new Vector2(_boardConfig.BoardPadding, 0f);
            }

            _layoutReady = true;
        }

        // ── Gem factory ───────────────────────────────────────────────────────

        public GemView InstantiateGem(int row, int col)
        {
            if (!_layoutReady)
                throw new InvalidOperationException("Call InitializeLayout before InstantiateGem");

            var view = Instantiate(_gemViewPrefab, _gemContainer).GetComponent<GemView>();
            view.gameObject.name = $"Gem_{row}_{col}";

            ApplySlotTransform(view.RectTransform, new Vector2Int(row, col));

            return view;
        }

        public void DestroyGem(GemView gem)
        {
            if (gem != null)
                Destroy(gem.gameObject);
        }

        // ── Позиции ───────────────────────────────────────────────────────────

        public Vector2 GetAnchoredPosition(int row, int col)
        {
            if (!_layoutReady)
                throw new InvalidOperationException("Call InitializeLayout before GetAnchoredPosition");

            var step = _cellSize + _boardConfig.CellSpacing;
            return new Vector2(
                col  * step + _boardConfig.GemPadding,
                -row * step - _boardConfig.GemPadding
            );
        }

        public Vector2 GetAnchoredPosition(Vector2Int cell) =>
            GetAnchoredPosition(cell.x, cell.y);

        /// <summary>
        /// Мировая позиция пивота (top-left) слота ячейки.
        /// Используется для DOMove во время анимации из DragLayer.
        /// </summary>
        public Vector3 GetSlotWorldPosition(Vector2Int cell)
        {
            var ap   = GetAnchoredPosition(cell);
            var rect = _gemContainer.rect;

            // anchoredPosition задаёт смещение пивота гема от точки привязки (0,1).
            // Точка привязки (0,1) в local-space контейнера = (rect.xMin, rect.yMax).
            var localPos = new Vector3(rect.xMin + ap.x, rect.yMax + ap.y, 0f);
            return _gemContainer.TransformPoint(localPos);
        }

        // ── Управление родителем ──────────────────────────────────────────────

        /// <summary>
        /// Перемещает гем в DragLayer, сохраняя визуальную позицию.
        /// Вызывать ДО начала анимации.
        /// </summary>
        public void ReparentToOverlay(GemView gem)
        {
            gem.RectTransform.SetParent(_dragLayer, worldPositionStays: true);
        }

        /// <summary>
        /// Возвращает гем в GemContainer и восстанавливает параметры слота.
        /// worldPositionStays: true — Unity пересчитывает anchoredPosition под новый контейнер,
        /// затем ApplySlotTransform выставляет точные значения слота без визуального скачка.
        /// Вызывать ПОСЛЕ завершения анимации (в onComplete).
        /// </summary>
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
