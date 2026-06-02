#nullable enable

using System;
using Match3.Core.Enums;
using UnityEngine;

namespace Match3.Configs
{
    /// <summary>
    /// Каталог всех предметов игры: бусты (иконка + цена) и типы наград (иконка).
    /// Единый источник правды для визуала и стоимости предметов.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemConfig", menuName = "Match3/Configs/Item")]
    public sealed class ItemConfig : ScriptableObject
    {
        [field: SerializeField] public BoostItemEntry[]  BoostItems  { get; private set; } = Array.Empty<BoostItemEntry>();
        [field: SerializeField] public RewardIconEntry[] RewardIcons { get; private set; } = Array.Empty<RewardIconEntry>();

        public Sprite? GetIcon(RewardType type, BoostType boost)
        {
            if (type == RewardType.Boost)
            {
                foreach (var entry in BoostItems)
                    if (entry.BoostType == boost) return entry.Icon;
                return null;
            }

            foreach (var entry in RewardIcons)
                if (entry.RewardType == type) return entry.Icon;

            return null;
        }

        public Sprite? GetBoostIcon(BoostType boost)
        {
            foreach (var entry in BoostItems)
                if (entry.BoostType == boost) return entry.Icon;
            return null;
        }

        public int GetBoostCoinPrice(BoostType boost)
        {
            foreach (var entry in BoostItems)
                if (entry.BoostType == boost) return entry.CoinPrice;
            return 0;
        }
    }

    [Serializable]
    public sealed class BoostItemEntry
    {
        [field: SerializeField] public BoostType BoostType { get; private set; }
        [field: SerializeField] public Sprite    Icon      { get; private set; } = null!;
        [field: SerializeField] public int       CoinPrice { get; private set; }
    }

    [Serializable]
    public sealed class RewardIconEntry
    {
        [field: SerializeField] public RewardType RewardType { get; private set; }
        [field: SerializeField] public Sprite     Icon       { get; private set; } = null!;
    }
}
