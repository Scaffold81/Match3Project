#nullable enable

#if UNITY_EDITOR

using Match3.Views;
using UnityEditor;
using UnityEngine;

namespace Match3.Editor
{
    /// <summary>
    /// Кастомный инспектор для LevelSelectPopupView.
    /// Позволяет быстро переназначить LevelButtonEntry[] через SerializedObject.
    /// </summary>
    [CustomEditor(typeof(LevelSelectPopupView))]
    public sealed class StageMapUISetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);

            if (GUILayout.Button("Rebind Level Entries from children"))
                RebindLevelEntries((LevelSelectPopupView)target);
        }

        /// <summary>
        /// Автоматически находит LevelButton_0/1/2 в дочерних объектах
        /// и перепривязывает _levelEntries через SerializedObject.
        /// </summary>
        private static void RebindLevelEntries(LevelSelectPopupView view)
        {
            var so         = new SerializedObject(view);
            var entriesProp = so.FindProperty("_levelEntries");

            if (entriesProp == null)
            {
                Debug.LogWarning("[StageMapUISetupEditor] _levelEntries not found");
                return;
            }

            entriesProp.arraySize = 3;

            for (var i = 0; i < 3; i++)
            {
                var btnGo = view.transform.Find($"PopupPanel/LevelButton_{i}");
                if (btnGo == null)
                {
                    Debug.LogWarning($"[StageMapUISetupEditor] LevelButton_{i} not found");
                    continue;
                }

                var ep = entriesProp.GetArrayElementAtIndex(i);

                ep.FindPropertyRelative("Button")
                  .objectReferenceValue = btnGo.GetComponent<UnityEngine.UI.Button>();

                ep.FindPropertyRelative("LevelLabel")
                  .objectReferenceValue = btnGo.Find("Label")
                                               ?.GetComponent<TMPro.TextMeshProUGUI>();

                ep.FindPropertyRelative("LockOverlay")
                  .objectReferenceValue = btnGo.Find("Lock")?.gameObject;

                var starsTransform = btnGo.Find("Stars");
                var sp = ep.FindPropertyRelative("Stars");

                if (starsTransform != null)
                {
                    sp.arraySize = 3;
                    for (var s = 0; s < 3; s++)
                    {
                        var starGo = starsTransform.Find($"Star_{s}");
                        sp.GetArrayElementAtIndex(s).objectReferenceValue =
                            starGo?.GetComponent<UnityEngine.UI.Image>();
                    }
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(view);

            Debug.LogWarning("[StageMapUISetupEditor] ✅ LevelEntries перепривязаны");
        }
    }
}

#endif
