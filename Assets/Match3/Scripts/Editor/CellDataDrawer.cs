#nullable enable

using Match3.Core.Enums;
using Match3.Core.Models;
using UnityEditor;
using UnityEngine;

namespace Match3.Editor
{
    [CustomPropertyDrawer(typeof(CellData))]
    public sealed class CellDataDrawer : PropertyDrawer
    {
        private const float CellSize    = 48f;
        private const float LayerMargin = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => CellSize;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var cellTypeProp     = property.FindPropertyRelative(nameof(CellData.cellType));
            var nodeTypeProp     = property.FindPropertyRelative(nameof(CellData.nodeType));
            var obstacleTypeProp = property.FindPropertyRelative(nameof(CellData.obstacleType));

            var cellType     = (CellType)cellTypeProp.enumValueIndex;
            var nodeType     = (NodeType)nodeTypeProp.enumValueIndex;
            var obstacleType = (ObstacleType)obstacleTypeProp.enumValueIndex;
            var hasObstacle  = obstacleType != ObstacleType.None;

            // Фон
            var bgColor = cellType == CellType.Hidden
                ? new Color(0.15f, 0.15f, 0.15f)
                : GetNodeColor(nodeType);
            EditorGUI.DrawRect(position, bgColor);

            // Рамка — цвет по типу препятствия
            var borderColor = GetObstacleColor(obstacleType);
            DrawBorder(position, borderColor, hasObstacle ? 3f : 1f);

            if (cellType == CellType.Hidden)
            {
                EditorGUI.LabelField(position, "✕", new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 18,
                    normal    = { textColor = new Color(1f, 1f, 1f, 0.3f) }
                });
                return;
            }

            // Метка NodeType
            var shortName  = nodeType == NodeType.None ? "?" : nodeType.ToString()[..2];
            var labelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize  = 13,
                fontStyle = FontStyle.Bold,
                normal    = { textColor = Color.white }
            };
            var shadow = position;
            shadow.x += 1; shadow.y += 1;
            EditorGUI.LabelField(shadow, shortName, new GUIStyle(labelStyle)
                { normal = { textColor = new Color(0f, 0f, 0f, 0.5f) } });
            EditorGUI.LabelField(position, shortName, labelStyle);

            // Иконка препятствия (угловой квадрат с первой буквой типа)
            if (hasObstacle)
            {
                var iconRect = new Rect(
                    position.xMax - LayerMargin - 12f,
                    position.y    + LayerMargin,
                    12f, 12f);
                EditorGUI.DrawRect(iconRect, GetObstacleColor(obstacleType));

                var letter = obstacleType.ToString()[..1];
                EditorGUI.LabelField(iconRect, letter, new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 8,
                    fontStyle = FontStyle.Bold,
                    normal    = { textColor = Color.white }
                });
            }
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.x,              rect.y,              rect.width,  thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x,              rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.x,              rect.y,              thickness,   rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y,            thickness,   rect.height), color);
        }

        public static Color GetNodeColor(NodeType nodeType) => nodeType switch
        {
            NodeType.Red    => new Color(0.85f, 0.22f, 0.22f),
            NodeType.Blue   => new Color(0.22f, 0.45f, 0.85f),
            NodeType.Green  => new Color(0.22f, 0.72f, 0.32f),
            NodeType.Yellow => new Color(0.92f, 0.78f, 0.15f),
            NodeType.Purple => new Color(0.58f, 0.22f, 0.85f),
            NodeType.Orange => new Color(0.95f, 0.55f, 0.12f),
            _               => new Color(0.4f,  0.4f,  0.4f),
        };

        public static Color GetObstacleColor(ObstacleType type) => type switch
        {
            ObstacleType.Ice   => new Color(0.4f,  0.8f,  1.0f),
            ObstacleType.Box   => new Color(0.7f,  0.5f,  0.2f),
            ObstacleType.Chain => new Color(0.55f, 0.55f, 0.55f),
            ObstacleType.Rock  => new Color(0.45f, 0.35f, 0.25f),
            _                  => new Color(0f,    0f,    0f, 0.4f),
        };
    }
}
