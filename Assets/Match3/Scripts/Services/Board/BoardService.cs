#nullable enable

using System;
using System.Collections.Generic;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using R3;
using UnityEngine;

namespace Match3.Services.Board
{
    public sealed class BoardService : IDisposable
    {
        private readonly ReactiveProperty<NodeType[,]> _board = new(new NodeType[0, 0]);
        private CellType[,] _cellTypes = new CellType[0, 0];

        public ReadOnlyReactiveProperty<NodeType[,]> Board => _board;
        public int Rows { get; private set; }
        public int Columns { get; private set; }

        public void Initialize(LevelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Rows = config.Rows;
            Columns = config.Columns;

            _board.Value = new NodeType[Rows, Columns];
            _cellTypes = new CellType[Rows, Columns];

            for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Columns; col++)
            {
                var cell = config.GetCell(row, col);
                _cellTypes[row, col] = cell.cellType;
                _board.Value[row, col] = cell.cellType == CellType.Hidden
                    ? NodeType.None
                    : cell.nodeType;
            }
        }

        public NodeType GetNode(int row, int col) => _board.Value[row, col];

        public void SetNode(int row, int col, NodeType nodeType)
        {
            if (!IsValidCell(row, col))
                throw new ArgumentOutOfRangeException($"Cell ({row},{col}) is out of range");

            _board.Value[row, col] = nodeType;
            _board.ForceNotify();
        }

        public void RemoveNode(int row, int col)
        {
            if (!IsValidCell(row, col))
                throw new ArgumentOutOfRangeException($"Cell ({row},{col}) is out of range");

            _board.Value[row, col] = NodeType.None;
            _board.ForceNotify();
        }

        public bool IsValidCell(int row, int col) =>
            row >= 0 && row < Rows && col >= 0 && col < Columns;

        public bool IsNormalCell(int row, int col) =>
            IsValidCell(row, col) && _cellTypes[row, col] == CellType.Normal;

        public bool IsEmpty(int row, int col) =>
            IsValidCell(row, col) && _board.Value[row, col] == NodeType.None;

        public CellType GetCellType(int row, int col) => _cellTypes[row, col];

        public void SwapNodes(Vector2Int a, Vector2Int b)
        {
            var temp = _board.Value[a.x, a.y];
            _board.Value[a.x, a.y] = _board.Value[b.x, b.y];
            _board.Value[b.x, b.y] = temp;
            _board.ForceNotify();
        }

        public void Dispose() => _board.Dispose();
    }
}
