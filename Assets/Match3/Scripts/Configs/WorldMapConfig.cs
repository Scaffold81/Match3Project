#nullable enable

using System;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "WorldMapConfig", menuName = "Match3/Map/WorldMap")]
    public sealed class WorldMapConfig : ScriptableObject
    {
        [field: SerializeField]
        [field: Tooltip("5 стран в порядке прохождения")]
        public CountryConfig[] Countries { get; private set; } = Array.Empty<CountryConfig>();

        public int CountryCount => Countries.Length;

        public CountryConfig? GetCountry(int index)
        {
            if (index < 0 || index >= Countries.Length) return null;
            return Countries[index];
        }

        public StageConfig? GetStage(int countryIndex, int stageIndex) =>
            GetCountry(countryIndex)?.GetStage(stageIndex);

        public LevelConfig? GetLevel(int countryIndex, int stageIndex, int levelIndex) =>
            GetStage(countryIndex, stageIndex)?.GetLevel(levelIndex);
    }
}
