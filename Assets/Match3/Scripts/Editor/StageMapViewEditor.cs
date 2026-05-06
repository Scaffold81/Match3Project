#nullable enable

#if UNITY_EDITOR

using Match3.Views;
using UnityEditor;
using UnityEngine;

namespace Match3.Editor
{
    [CustomEditor(typeof(StageMapView))]
    public sealed class StageMapViewEditor : UnityEditor.Editor
    {
        private const int CountryCount     = 5;
        private const int StagesPerCountry = 9;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("── Инструменты карты ──", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            var view = (StageMapView)target;

            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("▶  Place Nodes", GUILayout.Height(36f)))
                PlaceNodes(view);

            GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f);
            if (GUILayout.Button("✕  Clear Nodes", GUILayout.Height(28f)))
                ClearNodes(view);

            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Place Nodes удалит старые ноды и создаст новые.\n" +
                "CountryNode + 9 StageNode на каждую из 5 стран = 50 объектов.",
                MessageType.Info);
        }

        // ── Размещение ────────────────────────────────────────────────────────

        private static void PlaceNodes(StageMapView view)
        {
            // camelCase — поля публичные, не свойства
            if (view.stagePrefab == null || view.countryPrefab == null)
            {
                Debug.LogWarning("[StageMapViewEditor] Назначь countryPrefab и stagePrefab в инспекторе");
                return;
            }

            ClearNodes(view);

            var content  = view.Content;
            var spacingY = view.itemSpacingY;
            var zigzagX  = view.zigzagOffsetX;
            var rowIndex = 0;

            Undo.SetCurrentGroupName("Place Stage Map Nodes");
            var undoGroup = Undo.GetCurrentGroup();

            for (var c = 0; c < CountryCount; c++)
            {
                // CountryNode
                var countryGo = (GameObject)PrefabUtility.InstantiatePrefab(
                    view.countryPrefab.gameObject, content);
                Undo.RegisterCreatedObjectUndo(countryGo, "Create CountryNode");
                countryGo.name = $"Country_{c}";
                countryGo.GetComponent<CountryNodeView>().countryIndex = c;
                SetPosition(countryGo, rowIndex, spacingY, zigzagX);
                EditorUtility.SetDirty(countryGo);
                rowIndex++;

                // StageNodes
                for (var s = 0; s < StagesPerCountry; s++)
                {
                    var stageGo = (GameObject)PrefabUtility.InstantiatePrefab(
                        view.stagePrefab.gameObject, content);
                    Undo.RegisterCreatedObjectUndo(stageGo, "Create StageNode");
                    stageGo.name = $"Country_{c}_Stage_{s}";

                    var node = stageGo.GetComponent<StageNodeView>();
                    node.countryIndex = c;
                    node.stageIndex   = s;

                    SetPosition(stageGo, rowIndex, spacingY, zigzagX);
                    EditorUtility.SetDirty(stageGo);
                    rowIndex++;
                }
            }

            ResizeContent(content, rowIndex, spacingY);
            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(view.gameObject);

            Debug.LogWarning($"[StageMapViewEditor] ✅ Размещено {rowIndex} объектов");
        }

        // ── Очистка ───────────────────────────────────────────────────────────

        private static void ClearNodes(StageMapView view)
        {
            var content = view.Content;
            if (content == null) return;

            Undo.SetCurrentGroupName("Clear Stage Map Nodes");
            var undoGroup = Undo.GetCurrentGroup();

            for (var i = content.childCount - 1; i >= 0; i--)
            {
                var child = content.GetChild(i).gameObject;
                if (child.GetComponent<StageNodeView>() != null ||
                    child.GetComponent<CountryNodeView>() != null)
                    Undo.DestroyObjectImmediate(child);
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(view.gameObject);
        }

        // ── Хелперы ───────────────────────────────────────────────────────────

        private static void SetPosition(
            GameObject go, int rowIndex, float spacingY, float zigzagOffsetX)
        {
            var rt       = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0.5f);

            var offsetX = (rowIndex % 2 == 0) ? -zigzagOffsetX * 0.5f : zigzagOffsetX * 0.5f;
            rt.anchoredPosition = new Vector2(offsetX, spacingY * rowIndex + spacingY * 0.5f);
        }

        private static void ResizeContent(RectTransform content, int rowCount, float spacingY)
        {
            var sd    = content.sizeDelta;
            sd.y      = spacingY * rowCount + spacingY;
            content.sizeDelta = sd;
            EditorUtility.SetDirty(content.gameObject);
        }
    }
}

#endif
