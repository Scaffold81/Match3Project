#nullable enable

using System;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "StageConfig", menuName = "Match3/Map/Stage")]
    public sealed class StageConfig : ScriptableObject
    {
        [field: SerializeField]
        [field: Tooltip("Название этапа (например: 'Пирамида Хеопса')")]
        public string StageName { get; private set; } = string.Empty;

        [field: SerializeField]
        [field: Tooltip("Иконка этапа на карте")]
        public Sprite StageIcon { get; private set; } = null!;

        [field: SerializeField]
        [field: Tooltip("Если true — это бонусный этап. Открывается когда все обычные этапы страны пройдены. Даёт супер-приз.")]
        public bool IsBonusStage { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Супер-приз за прохождение бонусного этапа (иконка). Игнорируется для обычных этапов.")]
        public Sprite? SuperPrize { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Три уровня этапа. Всегда ровно 3.")]
        public LevelConfig[] Levels { get; private set; } = Array.Empty<LevelConfig>();

        public int LevelCount => Levels.Length;

        public LevelConfig? GetLevel(int index)
        {
            if (index < 0 || index >= Levels.Length) return null;
            return Levels[index];
        }
    }
}
