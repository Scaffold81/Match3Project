#nullable enable

using System;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services.Inventory;
using R3;
using Zenject;

namespace Match3.Services
{
    /// <summary>
    /// Выдаёт награды из RewardData[].
    /// Живёт в ProjectContext.
    ///
    /// Поддерживаемые типы:
    ///   Boost → InventoryService.Add(boost, amount)
    ///   Coins → CoinService (заглушка — расширить когда появится)
    ///   Lives → LivesService (заглушка — расширить когда появится)
    /// </summary>
    public sealed class RewardService
    {
        private readonly InventoryService _inventoryService;

        private readonly Subject<RewardData> _onRewardGranted = new();

        /// <summary>
        /// Срабатывает на каждую выданную награду.
        /// Presenter слушает и показывает анимацию/попап.
        /// </summary>
        public Observable<RewardData> OnRewardGranted => _onRewardGranted;

        [Inject]
        public RewardService(InventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        /// <summary>
        /// Выдаёт все награды из массива.
        /// Вызывать после победы на уровне.
        /// </summary>
        public void GrantAll(RewardData[] rewards)
        {
            if (rewards.Length == 0) return;

            foreach (var reward in rewards)
                GrantOne(reward);
        }

        // ── Приватная логика ──────────────────────────────────────────────────

        private void GrantOne(RewardData reward)
        {
            if (reward.Amount <= 0)
            {
                UnityEngine.Debug.LogWarning(
                    $"[RewardService] Пропущена награда {reward} — Amount <= 0");
                return;
            }

            switch (reward.Type)
            {
                case RewardType.Boost:
                    GrantBoost(reward);
                    break;

                case RewardType.Coins:
                    GrantCoins(reward);
                    break;

                case RewardType.Lives:
                    GrantLives(reward);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(reward.Type), reward.Type, null);
            }

            _onRewardGranted.OnNext(reward);
        }

        private void GrantBoost(RewardData reward)
        {
            if (reward.Boost == BoostType.None)
            {
                UnityEngine.Debug.LogWarning(
                    "[RewardService] Boost == None — укажи конкретный тип буста в конфиге");
                return;
            }

            _inventoryService.Add(reward.Boost, reward.Amount);
        }

        private static void GrantCoins(RewardData reward)
        {
            // TODO: CoinService.Add(reward.Amount) — добавить когда появится
            UnityEngine.Debug.LogWarning(
                $"[RewardService] Coins +{reward.Amount} (заглушка)");
        }

        private static void GrantLives(RewardData reward)
        {
            // TODO: LivesService.Add(reward.Amount) — добавить когда появится
            UnityEngine.Debug.LogWarning(
                $"[RewardService] Lives +{reward.Amount} (заглушка)");
        }

        public void Dispose() => _onRewardGranted.Dispose();
    }
}
