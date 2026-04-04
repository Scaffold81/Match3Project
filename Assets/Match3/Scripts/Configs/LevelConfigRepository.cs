#nullable enable

using System;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "LevelConfigRepository", menuName = "Match3/Configs/LevelRepository")]
    public sealed class LevelConfigRepository : ScriptableObject
    {
        [field: SerializeField] public LevelConfig[] Levels { get; private set; } = Array.Empty<LevelConfig>();

        public int Count => Levels.Length;

        public LevelConfig? GetLevel(int index)
        {
            if (index < 0 || index >= Levels.Length)
            {
                Debug.LogWarning($"LevelConfigRepository: index {index} out of range (count={Levels.Length})");
                return null;
            }
            return Levels[index];
        }

        public LevelConfig? First => Levels.Length > 0 ? Levels[0] : null;
    }
}
