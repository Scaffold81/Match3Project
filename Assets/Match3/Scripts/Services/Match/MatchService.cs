#nullable enable

using System.Collections.Generic;
using Match3.Core.Enums;
using UnityEngine;

namespace Match3.Services.Match
{
    public sealed class MatchService
    {
        private const int MinMatchLength = 3;

        public List<List<Vector2Int>> FindMatches(NodeType[,] board, int rows, int cols)
        {
            var matches = new List<List<Vector2Int>>();
            var matched = new bool[rows, cols];

            FindHorizontalMatches(board, rows, cols, matches, matched);
            FindVerticalMatches(board, rows, cols, matches, matched);

            return matches;
        }

        public bool HasAnyMatch(NodeType[,] board, int rows, int cols)
        {
            var dummy = new bool[rows, cols];
            var matches = new List<List<Vector2Int>>();

            FindHorizontalMatches(board, rows, cols, matches, dummy);
            if (matches.Count > 0) return true;

            FindVerticalMatches(board, rows, cols, matches, dummy);
            return matches.Count > 0;
        }

        public List<Vector2Int> GetAllMatchedCells(List<List<Vector2Int>> matches)
        {
            var result = new List<Vector2Int>();
            var seen = new HashSet<Vector2Int>();

            foreach (var match in matches)
            foreach (var cell in match)
                if (seen.Add(cell))
                    result.Add(cell);

            return result;
        }

        private void FindHorizontalMatches(
            NodeType[,] board, int rows, int cols,
            List<List<Vector2Int>> matches, bool[,] matched)
        {
            for (var row = 0; row < rows; row++)
            {
                var col = 0;
                while (col < cols)
                {
                    var nodeType = board[row, col];
                    if (nodeType == NodeType.None)
                    {
                        col++;
                        continue;
                    }

                    var end = col + 1;
                    while (end < cols && board[row, end] == nodeType)
                        end++;

                    if (end - col >= MinMatchLength)
                    {
                        var match = new List<Vector2Int>();
                        for (var c = col; c < end; c++)
                        {
                            match.Add(new Vector2Int(row, c));
                            matched[row, c] = true;
                        }
                        matches.Add(match);
                    }

                    col = end;
                }
            }
        }

        private void FindVerticalMatches(
            NodeType[,] board, int rows, int cols,
            List<List<Vector2Int>> matches, bool[,] matched)
        {
            for (var col = 0; col < cols; col++)
            {
                var row = 0;
                while (row < rows)
                {
                    var nodeType = board[row, col];
                    if (nodeType == NodeType.None)
                    {
                        row++;
                        continue;
                    }

                    var end = row + 1;
                    while (end < rows && board[end, col] == nodeType)
                        end++;

                    if (end - row >= MinMatchLength)
                    {
                        var match = new List<Vector2Int>();
                        for (var r = row; r < end; r++)
                        {
                            match.Add(new Vector2Int(r, col));
                            matched[r, col] = true;
                        }
                        matches.Add(match);
                    }

                    row = end;
                }
            }
        }
    }
}
