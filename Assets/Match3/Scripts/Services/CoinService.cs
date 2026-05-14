#nullable enable

using System;
using Match3.Configs;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services
{
    /// <summary>
    /// Хранит монеты между сессиями (PlayerPrefs).
    /// Живёт в ProjectContext.
    /// </summary>
    public sealed class CoinService : IDisposable
    {
        private const string SaveKey = "wallet_coins";

        private readonly ReactiveProperty<int> _coins;
        private readonly int _initialCoins;

        public ReadOnlyReactiveProperty<int> Coins => _coins;

        [Inject]
        public CoinService(EconomyConfig config)
        {
            _initialCoins = config.InitialCoins;
            _coins = new ReactiveProperty<int>(Load());
        }

        // ── Публичный API ─────────────────────────────────────────────────────

        public void Add(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            _coins.Value += amount;
            Save();
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (_coins.Value < amount)
            {
                Debug.LogWarning($"[CoinService] Недостаточно монет: нужно {amount}, есть {_coins.Value}");
                return false;
            }

            _coins.Value -= amount;
            Save();
            return true;
        }

        // ── PlayerPrefs ───────────────────────────────────────────────────────

        private int Load() => PlayerPrefs.GetInt(SaveKey, _initialCoins);

        private void Save()
        {
            PlayerPrefs.SetInt(SaveKey, _coins.Value);
            PlayerPrefs.Save();
        }

        public void Dispose() => _coins.Dispose();
    }
}
