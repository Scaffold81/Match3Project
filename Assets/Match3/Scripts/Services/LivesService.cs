#nullable enable

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services
{
    /// <summary>
    /// Хранит жизни и таймер регенерации между сессиями (PlayerPrefs).
    /// Живёт в ProjectContext.
    ///
    /// Ключи PlayerPrefs:
    ///   "wallet_lives"           → int    — текущее количество жизней
    ///   "wallet_lives_timestamp" → string — Unix-секунды момента прихода следующей жизни
    ///                              "0" = таймер не запущен (жизни полные)
    ///
    /// Логика таймера:
    ///   При потере жизни с максимума → nextLifeAt = now + RegenSeconds.
    ///   При уже запущенном таймере   → nextLifeAt не сбрасывается.
    ///   При достижении максимума     → nextLifeAt сбрасывается в 0.
    ///   При добавлении жизней сверх максимума → игнорируется молча.
    /// </summary>
    public sealed class LivesService : IDisposable
    {
        private const string LivesKey     = "wallet_lives";
        private const string TimestampKey = "wallet_lives_timestamp";

        private readonly EconomyConfig _config;
        private readonly long          _regenSeconds;

        private readonly ReactiveProperty<int>      _lives             = new();
        private readonly ReactiveProperty<TimeSpan> _timeUntilNextLife = new(TimeSpan.Zero);

        private readonly CancellationTokenSource _cts = new();

        private long _nextLifeAt; // Unix-секунды; 0 = таймер не запущен

        public ReadOnlyReactiveProperty<int>      Lives             => _lives;
        public ReadOnlyReactiveProperty<TimeSpan> TimeUntilNextLife => _timeUntilNextLife;
        public int                                MaxLives          => _config.MaxLives;

        [Inject]
        public LivesService(EconomyConfig config)
        {
            _config       = config;
            _regenSeconds = (long)config.LifeRegenSeconds;

            LoadAll();
            Tick(); // восстановить жизни накопленные пока приложение было закрыто

            StartTimerLoopAsync(_cts.Token).Forget();
        }

        // ── Публичный API ─────────────────────────────────────────────────────

        /// <summary>
        /// Тратит одну жизнь. Возвращает false если жизней нет.
        /// </summary>
        public bool TrySpendLife()
        {
            if (_lives.Value <= 0)
                return false;

            bool wasAtMax = _lives.Value == _config.MaxLives;
            _lives.Value--;

            if (wasAtMax)
                _nextLifeAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _regenSeconds;

            SaveAll();
            return true;
        }

        /// <summary>
        /// Добавляет жизни. Если жизни уже на максимуме — молча игнорирует.
        /// </summary>
        public void AddLives(int amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount));

            if (_lives.Value >= _config.MaxLives)
                return;

            _lives.Value = Math.Min(_lives.Value + amount, _config.MaxLives);

            if (_lives.Value >= _config.MaxLives)
                _nextLifeAt = 0;

            SaveAll();
        }

        // ── Таймер ────────────────────────────────────────────────────────────

        private async UniTaskVoid StartTimerLoopAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: ct);
                    Tick();
                }
            }
            catch (OperationCanceledException) { }
        }

        private void Tick()
        {
            if (_lives.Value >= _config.MaxLives)
            {
                _timeUntilNextLife.Value = TimeSpan.Zero;
                return;
            }

            if (_nextLifeAt == 0)
                return;

            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (now >= _nextLifeAt)
            {
                long elapsed = now - _nextLifeAt;
                int gained   = (int)(elapsed / _regenSeconds) + 1;
                gained       = Math.Min(gained, _config.MaxLives - _lives.Value);

                _lives.Value += gained;

                if (_lives.Value >= _config.MaxLives)
                {
                    _nextLifeAt              = 0;
                    _timeUntilNextLife.Value = TimeSpan.Zero;
                }
                else
                {
                    _nextLifeAt             += (long)gained * _regenSeconds;
                    _timeUntilNextLife.Value = TimeSpan.FromSeconds(_nextLifeAt - now);
                }

                SaveAll();
            }
            else
            {
                _timeUntilNextLife.Value = TimeSpan.FromSeconds(_nextLifeAt - now);
            }
        }

        // ── PlayerPrefs ───────────────────────────────────────────────────────

        private void LoadAll()
        {
            _lives.Value = PlayerPrefs.GetInt(LivesKey, _config.MaxLives);

            var raw = PlayerPrefs.GetString(TimestampKey, "0");
            _nextLifeAt = long.TryParse(raw, out var v) ? v : 0;
        }

        private void SaveAll()
        {
            PlayerPrefs.SetInt(LivesKey, _lives.Value);
            PlayerPrefs.SetString(TimestampKey, _nextLifeAt.ToString());
            PlayerPrefs.Save();
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _lives.Dispose();
            _timeUntilNextLife.Dispose();
        }
    }
}
