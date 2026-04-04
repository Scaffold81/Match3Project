#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    public sealed class BoardView : MonoBehaviour
    {
        // Prefab опционален — если null, создаём ячейки через код (без ассетов)
        [field: SerializeField] private GameObject? _gemViewPrefabObj = null;
        [field: SerializeField] private RectTransform _gemContainer  = null!;
        [field: SerializeField] private RectTransform _cellContainer = null!;

        private BoardConfig _boardConfig = null!;
        private float       _cellSize;
        private GemView[,]  _grid = new GemView[0, 0];

        public RectTransform RectTransform => (RectTransform)transform;
        public float CellSize    => _cellSize;
        public float CellSpacing => _boardConfig.CellSpacing;

        public event Action<Vector2Int>? OnGemClicked;

        public void Initialize(BoardConfig boardConfig)
        {
            _boardConfig = boardConfig;
        }

        // Создаёт фиксированную сетку GemView — вызывается один раз при старте уровня
        public void InitializeGrid(int rows, int cols)
        {
            // Чистим старую сетку
            foreach (var view in _grid)
                if (view != null) Destroy(view.gameObject);

            // Вычисляем cellSize из ширины Board
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

            // Создаём по одному GemView на каждую ячейку (через код или префаб)
            _grid = new GemView[rows, cols];
            for (var row = 0; row < rows; row++)
            for (var col = 0; col < cols; col++)
            {
                GameObject? prefabToUse = null;
                
                // Если префаб назначен — используем его, иначе создаём через код
                if (_gemViewPrefabObj != null)
                    prefabToUse = _gemViewPrefabObj;

                GemView view;
                if (prefabToUse == null)
                {
                    // Создание через код (без ассетов)
                    var gemObj = new GameObject($"GemView_{row}_{col}");
                    view = gemObj.AddComponent<GemView>();
                }
                else
                {
                    // Через префаб (если назначен)
                    prefabToUse.name = $"GemView_{row}_{col}";
                    view = Instantiate(prefabToUse, _gemContainer).GetComponent<GemView>();
                }

                var rt = view.RectTransform;
                
                // Позиционирование
                rt.pivot            = new Vector2(0f, 1f);
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(0f, 1f);
                rt.anchoredPosition = GetAnchoredPosition(row, col);
                rt.sizeDelta        = new Vector2(_cellSize, _cellSize);

                var r = row;
                var c = col;
                view.OnClicked += () => OnGemClicked?.Invoke(new Vector2Int(r, c));

                _grid[row, col] = view;
            }
        }

        public Vector2 GetAnchoredPosition(int row, int col)
        {
            var step = _cellSize + _boardConfig.CellSpacing;
            return new Vector2(col * step, -row * step);
        }

        public GemView? GetGemView(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= _grid.GetLength(0)) return null;
            if (cell.y < 0 || cell.y >= _grid.GetLength(1)) return null;
            return _grid[cell.x, cell.y];
        }

        public GemView? GetGemView(int row, int col) =>
            GetGemView(new Vector2Int(row, col));

        // Обменивает визуал двух ячеек без движения
        public void SwapVisualsAt(Vector2Int a, Vector2Int b)
        {
            var viewA = GetGemView(a);
            var viewB = GetGemView(b);
            if (viewA == null || viewB == null) return;
            viewA.SwapWith(viewB);
        }
    }
}
