#nullable enable

#if UNITY_EDITOR

using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using UnityEditor;
using UnityEngine;

namespace Match3.Editor
{
    /// <summary>
    /// Match3 → Patch Level Rewards
    ///
    /// Находит все LevelConfig ассеты в проекте и проставляет
    /// сбалансированные награды по формуле сложности.
    ///
    /// Таблица наград:
    ///   diff = countryIndex * 9 + stageIndex  (0..44)
    ///   base_coins = 50 + diff * 12           (50..578)
    ///
    ///   L1: coins(base)      + Hint×1
    ///   L2: coins(base+30)   + main_boost×1
    ///   L3: coins(base+70)   + main_boost×1 + sec_boost×1
    ///
    ///   diff  0-5  : main=Hint,     sec=HorizontalArrow
    ///   diff  6-11 : main=HArrow,   sec=VerticalArrow
    ///   diff 12-17 : main=VArrow,   sec=Bomb
    ///   diff 18-23 : main=Shuffle,  sec=Bomb
    ///   diff 24-29 : main=Bomb,     sec=ColorBomb
    ///   diff 30-35 : main=ColorBomb,sec=MegaBomb
    ///   diff 36+   : main=MegaBomb, sec=MegaBomb
    /// </summary>
    public static class RewardPatcher
    {
        [MenuItem("Match3/Patch Level Rewards")]
        public static void PatchAll()
        {
            var guids = AssetDatabase.FindAssets(
                "t:LevelConfig",
                new[] { "Assets/Match3/Configs/WorldMap" });

            if (guids.Length == 0)
            {
                Debug.LogWarning("[RewardPatcher] LevelConfig ассеты не найдены. " +
                                 "Проверь путь Assets/Match3/Configs/WorldMap");
                return;
            }

            var patched = 0;
            var skipped = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var guid in guids)
                {
                    var path   = AssetDatabase.GUIDToAssetPath(guid);
                    var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                    if (config == null) { skipped++; continue; }

                    // Определяем адрес из пути:
                    // .../CountryN_Name/StageNN/LevelN.asset
                    if (!TryParseAddress(path, out var countryIdx, out var stageIdx, out var levelIdx))
                    {
                        Debug.LogWarning($"[RewardPatcher] Не удалось распарсить адрес: {path}");
                        skipped++;
                        continue;
                    }

                    var rewards = BuildRewards(countryIdx, stageIdx, levelIdx);

                    var so = new SerializedObject(config);
                    var rewardsProp = so.FindProperty("<Rewards>k__BackingField");
                    if (rewardsProp == null)
                    {
                        Debug.LogWarning($"[RewardPatcher] Поле Rewards не найдено в {path}. " +
                                         "Убедись что LevelConfig скомпилирован с полем Rewards[].");
                        skipped++;
                        continue;
                    }

                    rewardsProp.arraySize = rewards.Length;
                    for (var i = 0; i < rewards.Length; i++)
                    {
                        var el = rewardsProp.GetArrayElementAtIndex(i);
                        el.FindPropertyRelative("<Type>k__BackingField")
                          .enumValueIndex = (int)rewards[i].Type;
                        el.FindPropertyRelative("<Boost>k__BackingField")
                          .enumValueIndex = (int)rewards[i].Boost;
                        el.FindPropertyRelative("<Amount>k__BackingField")
                          .intValue = rewards[i].Amount;
                    }

                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(config);
                    patched++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
            }

            Debug.LogWarning($"[RewardPatcher] ✅ Пропатчено: {patched} | Пропущено: {skipped} | Всего: {guids.Length}");
        }

        // ── Построение наград ─────────────────────────────────────────────────

        private static RewardData[] BuildRewards(int countryIdx, int stageIdx, int levelIdx)
        {
            var diff      = countryIdx * 9 + stageIdx;
            var baseCoins = 50 + diff * 12;

            GetBoosts(diff, out var mainBoost, out var secBoost);

            return levelIdx switch
            {
                0 => new[]  // L1 — монеты + Hint
                {
                    Coins(baseCoins),
                    Boost(BoostType.Hint, 1),
                },
                1 => new[]  // L2 — монеты + основной буст
                {
                    Coins(baseCoins + 30),
                    Boost(mainBoost, 1),
                },
                _ => new[]  // L3 — монеты + основной + вторичный
                {
                    Coins(baseCoins + 70),
                    Boost(mainBoost, 1),
                    Boost(secBoost,  1),
                },
            };
        }

        private static void GetBoosts(int diff, out BoostType main, out BoostType sec)
        {
            if (diff < 6)       { main = BoostType.Hint;         sec = BoostType.HorizontalArrow; }
            else if (diff < 12) { main = BoostType.HorizontalArrow; sec = BoostType.VerticalArrow; }
            else if (diff < 18) { main = BoostType.VerticalArrow;   sec = BoostType.Bomb;          }
            else if (diff < 24) { main = BoostType.Shuffle;          sec = BoostType.Bomb;          }
            else if (diff < 30) { main = BoostType.Bomb;             sec = BoostType.ColorBomb;     }
            else if (diff < 36) { main = BoostType.ColorBomb;        sec = BoostType.MegaBomb;      }
            else                { main = BoostType.MegaBomb;          sec = BoostType.MegaBomb;      }
        }

        // ── Хелперы ───────────────────────────────────────────────────────────

        private static RewardData Coins(int amount) =>
            new(RewardType.Coins, BoostType.None, amount);

        private static RewardData Boost(BoostType boost, int amount) =>
            new(RewardType.Boost, boost, amount);

        // ── Парсинг пути ──────────────────────────────────────────────────────

        /// <summary>
        /// Извлекает countryIdx, stageIdx, levelIdx из пути вида:
        /// .../Country{N}_Name/Stage{NN}/Level{N}.asset
        /// </summary>
        private static bool TryParseAddress(
            string path,
            out int countryIdx,
            out int stageIdx,
            out int levelIdx)
        {
            countryIdx = stageIdx = levelIdx = 0;

            // Нормализуем слеши
            path = path.Replace('\\', '/');

            // Level index: имя файла LevelN.asset → N-1
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith("Level") ||
                !int.TryParse(fileName.Substring(5), out var lNum))
                return false;
            levelIdx = lNum - 1;

            // Stage index: папка StageNN → NN-1
            var parts = path.Split('/');
            var stageDir = string.Empty;
            var countryDir = string.Empty;

            for (var i = parts.Length - 1; i >= 0; i--)
            {
                if (parts[i].StartsWith("Stage") && string.IsNullOrEmpty(stageDir))
                    stageDir = parts[i];
                else if (parts[i].StartsWith("Country") && string.IsNullOrEmpty(countryDir))
                    countryDir = parts[i];
            }

            if (string.IsNullOrEmpty(stageDir) || string.IsNullOrEmpty(countryDir))
                return false;

            // StageNN → stageIdx = NN - 1
            var stageNumStr = stageDir.Substring(5); // "Stage01" → "01"
            if (!int.TryParse(stageNumStr, out var sNum))
                return false;
            stageIdx = sNum - 1;

            // Country{N}_Name → countryIdx = N
            var underscoreIdx = countryDir.IndexOf('_');
            var countryNumStr = underscoreIdx > 0
                ? countryDir.Substring(7, underscoreIdx - 7)
                : countryDir.Substring(7);

            if (!int.TryParse(countryNumStr, out countryIdx))
                return false;

            return true;
        }
    }
}

#endif
