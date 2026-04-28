#nullable enable

using System;
using System.Collections.Generic;
using Match3.Core.Enums;
using R3;
using UnityEngine;

namespace Match3.Services.Inventory
{
    /// <summary>
    /// Хранит количество бустов между сессиями (PlayerPrefs).
    /// Живёт в ProjectContext — не уничтожается при смене сцены.
    /// </summary>
    public sealed class InventoryService : IDisposable
    {
        private const string SaveKeyPrefix = "inventory_boost_";

        public static readonly BoostType[] AllBoosts =
        {
            BoostType.HorizontalArrow,
            BoostType.VerticalArrow,
            BoostType.ColorBomb,
            BoostType.Bomb,
            BoostType.MegaBomb,
            BoostType.Hint,
            BoostType.Shuffle,
        };

        private readonly Dictionary<BoostType, ReactiveProperty<int>> _counts = new();

        public InventoryService()
        {
            foreach (var boost in AllBoosts)
                _counts[boost] = new ReactiveProperty<int>(Load(boost));
        }

        // ── Публичный API ─────────────────────────────────────────────────────

        public ReadOnlyReactiveProperty<int> GetCount(BoostType boost) =>
            _counts[boost];

        public bool HasAny(BoostType boost) =>
            _counts.TryGetValue(boost, out var prop) && prop.Value > 0;

        public void Add(BoostType boost, int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (!_counts.TryGetValue(boost, out var prop)) return;
            prop.Value += amount;
            Save(boost, prop.Value);
            Debug.LogWarning($"[InventoryService] +{amount} {boost} → итого {prop.Value}");
        }

        public bool TrySpend(BoostType boost)
        {
            if (!_counts.TryGetValue(boost, out var prop) || prop.Value <= 0)
            {
                Debug.LogWarning($"[InventoryService] Нет {boost} в инвентаре");
                return false;
            }

            prop.Value--;
            Save(boost, prop.Value);
            Debug.LogWarning($"[InventoryService] -{1} {boost} → осталось {prop.Value}");
            return true;
        }

        /// <summary>
        /// Временная функция — удалить когда будет реальный источник наград.
        /// Вызывается в начале каждого уровня через GameLoopController.Initialize.
        /// </summary>
        public void AddDebugStarterPack()
        {
            const int Amount = 1000;
            foreach (var boost in AllBoosts)
                Add(boost, Amount);
            Debug.LogWarning($"[InventoryService] ⚠️ DEBUG: +{Amount} каждого буста");
        }

        // ── PlayerPrefs ───────────────────────────────────────────────────────

        private static int Load(BoostType boost) =>
            PlayerPrefs.GetInt(SaveKeyPrefix + boost, 0);

        private static void Save(BoostType boost, int value)
        {
            PlayerPrefs.SetInt(SaveKeyPrefix + boost, value);
            PlayerPrefs.Save();
        }

        public void Dispose()
        {
            foreach (var prop in _counts.Values)
                prop.Dispose();
        }
    }
}
