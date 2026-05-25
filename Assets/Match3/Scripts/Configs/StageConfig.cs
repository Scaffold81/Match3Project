#nullable enable

using System;
using Match3.Core.Models;
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
        [field: Tooltip("Арт персонажа на панели выбора уровня")]
        public Sprite? CharacterSprite { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Арт грустного персонажа на панели поражения")]
        public Sprite? SadCharacterSprite { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Награды за прохождение всех 3 уровней этапа. Выдаются через RewardService.")]
        public RewardData[] StageRewards { get; private set; } = Array.Empty<RewardData>();

        [field: SerializeField]
        [field: Tooltip("Если true — это бонусный этап. Открывается когда все обычные этапы страны пройдены.")]
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

        /// <summary>
        /// Возвращает индекс следующего непройденного уровня в этапе.
        /// completedMask — массив bool[3]: true если уровень пройден.
        /// </summary>
        public int GetNextLevelIndex(bool[] completedMask)
        {
            for (var i = 0; i < completedMask.Length; i++)
                if (!completedMask[i]) return i;
            return 0;
        }
    }
}
