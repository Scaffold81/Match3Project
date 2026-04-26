#nullable enable

using System;
using Match3.Configs;
using Match3.Core;
using Match3.Services.Swap;
using R3;
using Zenject;

namespace Match3.Presenters
{
    /// <summary>
    /// Отвечает только за визуальный фидбек при свапе.
    /// Логика ввода и валидации — в GameLoopController + BoardInputHandler.
    /// </summary>
    public sealed class SwapPresenter : IInitializable, IDisposable
    {
        private readonly SwapService     _swapService;
        private readonly AnimationConfig _animConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public SwapPresenter(SwapService swapService, AnimationConfig animConfig)
        {
            _swapService = swapService;
            _animConfig  = animConfig;
        }

        public void Initialize()
        {
            // Визуальные эффекты можно расширить здесь:
            // например, подсвечивать подсказки через _swapService.FindAllPossibleSwaps()
            _ = _swapService;
            _ = _animConfig;
        }

        public void Dispose() => _disposables.Dispose();
    }
}
