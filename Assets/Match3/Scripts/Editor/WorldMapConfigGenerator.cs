#nullable enable

#if UNITY_EDITOR

using System.IO;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using UnityEditor;
using UnityEngine;

namespace Match3.Editor
{
    /// <summary>
    /// Генератор всех конфигов карты мира.
    /// Меню: Match3 → Generate World Map Configs
    ///
    /// Использует SerializedObject/SerializedProperty для обхода private set.
    /// Backing field для [field: SerializeField] = "&lt;PropName&gt;k__BackingField"
    /// </summary>
    public static class WorldMapConfigGenerator
    {
        private const string RootPath = "Assets/Match3/Configs";

        private static readonly string[] CountryNames =
            { "Egypt", "Greece", "China", "Maya", "India" };

        private static readonly Color[] CountrySectionColors =
        {
            new(0.94f, 0.87f, 0.60f), // Египет  — песок
            new(0.72f, 0.83f, 0.96f), // Греция  — голубой
            new(0.96f, 0.78f, 0.60f), // Китай   — оранжевый
            new(0.64f, 0.87f, 0.68f), // Майя    — зелёный
            new(0.86f, 0.72f, 0.94f), // Индия   — фиолетовый
        };

        [MenuItem("Match3/Generate World Map Configs")]
        public static void Generate()
        {
            EnsureDir($"{RootPath}/WorldMap");

            var worldMap   = CreateOrLoad<WorldMapConfig>($"{RootPath}/WorldMap/WorldMapConfig.asset");
            var worldMapSO = new SerializedObject(worldMap);
            var countriesProp = worldMapSO.FindProperty("<Countries>k__BackingField");
            countriesProp.arraySize = 5;

            for (var c = 0; c < 5; c++)
            {
                var country = GenerateCountry(c);
                countriesProp.GetArrayElementAtIndex(c).objectReferenceValue = country;
            }

            worldMapSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(worldMap);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Generator] ✅ Готово: 5 стран, 45 этапов, 135 уровней");
        }

        // ── Страна ───────────────────────────────────────────────────────────

        private static CountryConfig GenerateCountry(int c)
        {
            var name    = CountryNames[c];
            var dir     = $"{RootPath}/WorldMap/Country{c}_{name}";
            EnsureDir(dir);

            var country   = CreateOrLoad<CountryConfig>($"{dir}/{name}.asset");
            var so        = new SerializedObject(country);

            so.FindProperty("<CountryName>k__BackingField").stringValue  = name;
            so.FindProperty("<SectionColor>k__BackingField").colorValue  = CountrySectionColors[c];

            var stagesProp = so.FindProperty("<Stages>k__BackingField");
            stagesProp.arraySize = 9;

            for (var s = 0; s < 9; s++)
            {
                var stage = GenerateStage(c, s, dir);
                stagesProp.GetArrayElementAtIndex(s).objectReferenceValue = stage;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(country);
            return country;
        }

        // ── Этап ─────────────────────────────────────────────────────────────

        private static StageConfig GenerateStage(int c, int s, string countryDir)
        {
            var dir   = $"{countryDir}/Stage{s + 1:D2}";
            EnsureDir(dir);

            var stage = CreateOrLoad<StageConfig>($"{dir}/Stage{s + 1:D2}.asset");
            var so    = new SerializedObject(stage);

            so.FindProperty("<StageName>k__BackingField").stringValue = $"Stage {s + 1}";

            var levelsProp = so.FindProperty("<Levels>k__BackingField");
            levelsProp.arraySize = 3;

            for (var l = 0; l < 3; l++)
            {
                var level = GenerateLevel(c, s, l, dir);
                levelsProp.GetArrayElementAtIndex(l).objectReferenceValue = level;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stage);
            return stage;
        }

        // ── Уровень ──────────────────────────────────────────────────────────

        private static LevelConfig GenerateLevel(int c, int s, int l, string stageDir)
        {
            var level = CreateOrLoad<LevelConfig>($"{stageDir}/Level{l + 1}.asset");
            var so    = new SerializedObject(level);

            // Лимит ходов
            var progress  = c * 9 + s;
            var baseMoves = Mathf.RoundToInt(Mathf.Lerp(32f, 18f, progress / 44f));
            so.FindProperty("<MoveLimit>k__BackingField").intValue =
                Mathf.Max(10, baseMoves - l * 3);

            // Разрешённые цвета
            SetAllowedTypes(so, c);

            // Задачи
            SetObjectives(so, c, s, l);

            // Сетка
            SetGrid(so, c, s, l);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(level);
            return level;
        }

        // ── Allowed types ─────────────────────────────────────────────────────

        private static void SetAllowedTypes(SerializedObject so, int c)
        {
            var types = c switch
            {
                0 => new[] { NodeType.Red, NodeType.Blue, NodeType.Green, NodeType.Yellow },
                1 => new[] { NodeType.Red, NodeType.Blue, NodeType.Green, NodeType.Yellow, NodeType.Purple },
                _ => new[] { NodeType.Red, NodeType.Blue, NodeType.Green, NodeType.Yellow, NodeType.Purple, NodeType.Orange }
            };

            var prop = so.FindProperty("<AllowedNodeTypes>k__BackingField");
            prop.arraySize = types.Length;
            for (var i = 0; i < types.Length; i++)
                prop.GetArrayElementAtIndex(i).enumValueIndex = (int)types[i];
        }

        // ── Objectives ────────────────────────────────────────────────────────

        private static void SetObjectives(SerializedObject so, int c, int s, int l)
        {
            var progress = c * 9 + s;
            var base1    = Mathf.Max(5, Mathf.RoundToInt(Mathf.Lerp(15f, 50f, progress / 44f)) + l * 8);
            var base2    = Mathf.Max(5, Mathf.RoundToInt(base1 * 0.6f));

            var (objectives, count) = c switch
            {
                0 => (new[] { (NodeType.Red, base1) }, 1),
                1 => (new[] { (NodeType.Red, base1), (NodeType.Blue, base2) }, 2),
                2 => (new[] { (NodeType.Green, base1), (NodeType.Yellow, base1) }, 2),
                3 => (new[] { (NodeType.Red, base1), (NodeType.Blue, base2), (NodeType.Purple, base2) }, 3),
                _ => (new[] { (NodeType.Red, base1), (NodeType.Green, base1), (NodeType.Orange, base2) }, 3)
            };

            var prop = so.FindProperty("<Objectives>k__BackingField");
            prop.arraySize = count;

            for (var i = 0; i < count; i++)
            {
                var elem = prop.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("nodeType").enumValueIndex = (int)objectives[i].Item1;
                elem.FindPropertyRelative("count").intValue          = objectives[i].Item2;
            }
        }

        // ── Grid ──────────────────────────────────────────────────────────────

        private static void SetGrid(SerializedObject so, int c, int s, int l)
        {
            var size = GetGridSize(c, s);
            var prop = so.FindProperty("<Grid>k__BackingField");
            prop.arraySize = size;

            for (var row = 0; row < size; row++)
            {
                var rowProp   = prop.GetArrayElementAtIndex(row);
                var cellsProp = rowProp.FindPropertyRelative("<Cells>k__BackingField");
                cellsProp.arraySize = size;

                for (var col = 0; col < size; col++)
                {
                    var cell     = cellsProp.GetArrayElementAtIndex(col);
                    var cellType = GetCellType(c, s, row, col, size);
                    var hasLayer = GetHasLayer(c, s, l, row, col);

                    cell.FindPropertyRelative("cellType").enumValueIndex = (int)cellType;
                    cell.FindPropertyRelative("nodeType").enumValueIndex = (int)NodeType.None;
                    cell.FindPropertyRelative("hasLayer").boolValue      = hasLayer;
                }
            }
        }

        // ── Параметры ────────────────────────────────────────────────────────

        private static int GetGridSize(int c, int s) => c switch
        {
            0 => s < 5 ? 5 : 7,
            1 => 7,
            2 => s < 5 ? 7 : 9,
            3 => 9,
            _ => 9
        };

        private static CellType GetCellType(int c, int s, int row, int col, int size)
        {
            if (c >= 3 && s >= 4 && size == 9)
            {
                if ((row < 2 && col < 2) ||
                    (row < 2 && col >= size - 2) ||
                    (row >= size - 2 && col < 2) ||
                    (row >= size - 2 && col >= size - 2))
                    return CellType.Hidden;
            }

            if (c == 2 && s >= 7 && size == 9)
            {
                if ((row == 0 && col == 0) ||
                    (row == 0 && col == size - 1) ||
                    (row == size - 1 && col == 0) ||
                    (row == size - 1 && col == size - 1))
                    return CellType.Hidden;
            }

            return CellType.Normal;
        }

        private static bool GetHasLayer(int c, int s, int l, int row, int col)
        {
            if (c == 0) return false;

            var density = c switch
            {
                1 => 0.12f + s * 0.02f,
                2 => 0.18f + s * 0.03f,
                3 => 0.25f + s * 0.04f + l * 0.04f,
                _ => 0.35f + s * 0.04f + l * 0.04f
            };

            var hash = (row * 31 + col * 17 + s * 7 + l * 3 + c * 13) % 100;
            return hash < Mathf.RoundToInt(density * 100f);
        }

        // ── Утилиты ──────────────────────────────────────────────────────────

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}

#endif
