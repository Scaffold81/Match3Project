#nullable enable

using System;
using System.Collections.Generic;
using Match3.Services.Board;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services.Swap
{
    public sealed class SwapService : IDisposable
    {
        private readonly BoardService _boardService;

        private readonly Subject<(Vector2Int from, Vector2Int to)> _onSwapRequested = new();
        public Observable<(Vector2Int from, Vector2Int to)> OnSwapRequested => _onSwapRequested;

        private bool        _isLocked;
        private Vector2Int? _firstCell;

        [Inject]
        public SwapService(BoardService boardService)
        {
            _boardService = boardService;
        }

        public void Lock() => _isLocked = true;

        public void Unlock()
        {
            _isLocked  = false;
            _firstCell = null;
        }

        /// <summary>
        /// Сбрасывает выделение первой фишки без снятия глобального лока.
        /// Вызывается при активации или отмене буста — чтобы ранее выбранная
        /// фишка не инициировала случайный своп после завершения буста.
        /// </summary>
        public void ClearSelection() => _firstCell = null;

        /// <summary>
        /// Вызывается при клике на ячейку.
        /// Первый клик — запоминаем. Второй клик на соседа — запускаем своп.
        /// </summary>
        public void TrySelect(Vector2Int pos)
        {
            if (_isLocked) return;

            if (!_boardService.IsNormalCell(pos)) return;

            if (!_boardService.TryGetCell(pos, out var cell) || !cell.CanBeMoved) return;

            if (_firstCell == null)
            {
                _firstCell = pos;
                return;
            }

            var first = _firstCell.Value;

            if (first == pos)
            {
                _firstCell = null;
                return;
            }

            if (!_boardService.AreNeighbors(first, pos))
            {
                _firstCell = pos;
                return;
            }

            _firstCell = null;

            _boardService.LockCell(first, true);
            _boardService.LockCell(pos,   true);
            _onSwapRequested.OnNext((first, pos));
        }

        public List<(Vector2Int from, Vector2Int to)> FindAllPossibleSwaps() =>
            _boardService.FindAllPossibleSwaps();

        public void Dispose() => _onSwapRequested.Dispose();
    }
}
