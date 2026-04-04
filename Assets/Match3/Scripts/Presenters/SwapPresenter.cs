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
        private readonly SwapService     _swapService;
        private readonly BoardView       _boardView;
        private readonly AnimationConfig _animationConfig;

        private readonly CompositeDisposable _disposables = new();
        private Vector2Int? _selectedCell;

        [Inject]
        public SwapPresenter(
            SwapService     swapService,
            BoardView       boardView,
            AnimationConfig animationConfig)
        {
            _swapService     = swapService;
            _boardView       = boardView;
            _animationConfig = animationConfig;
        }

        public void Initialize()
        {
            _boardView.OnGemClicked += OnGemTapped;

            // Своп удался — меняем визуал + пульс анимация
            _swapService.OnSwapSuccess
                .Subscribe(data =>
                {
                    _boardView.SwapVisualsAt(data.from, data.to);
                    _boardView.GetGemView(data.from)?.PlaySwapPulse(_animationConfig.SwapDuration);
                    _boardView.GetGemView(data.to)?.PlaySwapPulse(_animationConfig.SwapDuration);
                    _selectedCell = null;
                })
                .AddTo(_disposables);

            // Своп не удался — меняем и сразу возвращаем обратно (визуально)
            _swapService.OnSwapFailed
                .Subscribe(data =>
                {
                    // Меняем визуал туда-обратно с паузой
                    _boardView.SwapVisualsAt(data.from, data.to);
                    _boardView.GetGemView(data.from)?.PlaySwapPulse(_animationConfig.SwapDuration, () =>
                        _boardView.SwapVisualsAt(data.from, data.to));
                    _selectedCell = null;
                })
                .AddTo(_disposables);
        }

        private void OnGemTapped(Vector2Int cell)
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

        public void Dispose()
        {
            _boardView.OnGemClicked -= OnGemTapped;
            _disposables.Dispose();
        }
    }
}
