#nullable enable

using System;
using Match3.Configs;
using Match3.Services.Swap;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class SwapPresenter : IInitializable, IDisposable
    {
        private readonly SwapService _swapService;
        private readonly BoardView _boardView;
        private readonly AnimationConfig _animationConfig;

        private readonly CompositeDisposable _disposables = new();

        private Vector2Int? _selectedCell;

        [Inject]
        public SwapPresenter(
            SwapService swapService,
            BoardView boardView,
            AnimationConfig animationConfig)
        {
            _swapService = swapService;
            _boardView = boardView;
            _animationConfig = animationConfig;
        }

        public void Initialize()
        {
            _swapService.OnSwapRequested
                .Subscribe(data => AnimateSwap(data.from, data.to))
                .AddTo(_disposables);

            _swapService.OnSwapFailed
                .Subscribe(data => AnimateReturn(data.from, data.to))
                .AddTo(_disposables);

            _swapService.OnSwapSuccess
                .Subscribe(_ => _selectedCell = null)
                .AddTo(_disposables);
        }

        public void OnCellTapped(Vector2Int cell)
        {
            if (_selectedCell == null)
            {
                _selectedCell = cell;
                return;
            }

            var from = _selectedCell.Value;
            _selectedCell = null;

            _swapService.TrySwap(from, cell);
        }

        private void AnimateSwap(Vector2Int from, Vector2Int to)
        {
            var gemA = _boardView.GetGemView(from);
            var gemB = _boardView.GetGemView(to);

            if (gemA == null || gemB == null) return;

            var posA = _boardView.GetAnchoredPosition(from.x, from.y);
            var posB = _boardView.GetAnchoredPosition(to.x, to.y);

            _boardView.MoveGem(from, to);
            _boardView.MoveGem(to, from);

            gemA.PlaySwap(posB, _animationConfig.SwapDuration);
            gemB.PlaySwap(posA, _animationConfig.SwapDuration);
        }

        private void AnimateReturn(Vector2Int from, Vector2Int to)
        {
            var gemA = _boardView.GetGemView(to);
            var gemB = _boardView.GetGemView(from);

            if (gemA == null || gemB == null) return;

            var posA = _boardView.GetAnchoredPosition(from.x, from.y);
            var posB = _boardView.GetAnchoredPosition(to.x, to.y);

            gemA.PlayReturn(posA, _animationConfig.SwapReturnDuration);
            gemB.PlayReturn(posB, _animationConfig.SwapReturnDuration);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
