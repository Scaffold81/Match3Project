#nullable enable

using System;
using Match3.Core.Enums;
using UnityEngine;

namespace Match3.Core.Models
{
    /// <summary>
    /// Одна награда — тип + количество + конкретный буст если нужен.
    /// Хранится в LevelConfig.Rewards[].
    /// </summary>
    [Serializable]
    public struct RewardData
    {
        [field: SerializeField]
        [field: Tooltip("Тип награды")]
        public RewardType Type { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Буст (только если Type == Boost)")]
        public BoostType Boost { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Количество")]
        public int Amount { get; private set; }

        public RewardData(RewardType type, BoostType boost, int amount)
        {
            Type   = type;
            Boost  = boost;
            Amount = amount;
        }

        public override string ToString() =>
            Type == RewardType.Boost
                ? $"{Amount}x {Boost}"
                : $"{Amount}x {Type}";
    }
}
