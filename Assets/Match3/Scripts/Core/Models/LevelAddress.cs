#nullable enable

using System;

namespace Match3.Core.Models
{
    [Serializable]
    public struct LevelAddress : IEquatable<LevelAddress>
    {
        public int CountryIndex;
        public int StageIndex;
        public int LevelIndex;

        public LevelAddress(int countryIndex, int stageIndex, int levelIndex)
        {
            CountryIndex = countryIndex;
            StageIndex   = stageIndex;
            LevelIndex   = levelIndex;
        }

        public bool Equals(LevelAddress other) =>
            CountryIndex == other.CountryIndex &&
            StageIndex   == other.StageIndex   &&
            LevelIndex   == other.LevelIndex;

        public override bool Equals(object? obj) =>
            obj is LevelAddress other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(CountryIndex, StageIndex, LevelIndex);

        public override string ToString() =>
            $"Country[{CountryIndex}] Stage[{StageIndex}] Level[{LevelIndex}]";

        public static bool operator ==(LevelAddress a, LevelAddress b) => a.Equals(b);
        public static bool operator !=(LevelAddress a, LevelAddress b) => !a.Equals(b);
    }
}
