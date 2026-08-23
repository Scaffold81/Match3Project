#nullable enable

using System;
using Match3.Configs;
using Match3.Core.Models;
using R3;
using UnityEngine;

namespace Match3.Services
{
    /// <summary>
    /// Хранит прогресс игрока между сессиями (PlayerPrefs).
    /// Живёт в ProjectContext.
    ///
    /// Структура страны: 9 обычных этапов (0-8) + 1 бонусный (9).
    /// Бонусный этап открывается когда все 9 обычных завершены.
    ///
    /// Ключи PlayerPrefs:
    ///   progress_stars_{c}_{s}_{l}  — звёзды уровня (0-3)
    ///   progress_current_c/s/l      — текущий адрес
    /// </summary>
    public sealed class ProgressService : IDisposable
    {
        private const string StarsKey          = "progress_stars_{0}_{1}_{2}";
        private const string CurrentKey        = "progress_current";
        private const string PendingCountryKey = "progress_pending_country";

        private const int RegularStageCount = 9;  // индексы 0-8
        private const int BonusStageIndex   = 9;  // индекс бонусного этапа
        private const int TotalStageCount   = 10; // 9 обычных + 1 бонусный

        private readonly WorldMapConfig _worldMapConfig;

        private readonly ReactiveProperty<LevelAddress> _currentAddress = new();
        public ReadOnlyReactiveProperty<LevelAddress> CurrentAddress => _currentAddress;

        public ProgressService(WorldMapConfig worldMapConfig)
        {
            _worldMapConfig = worldMapConfig;
            _currentAddress.Value = LoadCurrentAddress();
        }

        // ── Звёзды ───────────────────────────────────────────────────────────

        public int GetStars(int countryIdx, int stageIdx, int levelIdx)
        {
            var key = string.Format(StarsKey, countryIdx, stageIdx, levelIdx);
            return PlayerPrefs.GetInt(key, 0);
        }

        public int GetStars(LevelAddress address) =>
            GetStars(address.CountryIndex, address.StageIndex, address.LevelIndex);

        public void SetStars(LevelAddress address, int stars)
        {
            if (stars < 1 || stars > 3)
                throw new ArgumentOutOfRangeException(nameof(stars));

            var current = GetStars(address);
            if (stars <= current) return;

            var key = string.Format(StarsKey,
                address.CountryIndex, address.StageIndex, address.LevelIndex);
            PlayerPrefs.SetInt(key, stars);
            PlayerPrefs.Save();

            Debug.LogWarning($"[ProgressService] {address} → {stars}★");
        }

        // ── Суммарные звёзды ─────────────────────────────────────────────────

        public int GetStageStars(int countryIdx, int stageIdx)
        {
            var total = 0;
            for (var l = 0; l < 3; l++)
                total += GetStars(countryIdx, stageIdx, l);
            return total;
        }

        /// <summary>
        /// Звёзды только за обычные этапы страны (0-8), без бонусного.
        /// </summary>
        public int GetCountryStars(int countryIdx)
        {
            var total = 0;
            for (var s = 0; s < RegularStageCount; s++)
                total += GetStageStars(countryIdx, s);
            return total;
        }

        // ── Разблокировка ────────────────────────────────────────────────────

        public bool IsStageUnlocked(int countryIdx, int stageIdx)
        {
            // Бонусный этап — открывается когда все 9 обычных этапов пройдены
            if (stageIdx == BonusStageIndex)
                return AreAllRegularStagesCompleted(countryIdx);

            // Первый этап первой страны всегда открыт
            if (countryIdx == 0 && stageIdx == 0) return true;

            // Обычный этап — открывается когда предыдущий завершён
            if (stageIdx > 0)
                return IsStageCompleted(countryIdx, stageIdx - 1);

            // Первый этап страны — нужно завершить бонусный этап предыдущей страны
            if (countryIdx > 0)
                return IsStageCompleted(countryIdx - 1, BonusStageIndex);

            return false;
        }

        public bool IsStageCompleted(int countryIdx, int stageIdx)
        {
            for (var l = 0; l < 3; l++)
                if (GetStars(countryIdx, stageIdx, l) == 0)
                    return false;
            return true;
        }

        /// <summary>
        /// Все 9 обычных этапов страны завершены (бонусный не считается).
        /// </summary>
        private bool AreAllRegularStagesCompleted(int countryIdx)
        {
            for (var s = 0; s < RegularStageCount; s++)
                if (!IsStageCompleted(countryIdx, s))
                    return false;
            return true;
        }

        /// <summary>
        /// Страна полностью завершена когда пройден бонусный этап.
        /// </summary>
        public bool IsCountryCompleted(int countryIdx) =>
            IsStageCompleted(countryIdx, BonusStageIndex);

        public bool IsCountryUnlocked(int countryIdx)
        {
            if (countryIdx == 0) return true;
            return IsCountryCompleted(countryIdx - 1);
        }

        public bool IsLevelUnlocked(int countryIdx, int stageIdx, int levelIdx)
        {
            if (!IsStageUnlocked(countryIdx, stageIdx)) return false;
            if (levelIdx == 0) return true;
            return GetStars(countryIdx, stageIdx, levelIdx - 1) > 0;
        }

        /// <summary>
        /// Сбрасывает весь прогресс: все звёзды обнуляются, текущий адрес — на первый
        /// уровень. Первый этап первой страны разблокирован всегда (см. IsStageUnlocked),
        /// поэтому отдельно "оставлять первый уровень открытым" не требуется.
        /// </summary>
        public void ResetAllProgress()
        {
            for (var c = 0; c < _worldMapConfig.CountryCount; c++)
            for (var s = 0; s < TotalStageCount; s++)
            for (var l = 0; l < 3; l++)
                PlayerPrefs.DeleteKey(string.Format(StarsKey, c, s, l));

            PlayerPrefs.DeleteKey($"{CurrentKey}_c");
            PlayerPrefs.DeleteKey($"{CurrentKey}_s");
            PlayerPrefs.DeleteKey($"{CurrentKey}_l");
            ClearPendingCountryReward();

            _currentAddress.Value = new LevelAddress(0, 0, 0);
            PlayerPrefs.Save();

            Debug.LogWarning("[ProgressService] Прогресс сброшен — все уровни закрыты, кроме первого");
        }

        // ── Ожидающая награда за страну ───────────────────────────────────────────

        /// <summary>
        /// Индекс страны для отображения попапа завершения на StageMap. -1 = нет.
        /// </summary>
        public int GetPendingCountryReward() =>
            PlayerPrefs.GetInt(PendingCountryKey, -1);

        public void SetPendingCountryReward(int countryIndex)
        {
            PlayerPrefs.SetInt(PendingCountryKey, countryIndex);
            PlayerPrefs.Save();
        }

        public void ClearPendingCountryReward()
        {
            PlayerPrefs.DeleteKey(PendingCountryKey);
            PlayerPrefs.Save();
        }

        // ── Текущий адрес ────────────────────────────────────────────────────

        public void SetCurrentAddress(LevelAddress address)
        {
            _currentAddress.Value = address;
            PlayerPrefs.SetInt($"{CurrentKey}_c", address.CountryIndex);
            PlayerPrefs.SetInt($"{CurrentKey}_s", address.StageIndex);
            PlayerPrefs.SetInt($"{CurrentKey}_l", address.LevelIndex);
            PlayerPrefs.Save();
        }

        private static LevelAddress LoadCurrentAddress() => new(
            PlayerPrefs.GetInt($"{CurrentKey}_c", 0),
            PlayerPrefs.GetInt($"{CurrentKey}_s", 0),
            PlayerPrefs.GetInt($"{CurrentKey}_l", 0));

        // ── Следующий уровень ────────────────────────────────────────────────

        public LevelAddress? GetNextLevel(LevelAddress current)
        {
            // Следующий уровень в том же этапе
            if (current.LevelIndex < 2)
                return new LevelAddress(current.CountryIndex, current.StageIndex, current.LevelIndex + 1);

            // Следующий этап в той же стране (включая бонусный)
            if (current.StageIndex < BonusStageIndex)
                return new LevelAddress(current.CountryIndex, current.StageIndex + 1, 0);

            // Следующая страна (после бонусного этапа)
            if (current.CountryIndex < _worldMapConfig.CountryCount - 1)
                return new LevelAddress(current.CountryIndex + 1, 0, 0);

            return null;
        }

        public void Dispose() => _currentAddress.Dispose();
    }
}
