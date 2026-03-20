#nullable enable

using System;
using System.Collections.Generic;
using Match3.Configs;
using Match3.Core.Enums;
using R3;
using UnityEngine;

namespace Match3.Services.Layer
{
    public sealed class LayerService : IDisposable
    {
        private bool[,] _layers = new bool[0, 0];
        private int _totalLayerCells;

        private readonly ReactiveProperty<int> _clearedCount = new(0);
        private readonly Subject<Vector2Int> _onLayerCleared = new();
        private readonly Subject<Unit> _onAllLayersCleared = new();

        public ReadOnlyReactiveProperty<int> ClearedCount => _clearedCount;
        public Observable<Vector2Int> OnLayerCleared => _onLayerCleared;
        public Observable<Unit> OnAllLayersCleared => _onAllLayersCleared;

        public int TotalLayerCells => _totalLayerCells;
        public bool IsAllCleared => _clearedCount.Value >= _totalLayerCells && _totalLayerCells > 0;

        public void Initialize(LevelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _layers = new bool[config.Rows, config.Columns];
            _totalLayerCells = 0;
            _clearedCount.Value = 0;

            for (var row = 0; row < config.Rows; row++)
            for (var col = 0; col < config.Columns; col++)
            {
                var cell = config.GetCell(row, col);
                _layers[row, col] = cell.hasLayer;
                if (cell.hasLayer) _totalLayerCells++;
            }
        }

        public bool HasLayer(int row, int col) => _layers[row, col];

        public void TryClearLayer(int row, int col)
        {
            if (row < 0 || row >= _layers.GetLength(0)) return;
            if (col < 0 || col >= _layers.GetLength(1)) return;
            if (!_layers[row, col]) return;

            _layers[row, col] = false;
            _clearedCount.Value++;

            _onLayerCleared.OnNext(new Vector2Int(row, col));

            if (IsAllCleared)
                _onAllLayersCleared.OnNext(Unit.Default);
        }

        public void ProcessMatches(List<Vector2Int> matchedCells)
        {
            foreach (var cell in matchedCells)
                TryClearLayer(cell.x, cell.y);
        }

        public void Dispose()
        {
            _clearedCount.Dispose();
            _onLayerCleared.Dispose();
            _onAllLayersCleared.Dispose();
        }
    }
}
