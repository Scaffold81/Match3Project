#nullable enable

namespace Match3.Services
{
    /// <summary>
    /// Считает количество звёзд за уровень по оставшимся ходам.
    /// </summary>
    public static class StarCalculator
    {
        private const float ThreeStarThreshold = 0.6f;
        private const float TwoStarThreshold   = 0.3f;

        /// <summary>
        /// moveLimit == 0 → режим без ограничений → всегда 3 звезды.
        /// </summary>
        public static int Calculate(int movesLeft, int moveLimit)
        {
            if (moveLimit <= 0) return 3;

            var ratio = (float)movesLeft / moveLimit;
            if (ratio >= ThreeStarThreshold) return 3;
            if (ratio >= TwoStarThreshold)   return 2;
            return 1;
        }
    }
}
