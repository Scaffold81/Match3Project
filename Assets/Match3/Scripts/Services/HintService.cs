#nullable enable

using System;
using System.Collections.Generic;
using Match3.Core;
using Match3.Core.Enums;
using Match3.Services.Board;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services.Hint
{
    public sealed class HintService : IDisposable
    {
        private readonly BoardService _boardService;

        private readonly Subject<(Vector2Int from, Vector2Int to)> _onHintRequested   = new();
        private readonly Subject<Unit>                              _onShuffleRequested = new();

        public Observable<(Vector2Int from, Vector2Int to)> OnHintRequested   => _onHintRequested;
        public Observable<Unit>                             OnShuffleRequested => _onShuffleRequested;

        [Inject]
        public HintService(BoardService boardService)
        {
            _boardService = boardService;
        }

        // ── Подсказка ─────────────────────────────────────────────────────────

        public List<(Vector2Int from, Vector2Int to)> GetPossibleSwaps() =>
            _boardService.FindAllPossibleSwaps();

        public bool TryRequestHint()
        {
            var swaps = GetPossibleSwaps();
            if (swaps.Count == 0)
            {
                Debug.LogWarning("[HintService] Нет доступных ходов");
                return false;
            }

            var hint = swaps[UnityEngine.Random.Range(0, swaps.Count)];
            Debug.LogWarning($"[HintService] Подсказка: {hint.from} → {hint.to}");
            _onHintRequested.OnNext(hint);
            return true;
        }

        // ── Перемешивание ─────────────────────────────────────────────────────

        public List<(Vector2Int pos, NodeType type)> Shuffle()
        {
            const int MaxAttempts = 100;

            var cells = CollectNormalCells();
            if (cells.Count == 0) return new List<(Vector2Int, NodeType)>();

            var types = ExtractTypes(cells);

            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                FisherYatesShuffle(types);
                PlaceTypes(cells, types);

                var hasMatches  = _boardService.FindAndCreateMatches(cells).Count > 0;
                var hasPossible = _boardService.FindAllPossibleSwaps().Count > 0;
                ResetMatches(cells);

                if (!hasMatches && hasPossible)
                {
                    Debug.LogWarning($"[HintService] Shuffle успешен за {attempt + 1} попыток");
                    _onShuffleRequested.OnNext(Unit.Default);
                    return BuildResult(cells);
                }
            }

            Debug.LogWarning("[HintService] Shuffle fallback");
            _onShuffleRequested.OnNext(Unit.Default);
            return BuildResult(cells);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private List<Vector2Int> CollectNormalCells()
        {
            var result = new List<Vector2Int>();
            for (var row = 0; row < _boardService.Rows; row++)
            for (var col = 0; col < _boardService.Columns; col++)
            {
                var pos = new Vector2Int(row, col);
                if (_boardService.IsNormalCell(pos) && _boardService.GetGem(pos) != null)
                    result.Add(pos);
            }
            return result;
        }

        private List<NodeType> ExtractTypes(List<Vector2Int> cells)
        {
            var types = new List<NodeType>(cells.Count);
            foreach (var pos in cells)
            {
                var gem = _boardService.GetGem(pos);
                if (gem != null) types.Add(gem.GemType);
            }
            return types;
        }

        private void PlaceTypes(List<Vector2Int> cells, List<NodeType> types)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                var gem = _boardService.GetGem(cells[i]);
                if (gem != null) gem.SetGemType(types[i]);
            }
        }

        private void ResetMatches(List<Vector2Int> cells)
        {
            foreach (var pos in cells)
            {
                var gem = _boardService.GetGem(pos);
                if (gem != null) gem.CurrentMatch = null;
            }
        }

        private List<(Vector2Int, NodeType)> BuildResult(List<Vector2Int> cells)
        {
            var result = new List<(Vector2Int, NodeType)>(cells.Count);
            foreach (var pos in cells)
            {
                var gem = _boardService.GetGem(pos);
                if (gem != null) result.Add((pos, gem.GemType));
            }
            return result;
        }

        private static void FisherYatesShuffle<T>(List<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public void Dispose()
        {
            _onHintRequested.Dispose();
            _onShuffleRequested.Dispose();
        }
    }
}
