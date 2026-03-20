#nullable enable

using System.Collections.Generic;
using Match3.Core.Enums;
using Match3.Services.Board;
using UnityEngine;
using Zenject;

namespace Match3.Services.Gravity
{
    public sealed class GravityService
    {
        private readonly BoardService _boardService;

        [Inject]
        public GravityService(BoardService boardService)
        {
            _boardService = boardService;
        }

        public List<(Vector2Int from, Vector2Int to)> ApplyGravity()
        {
            var moves = new List<(Vector2Int from, Vector2Int to)>();

            for (var col = 0; col < _boardService.Columns; col++)
                ApplyGravityInColumn(col, moves);

            return moves;
        }

        private void ApplyGravityInColumn(int col, List<(Vector2Int from, Vector2Int to)> moves)
        {
            for (var row = _boardService.Rows - 1; row >= 0; row--)
            {
                if (!_boardService.IsNormalCell(row, col)) continue;
                if (!_boardService.IsEmpty(row, col)) continue;

                var sourceRow = FindNodeAbove(row - 1, col);
                if (sourceRow < 0) continue;

                var from = new Vector2Int(sourceRow, col);
                var to = new Vector2Int(row, col);

                _boardService.SetNode(row, col, _boardService.GetNode(sourceRow, col));
                _boardService.RemoveNode(sourceRow, col);

                moves.Add((from, to));

                row++;
            }
        }

        private int FindNodeAbove(int startRow, int col)
        {
            for (var row = startRow; row >= 0; row--)
            {
                if (!_boardService.IsNormalCell(row, col)) continue;
                if (!_boardService.IsEmpty(row, col)) return row;
            }
            return -1;
        }
    }
}
