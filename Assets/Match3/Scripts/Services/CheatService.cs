#nullable enable

using Match3.Configs;
using Match3.Core.Models;

namespace Match3.Services
{
    /// <summary>
    /// Читы для разработки. Не использовать в продакшн-сборках.
    /// </summary>
    public sealed class CheatService
    {
        private const int StagesPerCountry = 10;
        private const int LevelsPerStage   = 3;

        private readonly ProgressService _progressService;
        private readonly WorldMapConfig  _worldMapConfig;

        public CheatService(ProgressService progressService, WorldMapConfig worldMapConfig)
        {
            _progressService = progressService;
            _worldMapConfig  = worldMapConfig;
        }

        /// <summary>
        /// Ставит 1 звезду на каждый уровень каждого этапа каждой страны.
        /// </summary>
        public void UnlockAll()
        {
            for (var c = 0; c < _worldMapConfig.CountryCount; c++)
            for (var s = 0; s < StagesPerCountry; s++)
            for (var l = 0; l < LevelsPerStage; l++)
                _progressService.SetStars(new LevelAddress(c, s, l), 1);
        }

        /// <summary>
        /// Сбрасывает весь прогресс — все уровни закрыты, кроме первого.
        /// </summary>
        public void LockAll() => _progressService.ResetAllProgress();
    }
}
