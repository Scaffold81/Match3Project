#nullable enable

using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using UnityEditor;
using UnityEngine;

namespace Match3.Editor
{
    public sealed class LevelEditorWindow : EditorWindow
    {
        // ── Константы ────────────────────────────────────────────────────
        private const float CellSize      = 52f;
        private const float CellSpacing   = 2f;
        private const float SidebarWidth  = 220f;

        // ── Состояние ────────────────────────────────────────────────────
        private LevelConfig? _config;
        private SerializedObject? _so;

        private NodeType     _paintNodeType     = NodeType.Red;
        private CellType     _paintCellType     = CellType.Normal;
        private ObstacleType _paintObstacleType = ObstacleType.Ice;
        private int          _paintObstacleHp   = 1;
        private PaintMode    _paintMode         = PaintMode.Node;

        private Vector2 _gridScroll;
        private Vector2 _sidebarScroll;

        private int _newRows    = 7;
        private int _newColumns = 7;

        private enum PaintMode { Node, CellType, Obstacle }

        // ── Открытие окна ────────────────────────────────────────────────
        [MenuItem("Match3/Level Editor")]
        public static void Open() =>
            GetWindow<LevelEditorWindow>("Level Editor").minSize = new Vector2(700f, 500f);

        [MenuItem("Assets/Edit Level Config", true)]
        private static bool CanOpenFromAsset() => Selection.activeObject is LevelConfig;

        [MenuItem("Assets/Edit Level Config")]
        private static void OpenFromAsset()
        {
            var window = GetWindow<LevelEditorWindow>("Level Editor");
            window.LoadConfig((LevelConfig)Selection.activeObject);
        }

        // ── Unity callbacks ──────────────────────────────────────────────
        private void OnGUI()
        {
            DrawToolbar();

            if (_config == null || _so == null)
            {
                DrawNoConfigMessage();
                return;
            }

            _so.Update();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawGrid();
                DrawSidebar();
            }

            _so.ApplyModifiedProperties();
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is LevelConfig config)
                LoadConfig(config);
        }

        // ── Toolbar ──────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var newConfig = (LevelConfig?)EditorGUILayout.ObjectField(
                    _config, typeof(LevelConfig), false, GUILayout.Width(200f));

                if (newConfig != _config && newConfig != null)
                    LoadConfig(newConfig);

                GUILayout.Space(8f);

                if (GUILayout.Button("New Config", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    CreateNewConfig();

                GUILayout.FlexibleSpace();

                if (_config != null)
                {
                    EditorGUILayout.LabelField(
                        $"{_config.Rows}x{_config.Columns}",
                        new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            normal    = { textColor = new Color(0.8f, 0.8f, 0.8f) }
                        },
                        GUILayout.Width(60f));
                }
            }
        }

        // ── Сетка ────────────────────────────────────────────────────────
        private void DrawGrid()
        {
            if (_config == null || _so == null) return;

            var gridProp = _so.FindProperty("<Grid>k__BackingField");
            if (gridProp == null) return;

            var totalW = _config.Columns * (CellSize + CellSpacing);
            var totalH = _config.Rows    * (CellSize + CellSpacing);

            using var scroll = new EditorGUILayout.ScrollViewScope(
                _gridScroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            _gridScroll = scroll.scrollPosition;

            var startRect = GUILayoutUtility.GetRect(totalW, totalH);

            for (var row = 0; row < _config.Rows; row++)
            {
                var rowProp   = gridProp.GetArrayElementAtIndex(row);
                var cellsProp = rowProp.FindPropertyRelative("<Cells>k__BackingField");
                if (cellsProp == null) continue;

                for (var col = 0; col < _config.Columns; col++)
                {
                    if (col >= cellsProp.arraySize) continue;

                    var cellProp         = cellsProp.GetArrayElementAtIndex(col);
                    var cellTypeProp     = cellProp.FindPropertyRelative(nameof(CellData.cellType));
                    var nodeTypeProp     = cellProp.FindPropertyRelative(nameof(CellData.nodeType));
                    var obstacleTypeProp = cellProp.FindPropertyRelative(nameof(CellData.obstacleType));
                    var obstacleHpProp   = cellProp.FindPropertyRelative(nameof(CellData.obstacleHp));

                    var cellRect = new Rect(
                        startRect.x + col * (CellSize + CellSpacing),
                        startRect.y + row * (CellSize + CellSpacing),
                        CellSize, CellSize);

                    DrawCell(cellRect, cellTypeProp, nodeTypeProp, obstacleTypeProp, obstacleHpProp);
                }
            }
        }

        private void DrawCell(
            Rect rect,
            SerializedProperty cellTypeProp,
            SerializedProperty nodeTypeProp,
            SerializedProperty obstacleTypeProp,
            SerializedProperty obstacleHpProp)
        {
            var cellType     = (CellType)cellTypeProp.enumValueIndex;
            var nodeType     = (NodeType)nodeTypeProp.enumValueIndex;
            var obstacleType = (ObstacleType)obstacleTypeProp.enumValueIndex;
            var hasObstacle  = obstacleType != ObstacleType.None;

            // Фон
            var bgColor = cellType == CellType.Hidden
                ? new Color(0.15f, 0.15f, 0.15f)
                : CellDataDrawer.GetNodeColor(nodeType);
            EditorGUI.DrawRect(rect, bgColor);

            // Рамка
            var borderColor = CellDataDrawer.GetObstacleColor(obstacleType);
            DrawBorder(rect, borderColor, hasObstacle ? 3f : 1f);

            // Текст ячейки
            if (cellType == CellType.Hidden)
            {
                EditorGUI.LabelField(rect, "X", new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 20,
                    normal    = { textColor = new Color(1f, 1f, 1f, 0.25f) }
                });
            }
            else
            {
                var label = nodeType == NodeType.None ? "?" : nodeType.ToString()[..2];
                EditorGUI.LabelField(rect, label, new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 15,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = Color.white }
                });
            }

            // Иконка препятствия + HP
            if (hasObstacle)
            {
                var iconRect = new Rect(rect.xMax - 14f, rect.y + 2f, 12f, 12f);
                EditorGUI.DrawRect(iconRect, CellDataDrawer.GetObstacleColor(obstacleType));

                var hp     = obstacleHpProp.intValue;
                var letter = hp > 0 ? hp.ToString() : obstacleType.ToString()[..1];
                EditorGUI.LabelField(iconRect, letter, new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 8,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = Color.white }
                });
            }

            // Ввод
            var e = Event.current;
            if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                && rect.Contains(e.mousePosition))
            {
                ApplyPaint(cellTypeProp, nodeTypeProp, obstacleTypeProp, obstacleHpProp, e.button);
                e.Use();
                GUI.changed = true;
            }
        }

        private void ApplyPaint(
            SerializedProperty cellTypeProp,
            SerializedProperty nodeTypeProp,
            SerializedProperty obstacleTypeProp,
            SerializedProperty obstacleHpProp,
            int mouseButton)
        {
            switch (_paintMode)
            {
                case PaintMode.Node:
                    if (mouseButton == 0)
                    {
                        cellTypeProp.enumValueIndex = (int)CellType.Normal;
                        nodeTypeProp.enumValueIndex = (int)_paintNodeType;
                    }
                    else
                    {
                        cellTypeProp.enumValueIndex = (int)CellType.Normal;
                        nodeTypeProp.enumValueIndex = (int)NodeType.None;
                    }
                    break;

                case PaintMode.CellType:
                    cellTypeProp.enumValueIndex = mouseButton == 0
                        ? (int)_paintCellType
                        : (int)CellType.Normal;
                    break;

                case PaintMode.Obstacle:
                    if (mouseButton == 0)
                    {
                        obstacleTypeProp.enumValueIndex = (int)_paintObstacleType;
                        obstacleHpProp.intValue         = _paintObstacleHp;
                    }
                    else
                    {
                        obstacleTypeProp.enumValueIndex = (int)ObstacleType.None;
                        obstacleHpProp.intValue         = 0;
                    }
                    break;
            }
        }

        // ── Sidebar ──────────────────────────────────────────────────────
        private void DrawSidebar()
        {
            if (_config == null || _so == null) return;

            using var scroll = new EditorGUILayout.ScrollViewScope(
                _sidebarScroll,
                GUILayout.Width(SidebarWidth),
                GUILayout.ExpandHeight(true));
            _sidebarScroll = scroll.scrollPosition;

            EditorGUILayout.LabelField("Paint Mode", EditorStyles.boldLabel);
            _paintMode = (PaintMode)GUILayout.SelectionGrid(
                (int)_paintMode,
                new[] { "Node", "Cell Type", "Obstacle" },
                3);

            EditorGUILayout.Space(8f);

            switch (_paintMode)
            {
                case PaintMode.Node:     DrawNodePalette();     break;
                case PaintMode.CellType: DrawCellTypePalette(); break;
                case PaintMode.Obstacle: DrawObstaclePalette(); break;
            }

            EditorGUILayout.Space(12f);
            DrawDivider();

            // Размер сетки
            EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
            _newRows    = Mathf.Clamp(EditorGUILayout.IntField("Rows",    _newRows),    1, 20);
            _newColumns = Mathf.Clamp(EditorGUILayout.IntField("Columns", _newColumns), 1, 20);

            if (GUILayout.Button("Resize Grid"))
                ResizeGrid(_newRows, _newColumns);

            EditorGUILayout.Space(12f);
            DrawDivider();

            // Настройки уровня
            EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);

            var moveLimitProp = _so.FindProperty("<MoveLimit>k__BackingField");
            if (moveLimitProp != null)
                EditorGUILayout.PropertyField(moveLimitProp, new GUIContent("Move Limit"));

            EditorGUILayout.Space(4f);

            var allowedProp = _so.FindProperty("<AllowedNodeTypes>k__BackingField");
            if (allowedProp != null)
                EditorGUILayout.PropertyField(allowedProp, new GUIContent("Allowed Types"), true);

            EditorGUILayout.Space(4f);

            var objectivesProp = _so.FindProperty("<Objectives>k__BackingField");
            if (objectivesProp != null)
                EditorGUILayout.PropertyField(objectivesProp, new GUIContent("Objectives"), true);

            EditorGUILayout.Space(12f);
            DrawDivider();

            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            if (GUILayout.Button("Fill All — Random"))    FillAll(CellType.Normal, NodeType.None);
            if (GUILayout.Button("Clear All — Hidden"))   FillAll(CellType.Hidden, NodeType.None);
            if (GUILayout.Button("Clear All Obstacles"))  ClearAllObstacles();
        }

        private void DrawNodePalette()
        {
            EditorGUILayout.LabelField("Node Type", EditorStyles.miniLabel);

            var col = 0;
            EditorGUILayout.BeginHorizontal();

            foreach (NodeType nt in System.Enum.GetValues(typeof(NodeType)))
            {
                if (nt == NodeType.None) continue;

                var isSelected = _paintNodeType == nt;
                var color      = CellDataDrawer.GetNodeColor(nt);

                if (GUILayout.Button(nt.ToString()[..2], MakeColoredButton(color, isSelected),
                    GUILayout.Width(58f), GUILayout.Height(40f)))
                    _paintNodeType = nt;

                col++;
                if (col % 3 == 0)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("LMB = рисовать\nRMB = сброс (NodeType.None)", MessageType.None);
        }

        private void DrawCellTypePalette()
        {
            EditorGUILayout.LabelField("Cell Type", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (CellType ct in System.Enum.GetValues(typeof(CellType)))
                {
                    var isSelected = _paintCellType == ct;
                    var color      = ct == CellType.Hidden
                        ? new Color(0.15f, 0.15f, 0.15f)
                        : new Color(0.3f, 0.6f, 0.3f);

                    if (GUILayout.Button(ct.ToString(), MakeColoredButton(color, isSelected),
                        GUILayout.Height(40f)))
                        _paintCellType = ct;
                }
            }

            EditorGUILayout.HelpBox("LMB = рисовать\nRMB = сброс (Normal)", MessageType.None);
        }

        private void DrawObstaclePalette()
        {
            EditorGUILayout.LabelField("Obstacle Type", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (ObstacleType ot in System.Enum.GetValues(typeof(ObstacleType)))
                {
                    if (ot == ObstacleType.None) continue;

                    var isSelected = _paintObstacleType == ot;
                    var color      = CellDataDrawer.GetObstacleColor(ot);

                    if (GUILayout.Button(ot.ToString(), MakeColoredButton(color, isSelected),
                        GUILayout.Height(36f)))
                        _paintObstacleType = ot;
                }
            }

            EditorGUILayout.Space(6f);
            _paintObstacleHp = EditorGUILayout.IntSlider("HP", _paintObstacleHp, 1, 4);
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("LMB = поставить препятствие\nRMB = убрать препятствие", MessageType.None);
        }

        // ── Grid operations ──────────────────────────────────────────────
        private void ResizeGrid(int rows, int cols)
        {
            if (_so == null || _config == null) return;

            _so.Update();
            var gridProp = _so.FindProperty("<Grid>k__BackingField");
            if (gridProp == null) return;

            gridProp.arraySize = rows;

            for (var r = 0; r < rows; r++)
            {
                var rowProp   = gridProp.GetArrayElementAtIndex(r);
                var cellsProp = rowProp.FindPropertyRelative("<Cells>k__BackingField");
                if (cellsProp == null) continue;

                var oldCols = cellsProp.arraySize;
                cellsProp.arraySize = cols;

                for (var c = oldCols; c < cols; c++)
                {
                    var cell = cellsProp.GetArrayElementAtIndex(c);
                    cell.FindPropertyRelative(nameof(CellData.cellType))!    .enumValueIndex = (int)CellType.Normal;
                    cell.FindPropertyRelative(nameof(CellData.nodeType))!    .enumValueIndex = (int)NodeType.None;
                    cell.FindPropertyRelative(nameof(CellData.obstacleType))!.enumValueIndex = (int)ObstacleType.None;
                    cell.FindPropertyRelative(nameof(CellData.obstacleHp))!  .intValue       = 0;
                }
            }

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
        }

        private void FillAll(CellType cellType, NodeType nodeType)
        {
            if (_so == null || _config == null) return;

            _so.Update();
            var gridProp = _so.FindProperty("<Grid>k__BackingField");
            if (gridProp == null) return;

            for (var r = 0; r < gridProp.arraySize; r++)
            {
                var cellsProp = gridProp
                    .GetArrayElementAtIndex(r)
                    .FindPropertyRelative("<Cells>k__BackingField");
                if (cellsProp == null) continue;

                for (var c = 0; c < cellsProp.arraySize; c++)
                {
                    var cell = cellsProp.GetArrayElementAtIndex(c);
                    cell.FindPropertyRelative(nameof(CellData.cellType))!.enumValueIndex = (int)cellType;
                    cell.FindPropertyRelative(nameof(CellData.nodeType))!.enumValueIndex = (int)nodeType;
                }
            }

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
        }

        private void ClearAllObstacles()
        {
            if (_so == null || _config == null) return;

            _so.Update();
            var gridProp = _so.FindProperty("<Grid>k__BackingField");
            if (gridProp == null) return;

            for (var r = 0; r < gridProp.arraySize; r++)
            {
                var cellsProp = gridProp
                    .GetArrayElementAtIndex(r)
                    .FindPropertyRelative("<Cells>k__BackingField");
                if (cellsProp == null) continue;

                for (var c = 0; c < cellsProp.arraySize; c++)
                {
                    var cell = cellsProp.GetArrayElementAtIndex(c);
                    cell.FindPropertyRelative(nameof(CellData.obstacleType))!.enumValueIndex = (int)ObstacleType.None;
                    cell.FindPropertyRelative(nameof(CellData.obstacleHp))!  .intValue       = 0;
                }
            }

            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private void LoadConfig(LevelConfig config)
        {
            _config     = config;
            _so         = new SerializedObject(config);
            _newRows    = config.Rows    > 0 ? config.Rows    : 7;
            _newColumns = config.Columns > 0 ? config.Columns : 7;
            Repaint();
        }

        private void CreateNewConfig()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "New Level Config", "Level_01", "asset",
                "Choose location", "Assets/Match3/Configs/Levels");

            if (string.IsNullOrEmpty(path)) return;

            var config = CreateInstance<LevelConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();

            LoadConfig(config);
            ResizeGrid(_newRows, _newColumns);
        }

        private void DrawNoConfigMessage()
        {
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.LabelField("No Level Config selected",
                        new GUIStyle(EditorStyles.largeLabel) { alignment = TextAnchor.MiddleCenter });
                    EditorGUILayout.Space(8f);
                    if (GUILayout.Button("Create New Config", GUILayout.Width(180f), GUILayout.Height(36f)))
                        CreateNewConfig();
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.FlexibleSpace();
        }

        private static void DrawDivider()
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
            EditorGUILayout.Space(4f);
        }

        private static GUIStyle MakeColoredButton(Color color, bool selected)
        {
            var style = new GUIStyle(GUI.skin.button);
            var tex   = new Texture2D(1, 1);
            tex.SetPixel(0, 0, selected ? Color.Lerp(color, Color.white, 0.3f) : color);
            tex.Apply();
            style.normal.background = tex;
            style.hover.background  = tex;
            style.active.background = tex;
            style.normal.textColor  = Color.white;
            style.fontStyle         = selected ? FontStyle.Bold : FontStyle.Normal;
            style.border            = selected ? new RectOffset(3, 3, 3, 3) : new RectOffset(1, 1, 1, 1);
            return style;
        }

        private static void DrawBorder(Rect rect, Color color, float t)
        {
            EditorGUI.DrawRect(new Rect(rect.x,        rect.y,        rect.width, t),          color);
            EditorGUI.DrawRect(new Rect(rect.x,        rect.yMax - t, rect.width, t),          color);
            EditorGUI.DrawRect(new Rect(rect.x,        rect.y,        t,          rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - t, rect.y,        t,          rect.height), color);
        }
    }
}
