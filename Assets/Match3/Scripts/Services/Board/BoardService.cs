#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Match3.Configs;
using Match3.Core;
using Match3.Core.Enums;
using Match3.Core.Models;
using UnityEngine;

namespace Match3.Services.Board
{
    public sealed class BoardService
    {
        private readonly Dictionary<Vector2Int, BoardCell> _cells        = new();
        private readonly List<Vector2Int>                  _spawners     = new();
        private readonly Dictionary<Vector2Int, NodeType>  _pendingTypes = new();
        private NodeType[] _allowedTypes = Array.Empty<NodeType>();

        // Координаты: Vector2Int(row, col)
        // row увеличивается ВНИЗ → "вниз" = (+1, 0)
        // col увеличивается ВПРАВО → "вправо" = (0, +1)
        private static readonly Vector2Int DirDown      = new( 1,  0);
        private static readonly Vector2Int DirUp        = new(-1,  0);
        private static readonly Vector2Int DirRight     = new( 0,  1);
        private static readonly Vector2Int DirLeft      = new( 0, -1);
        private static readonly Vector2Int DirDownLeft  = new( 1, -1);
        private static readonly Vector2Int DirDownRight = new( 1,  1);

        private static readonly Vector2Int[] MatchOffsets =
        {
            DirUp, DirRight, DirDown, DirLeft
        };

        private static readonly Vector2Int[] DiagonalFallDirs =
        {
            DirLeft, DirRight
        };

        public IReadOnlyDictionary<Vector2Int, BoardCell> Cells    => _cells;
        public IReadOnlyList<Vector2Int>                  Spawners => _spawners;
        public int Rows    { get; private set; }
        public int Columns { get; private set; }

        // ── Initialization ───────────────────────────────────────────────────

        public void Initialize(LevelConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _cells.Clear();
            _spawners.Clear();

            Rows    = config.Rows;
            Columns = config.Columns;

            for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Columns; col++)
            {
                var data = config.GetCell(row, col);
                var pos  = new Vector2Int(row, col);
                _cells[pos] = new BoardCell { CellType = data.cellType };
            }

            // Спавнер = ячейка над первой Normal ячейкой в каждой колонке
            for (var col = 0; col < Columns; col++)
            {
                for (var row = 0; row < Rows; row++)
                {
                    if (!IsNormalCell(new Vector2Int(row, col))) continue;
                    _spawners.Add(new Vector2Int(row - 1, col));
                    break;
                }
            }

            _allowedTypes = config.AllowedNodeTypes.Length > 0
                ? config.AllowedNodeTypes
                : GetAllNodeTypes();
        }

        // ── Cell queries ─────────────────────────────────────────────────────

        public bool TryGetCell(Vector2Int pos, out BoardCell cell) =>
            _cells.TryGetValue(pos, out cell!);

        public bool IsValidCell(Vector2Int pos)  => _cells.ContainsKey(pos);
        public bool IsNormalCell(Vector2Int pos) => _cells.TryGetValue(pos, out var c) && c.CellType == CellType.Normal;
        public bool IsEmpty(Vector2Int pos)      => _cells.TryGetValue(pos, out var c) && c.IsEmpty();

        public IGemView? GetGem(Vector2Int pos) =>
            _cells.TryGetValue(pos, out var c) ? c.ContainingGem : null;

        // ── Mutation ─────────────────────────────────────────────────────────

        public void PlaceGem(Vector2Int pos, IGemView gem)
        {
            if (!_cells.TryGetValue(pos, out var cell))
                throw new ArgumentOutOfRangeException(nameof(pos), $"Cell {pos} does not exist");
            cell.ContainingGem = gem;
        }

        public void RemoveGem(Vector2Int pos)
        {
            if (_cells.TryGetValue(pos, out var cell))
                cell.ContainingGem = null;
        }

        public void ExchangeGems(Vector2Int a, Vector2Int b)
        {
            if (!_cells.TryGetValue(a, out var cellA))
                throw new ArgumentOutOfRangeException(nameof(a));
            if (!_cells.TryGetValue(b, out var cellB))
                throw new ArgumentOutOfRangeException(nameof(b));

            var typeA = cellA.ContainingGem?.GemType ?? NodeType.None;
            var typeB = cellB.ContainingGem?.GemType ?? NodeType.None;

            (cellA.ContainingGem, cellB.ContainingGem) = (cellB.ContainingGem, cellA.ContainingGem);

            if (cellA.ContainingGem != null) cellA.ContainingGem.MoveTo(a);
            if (cellB.ContainingGem != null) cellB.ContainingGem.MoveTo(b);

            Debug.LogWarning(
                $"[BoardService] ExchangeGems:\n" +
                $"  {a}: {typeA} → {cellA.ContainingGem?.GemType ?? NodeType.None}\n" +
                $"  {b}: {typeB} → {cellB.ContainingGem?.GemType ?? NodeType.None}\n" +
                DumpBoard());
        }

        private string DumpBoard()
        {
            var sb = new StringBuilder();
            sb.AppendLine("  [Board state]");

            for (var row = 0; row < Rows; row++)
            {
                sb.Append($"  row{row}: ");
                for (var col = 0; col < Columns; col++)
                {
                    var pos = new Vector2Int(row, col);
                    string label;
                    if (_cells.TryGetValue(pos, out var cell))
                    {
                        var t = cell.ContainingGem?.GemType ?? NodeType.None;
                        label = t == NodeType.None ? "No" : t.ToString()[..2];
                    }
                    else
                    {
                        label = "XX";
                    }
                    sb.Append($"[{col}]{label,-4}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        public void LockCell(Vector2Int pos, bool locked)
        {
            if (_cells.TryGetValue(pos, out var cell))
                cell.Locked = locked;
        }

        public bool AreNeighbors(Vector2Int a, Vector2Int b)
        {
            var diff = new Vector2Int(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
            return (diff.x == 1 && diff.y == 0) || (diff.x == 0 && diff.y == 1);
        }

        // ── Initial board fill (no-match guarantee) ───────────────────────────

        public List<(Vector2Int pos, NodeType type)> GenerateInitialGems(NodeType[] allowedTypes)
        {
            if (allowedTypes == null || allowedTypes.Length == 0)
                throw new ArgumentException("allowedTypes must not be empty", nameof(allowedTypes));

            var result = new List<(Vector2Int, NodeType)>();

            for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Columns; col++)
            {
                var pos = new Vector2Int(row, col);
                if (!IsNormalCell(pos) || _cells[pos].ContainingGem != null)
                    continue;

                var available = new List<NodeType>(allowedTypes);
                ExcludeMatchingTypes(pos, available);

                if (available.Count == 0)
                    available.AddRange(allowedTypes);

                var chosen = available[UnityEngine.Random.Range(0, available.Count)];
                _cells[pos].CellType = CellType.Normal;
                result.Add((pos, chosen));
                _pendingTypes[pos] = chosen;
            }

            _pendingTypes.Clear();
            return result;
        }

        private void ExcludeMatchingTypes(Vector2Int pos, List<NodeType> available)
        {
            CheckLine(pos, DirLeft,  DirRight, available);
            CheckLine(pos, DirUp,    DirDown,  available);
            CheckLine(pos, DirLeft,  DirLeft  * 2, available);
            CheckLine(pos, DirRight, DirRight * 2, available);
            CheckLine(pos, DirUp,    DirUp    * 2, available);
            CheckLine(pos, DirDown,  DirDown  * 2, available);
        }

        private void CheckLine(Vector2Int pos, Vector2Int dir1, Vector2Int dir2, List<NodeType> available)
        {
            var type1 = GetPendingOrPlacedType(pos + dir1);
            var type2 = GetPendingOrPlacedType(pos + dir2);
            if (type1 != NodeType.None && type1 == type2)
                available.Remove(type1);
        }

        private NodeType GetPendingOrPlacedType(Vector2Int pos)
        {
            if (_pendingTypes.TryGetValue(pos, out var pending))
                return pending;
            if (_cells.TryGetValue(pos, out var cell) && cell.ContainingGem != null)
                return cell.ContainingGem.GemType;
            return NodeType.None;
        }

        // ── Spawn ─────────────────────────────────────────────────────────────

        public List<(Vector2Int pos, NodeType type)> GetSpawnList()
        {
            var result = new List<(Vector2Int, NodeType)>();

            // Спавним сверху вниз — сначала верхние строки чтобы
            // последующая гравитация могла их сдвинуть корректно
            for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Columns; col++)
            {
                var pos = new Vector2Int(row, col);
                if (!IsNormalCell(pos)) continue;
                if (!IsEmpty(pos))      continue;
                result.Add((pos, GetRandomAllowedType()));
            }

            Debug.LogWarning($"[BoardService] GetSpawnList: {result.Count} пустых ячеек найдено");
            return result;
        }

        private NodeType GetRandomAllowedType() =>
            _allowedTypes[UnityEngine.Random.Range(0, _allowedTypes.Length)];

        private static NodeType[] GetAllNodeTypes()
        {
            var values = Enum.GetValues(typeof(NodeType));
            var result = new List<NodeType>();
            foreach (NodeType v in values)
                if (v != NodeType.None)
                    result.Add(v);
            return result.ToArray();
        }

        // ── Match detection ───────────────────────────────────────────────────

        public List<GemMatch> FindAndCreateMatches(IEnumerable<Vector2Int> seedCells)
        {
            var result  = new List<GemMatch>();
            var visited = new HashSet<Vector2Int>();

            foreach (var seed in seedCells)
            {
                if (visited.Contains(seed)) continue;
                DoMatchCheck(seed, createMatch: true, result, visited);
            }

            return result;
        }

        public bool HasMatchAfterSwap(Vector2Int a, Vector2Int b)
        {
            var cellA = _cells[a];
            var cellB = _cells[b];

            (cellA.ContainingGem, cellB.ContainingGem) = (cellB.ContainingGem, cellA.ContainingGem);

            var dummy   = new List<GemMatch>();
            var visited = new HashSet<Vector2Int>();
            DoMatchCheck(a, createMatch: false, dummy, visited);
            DoMatchCheck(b, createMatch: false, dummy, visited);
            var hasMatch = dummy.Count > 0;

            (cellA.ContainingGem, cellB.ContainingGem) = (cellB.ContainingGem, cellA.ContainingGem);

            return hasMatch;
        }

        private void DoMatchCheck(
            Vector2Int          startCell,
            bool                createMatch,
            List<GemMatch>      results,
            HashSet<Vector2Int> visited)
        {
            if (!_cells.TryGetValue(startCell, out var centerCell) ||
                !centerCell.CanMatch() ||
                centerCell.ContainingGem!.CurrentMatch != null)
                return;

            var startType = centerCell.ContainingGem.GemType;
            var island    = new List<Vector2Int>();
            var islandSet = new HashSet<Vector2Int>();
            var queue     = new Queue<Vector2Int>();
            queue.Enqueue(startCell);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (islandSet.Contains(current)) continue;

                island.Add(current);
                islandSet.Add(current);

                foreach (var dir in MatchOffsets)
                {
                    var next = current + dir;
                    if (islandSet.Contains(next)) continue;

                    if (_cells.TryGetValue(next, out var nextCell)
                        && nextCell.CanMatch()
                        && nextCell.ContainingGem!.CurrentMatch == null
                        && nextCell.ContainingGem.GemType == startType)
                    {
                        queue.Enqueue(next);
                    }
                }
            }

            var lineSet = new HashSet<Vector2Int>();

            foreach (var idx in island)
            {
                foreach (var dir in MatchOffsets)
                {
                    if (islandSet.Contains(idx + dir)) continue;

                    var line = new List<Vector2Int> { idx };
                    var next = idx - dir;
                    while (islandSet.Contains(next))
                    {
                        line.Add(next);
                        next -= dir;
                    }

                    if (line.Count >= 3)
                        foreach (var cell in line)
                            lineSet.Add(cell);
                }
            }

            if (lineSet.Count == 0) return;

            foreach (var cell in lineSet)
                visited.Add(cell);

            if (!createMatch)
            {
                results.Add(new GemMatch());
                return;
            }

            var match = new GemMatch { OriginPoint = startCell };

            foreach (var cell in lineSet)
            {
                if (!_cells.TryGetValue(cell, out var boardCell)) continue;
                if (!boardCell.CanDelete()) continue;
                if (boardCell.ContainingGem == null) continue;
                match.AddGem(boardCell.ContainingGem);
            }

            if (match.MatchingCells.Count > 0)
                results.Add(match);
        }

        // ── Gravity ───────────────────────────────────────────────────────────

        public List<(Vector2Int from, Vector2Int to)> ComputeAndApplyFalls()
        {
            var moves = new List<(Vector2Int, Vector2Int)>();

            bool anyMoved;
            do
            {
                anyMoved = false;

                // Идём снизу вверх: row = Rows-1 → 0
                // чтобы нижние ячейки освобождались первыми
                for (var row = Rows - 1; row >= 0; row--)
                for (var col = 0; col < Columns; col++)
                {
                    var pos  = new Vector2Int(row, col);
                    var down = pos + DirDown; // следующая строка = ниже на экране

                    if (!_cells.TryGetValue(pos, out var cell)) continue;
                    if (!cell.CanFall) continue;

                    // Прямое падение вниз
                    if (TryFall(pos, down, moves))
                    {
                        anyMoved = true;
                        continue;
                    }

                    // Диагональное падение (если прямо заблокировано)
                    if (_cells.TryGetValue(down, out var downCell) && downCell.BlockFall)
                    {
                        foreach (var diagDir in DiagonalFallDirs)
                        {
                            if (!TryFall(pos, down + diagDir, moves)) continue;
                            anyMoved = true;
                            break;
                        }
                    }
                }
            } while (anyMoved);

            Debug.LogWarning($"[BoardService] ComputeAndApplyFalls: {moves.Count} перемещений\n{DumpBoard()}");
            return moves;
        }

        private bool TryFall(Vector2Int from, Vector2Int to, List<(Vector2Int, Vector2Int)> moves)
        {
            if (!_cells.TryGetValue(to, out var targetCell)) return false;
            if (!targetCell.IsEmpty()) return false;
            if (!_cells.TryGetValue(from, out var sourceCell)) return false;
            if (sourceCell.ContainingGem == null) return false;

            var gem = sourceCell.ContainingGem;
            targetCell.ContainingGem = gem;
            sourceCell.ContainingGem = null;
            gem.MoveTo(to);

            moves.Add((from, to));
            return true;
        }

        // ── Swap hints ────────────────────────────────────────────────────────

        public List<(Vector2Int from, Vector2Int to)> FindAllPossibleSwaps()
        {
            var result = new List<(Vector2Int, Vector2Int)>();

            for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Columns; col++)
            {
                var pos = new Vector2Int(row, col);
                if (!_cells.TryGetValue(pos, out var cell) || !cell.CanBeMoved) continue;

                TryAddSwapHint(pos, pos + DirUp,    result);
                TryAddSwapHint(pos, pos + DirRight, result);
            }

            return result;
        }

        private void TryAddSwapHint(Vector2Int from, Vector2Int to, List<(Vector2Int, Vector2Int)> result)
        {
            if (!_cells.TryGetValue(to, out var toCell) || !toCell.CanBeMoved) return;
            if (HasMatchAfterSwap(from, to))
                result.Add((from, to));
        }
    }
}
