#nullable enable

using System;
using R3;

namespace Match3.Services.MoveCounter
{
    public sealed class MoveCounterService : IDisposable
    {
        private readonly ReactiveProperty<int> _movesUsed = new(0);
        private readonly ReactiveProperty<int> _movesLeft = new(0);
        private readonly Subject<Unit> _onMovesExhausted = new();

        public ReadOnlyReactiveProperty<int> MovesUsed => _movesUsed;
        public ReadOnlyReactiveProperty<int> MovesLeft => _movesLeft;
        public Observable<Unit> OnMovesExhausted => _onMovesExhausted;

        public bool IsLimited { get; private set; }
        public bool IsExhausted => IsLimited && _movesLeft.Value <= 0;

        public void Initialize(int moveLimit)
        {
            if (moveLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(moveLimit));

            _movesUsed.Value = 0;
            IsLimited = moveLimit > 0;
            _movesLeft.Value = moveLimit;
        }

        public bool UseMove()
        {
            if (IsLimited && IsExhausted)
                return false;

            _movesUsed.Value++;

            if (IsLimited)
            {
                _movesLeft.Value--;

                if (_movesLeft.Value <= 0)
                    _onMovesExhausted.OnNext(Unit.Default);
            }

            return true;
        }

        public void Dispose()
        {
            _movesUsed.Dispose();
            _movesLeft.Dispose();
            _onMovesExhausted.Dispose();
        }
    }
}
