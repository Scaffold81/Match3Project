#nullable enable
using System;
using Match3.Core.Models;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "AdConfig", menuName = "Match3/Configs/Ad")]
    public sealed class AdConfig : ScriptableObject
    {
        [field: SerializeField] public string AppIdAndroid                  { get; private set; } = string.Empty;
        [field: SerializeField] public string AppIdIos                      { get; private set; } = string.Empty;
        [field: SerializeField] public int    InterstitialCooldownSeconds   { get; private set; } = 30;
        [field: SerializeField] public int    MinLevelsBetweenInterstitials { get; private set; } = 3;
        [field: SerializeField] public AdPlacementEntry[] Placements        { get; private set; } = Array.Empty<AdPlacementEntry>();

        public AdPlacementEntry? GetPlacement(AdPlacementId id)
        {
            foreach (var p in Placements)
                if (p.PlacementId == id) return p;
            return null;
        }
    }

    [Serializable]
    public sealed class AdPlacementEntry
    {
        [field: SerializeField] public AdPlacementId PlacementId   { get; private set; }
        [field: SerializeField] public string        UnitIdAndroid { get; private set; } = string.Empty;
        [field: SerializeField] public string        UnitIdIos     { get; private set; } = string.Empty;
        [field: SerializeField] public RewardData[]  Rewards       { get; private set; } = Array.Empty<RewardData>();
    }
}
