#nullable enable

using System;
using Match3.Core.Enums;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "RewardIconConfig", menuName = "Match3/Configs/RewardIcons")]
    public sealed class RewardIconConfig : ScriptableObject
    {
        [field: SerializeField] public BoostIconEntry[]  BoostIcons  { get; private set; } = Array.Empty<BoostIconEntry>();
        [field: SerializeField] public RewardIconEntry[] RewardIcons { get; private set; } = Array.Empty<RewardIconEntry>();

        public Sprite? GetIcon(RewardType type, BoostType boost)
        {
            if (type == RewardType.Boost)
            {
                foreach (var entry in BoostIcons)
                    if (entry.BoostType == boost)
                        return entry.Icon;
                return null;
            }

            foreach (var entry in RewardIcons)
                if (entry.RewardType == type)
                    return entry.Icon;

            return null;
        }
    }

    [Serializable]
    public sealed class BoostIconEntry
    {
        [field: SerializeField] public BoostType BoostType { get; private set; }
        [field: SerializeField] public Sprite    Icon      { get; private set; } = null!;
    }

    [Serializable]
    public sealed class RewardIconEntry
    {
        [field: SerializeField] public RewardType RewardType { get; private set; }
        [field: SerializeField] public Sprite     Icon       { get; private set; } = null!;
    }
}
