#nullable enable

using System;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services.Inventory;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services
{
    /// <summary>
    /// Выдаёт награды из RewardData[].
    /// Живёт в ProjectContext.
    ///
    /// Поддерживаемые типы:
    ///   Boost → InventoryService.Add(boost, amount)
    ///   Coins → CoinService.Add(amount)
    ///   Lives → LivesService.AddLives(amount)
    /// </summary>
    public sealed class RewardService : IDisposable
    {
        private readonly InventoryService _inventoryService;
        private readonly CoinService      _coinService;
        private readonly LivesService     _livesService;

        private readonly Subject<RewardData> _onRewardGranted = new();

        /// <summary>
        /// Срабатывает на каждую выданную награду.
        /// Presenter слушает и показывает анимацию/попап.
        /// </summary>
        public Observable<RewardData> OnRewardGranted => _onRewardGranted;

        [Inject]
        public RewardService(
            InventoryService inventoryService,
            CoinService      coinService,
            LivesService     livesService)
        {
            _inventoryService = inventoryService;
            _coinService      = coinService;
            _livesService     = livesService;
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
                Debug.LogWarning(
                    $"[RewardService] Пропущена награда {reward} — Amount <= 0");
                return;
            }

            switch (reward.Type)
            {
                case RewardType.Boost:
                    GrantBoost(reward);
                    break;

                case RewardType.Coins:
                    _coinService.Add(reward.Amount);
                    break;

                case RewardType.Lives:
                    _livesService.AddLives(reward.Amount);
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
                Debug.LogWarning(
                    "[RewardService] Boost == None — укажи конкретный тип буста в конфиге");
                return;
            }

            _inventoryService.Add(reward.Boost, reward.Amount);
        }

        public void Dispose() => _onRewardGranted.Dispose();
    }
}
