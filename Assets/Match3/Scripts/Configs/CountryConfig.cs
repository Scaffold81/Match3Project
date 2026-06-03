#nullable enable

using System;
using Match3.Core.Models;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "CountryConfig", menuName = "Match3/Map/Country")]
    public sealed class CountryConfig : ScriptableObject
    {
        [field: SerializeField]
        [field: Tooltip("Название страны (например: 'Египет')")]
        public string CountryName { get; private set; } = string.Empty;

        [field: SerializeField]
        [field: Tooltip("Иконка страны — используется на большой кнопке")]
        public Sprite CountryIcon { get; private set; } = null!;

        [field: SerializeField]
        [field: Tooltip("Иконка заблокированной страны")]
        public Sprite LockedIcon { get; private set; } = null!;

        [field: SerializeField]
        [field: Tooltip("Фоновый цвет или спрайт для секции этой страны на карте")]
        public Color SectionColor { get; private set; } = Color.white;

        [field: SerializeField]
        [field: Tooltip("Фоновый спрайт для игровой сцены (используется если у этапа нет своего override)")]
        public Sprite? GameBackgroundSprite { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Персонаж на попапе завершения страны")]
        public Sprite? CharacterSprite { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Награды за полное прохождение страны (бонусный этап пройден).")]
        public RewardData[] CountryRewards { get; private set; } = Array.Empty<RewardData>();

        [field: SerializeField]
        [field: Tooltip("9 этапов страны. Всегда ровно 9.")]
        public StageConfig[] Stages { get; private set; } = Array.Empty<StageConfig>();

        public int StageCount => Stages.Length;

        public StageConfig? GetStage(int index)
        {
            if (index < 0 || index >= Stages.Length) return null;
            return Stages[index];
        }
    }
}
