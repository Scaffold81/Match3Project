#nullable enable
using System;
using Match3.Core.Models;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "ShopConfig", menuName = "Match3/Configs/Shop")]
    public sealed class ShopConfig : ScriptableObject
    {
        [field: SerializeField] public ShopItemData[] Items { get; private set; } = Array.Empty<ShopItemData>();

        public ShopItemData? FindById(string purchaseId)
        {
            foreach (var item in Items)
            {
                if (item.PurchaseId == purchaseId)
                    return item;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class ShopItemData
    {
        [field: SerializeField] public string     PurchaseId { get; private set; } = string.Empty;
        [field: SerializeField] public int        CoinCost   { get; private set; }
        [field: SerializeField] public Sprite?    Icon       { get; private set; }
        [field: SerializeField] public string     Title      { get; private set; } = string.Empty;
        [field: SerializeField] public RewardData[] Rewards  { get; private set; } = Array.Empty<RewardData>();
    }
}
