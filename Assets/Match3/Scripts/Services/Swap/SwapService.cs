#nullable enable

using System;
using System.Collections.Generic;
using Match3.Services.Board;
using Match3.Services.Match;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services.Swap
{
    public sealed class SwapService : IDisposable
    {
        private readonly BoardService _boardService;
        private readonly MatchService _matchService;

        private readonly Subject<(Vector2Int from, Vector2Int to)> _onSwapRequested = new();
        private readonly Subject<(Vector2Int from, Vector2Int to)> _onSwapSuccess = new();
        private readonly Subject<(Vector2Int from, Vector2Int to)> _onSwapFailed = new();

        public Observable<(Vector2Int from, Vector2Int to)> OnSwapRequested => _onSwapRequested;
        public Observable<(Vector2Int from, Vector2Int to)> OnSwapSuccess => _onSwapSuccess;
        public Observable<(Vector2Int from, Vector2Int to)> OnSwapFailed => _onSwapFailed;

        private bool _isLocked;

        [Inject]
        public SwapService(BoardService boardService, MatchService matchService)
        {
            _boardService = boardService;
            _matchService = matchService;
        }

        public void Lock() => _isLocked = true;
        public void Unlock() => _isLocked = false;

        public bool TrySwap(Vector2Int from, Vector2Int to)
        {
            if (_isLocked) return false;
            if (!AreNeighbors(from, to)) return false;
            if (!_boardService.IsNormalCell(from.x, from.y)) return false;
            if (!_boardService.IsNormalCell(to.x, to.y)) return false;

            _onSwapRequested.OnNext((from, to));
            _boardService.SwapNodes(from, to);

            var board = _boardService.Board.CurrentValue;
            var matches = _matchService.FindMatches(board, _boardService.Rows, _boardService.Columns);

            if (matches.Count > 0)
            {
                _onSwapSuccess.OnNext((from, to));
                return true;
            }

            _boardService.SwapNodes(from, to);
            _onSwapFailed.OnNext((from, to));
            return false;
        }

        private bool AreNeighbors(Vector2Int a, Vector2Int b)
        {
            var diff = new Vector2Int(Math.Abs(a.x - b.x), Math.Abs(a.y - b.y));
            return (diff.x == 1 && diff.y == 0) || (diff.x == 0 && diff.y == 1);
        }

        public void Dispose()
        {
            _onSwapRequested.Dispose();
            _onSwapSuccess.Dispose();
            _onSwapFailed.Dispose();
        }
    }
}
