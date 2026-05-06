#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// View карты уровней.
    /// Ноды размещаются через булевый флаг _placeNodes прямо в Inspector.
    /// В рантайме только обновляет состояние уже размещённых нод.
    /// </summary>
    public sealed class StageMapView : MonoBehaviour
    {
        [Header("ScrollRect")]
        [SerializeField] private ScrollRect    _scrollRect = null!;
        [SerializeField] private RectTransform _content    = null!;

        [Header("Префабы")]
        [SerializeField] public CountryNodeView countryPrefab = null!;
        [SerializeField] public StageNodeView   stagePrefab   = null!;

        [Header("Параметры зигзага")]
        [SerializeField] public float zigzagOffsetX = 120f;
        [SerializeField] public float itemSpacingY  = 160f;

        [Header("— Поставь true чтобы расставить ноды —")]
        [SerializeField] private bool _placeNodes;
        [SerializeField] private bool _clearNodes;

        private const int CountryCount     = 5;
        private const int StagesPerCountry = 9;

        // ── OnValidate — срабатывает при изменении поля в Inspector ──────────
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_placeNodes)
            {
                _placeNodes = false;
                // Откладываем на следующий кадр — нельзя менять сцену внутри OnValidate
                UnityEditor.EditorApplication.delayCall += PlaceNodesInEditor;
            }

            if (_clearNodes)
            {
                _clearNodes = false;
                UnityEditor.EditorApplication.delayCall += ClearNodesInEditor;
            }
        }

        private void PlaceNodesInEditor()
        {
            if (this == null) return; // объект мог быть удалён

            if (countryPrefab == null || stagePrefab == null)
            {
                Debug.LogWarning("[StageMapView] Назначь countryPrefab и stagePrefab");
                return;
            }

            ClearNodesInEditor();

            var rowIndex = 0;

            UnityEditor.Undo.SetCurrentGroupName("Place Stage Map Nodes");
            var group = UnityEditor.Undo.GetCurrentGroup();

            for (var c = 0; c < CountryCount; c++)
            {
                // CountryNode
                var countryGo = (GameObject)UnityEditor.PrefabUtility
                    .InstantiatePrefab(countryPrefab.gameObject, _content);
                UnityEditor.Undo.RegisterCreatedObjectUndo(countryGo, "CountryNode");
                countryGo.name = $"Country_{c}";
                countryGo.GetComponent<CountryNodeView>().countryIndex = c;
                SetPosition(countryGo, rowIndex);
                rowIndex++;

                // StageNodes
                for (var s = 0; s < StagesPerCountry; s++)
                {
                    var stageGo = (GameObject)UnityEditor.PrefabUtility
                        .InstantiatePrefab(stagePrefab.gameObject, _content);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(stageGo, "StageNode");
                    stageGo.name = $"Country_{c}_Stage_{s}";
                    var node = stageGo.GetComponent<StageNodeView>();
                    node.countryIndex = c;
                    node.stageIndex   = s;
                    SetPosition(stageGo, rowIndex);
                    rowIndex++;
                }
            }

            // Подгоняем высоту Content
            var sd = _content.sizeDelta;
            sd.y = itemSpacingY * rowIndex + itemSpacingY;
            _content.sizeDelta = sd;

            UnityEditor.Undo.CollapseUndoOperations(group);
            UnityEditor.EditorUtility.SetDirty(gameObject);

            Debug.LogWarning($"[StageMapView] ✅ Размещено {rowIndex} объектов");
        }

        private void ClearNodesInEditor()
        {
            if (this == null || _content == null) return;

            UnityEditor.Undo.SetCurrentGroupName("Clear Stage Map Nodes");
            var group = UnityEditor.Undo.GetCurrentGroup();

            for (var i = _content.childCount - 1; i >= 0; i--)
            {
                var child = _content.GetChild(i).gameObject;
                if (child.GetComponent<StageNodeView>()   != null ||
                    child.GetComponent<CountryNodeView>() != null)
                    UnityEditor.Undo.DestroyObjectImmediate(child);
            }

            UnityEditor.Undo.CollapseUndoOperations(group);
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        private void SetPosition(GameObject go, int rowIndex)
        {
            var rt       = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            var offsetX  = (rowIndex % 2 == 0) ? -zigzagOffsetX * 0.5f : zigzagOffsetX * 0.5f;
            rt.anchoredPosition = new Vector2(offsetX, itemSpacingY * rowIndex + itemSpacingY * 0.5f);
        }
#endif

        // ── Runtime ──────────────────────────────────────────────────────────

        private readonly List<StageNodeView>   _stageNodes   = new();
        private readonly List<CountryNodeView> _countryNodes = new();

        public List<StageNodeView>   StageNodes   => _stageNodes;
        public List<CountryNodeView> CountryNodes => _countryNodes;
        public RectTransform         Content      => _content;

        public void RefreshStages(
            Func<int, int, int>  getStageStars,
            Func<int, int, bool> isStageUnlocked)
        {
            _stageNodes.Clear();

            var nodes = _content
                .GetComponentsInChildren<StageNodeView>(includeInactive: true)
                .OrderBy(n => n.countryIndex * 100 + n.stageIndex)
                .ToList();

            foreach (var node in nodes)
            {
                node.Refresh(
                    getStageStars(node.countryIndex, node.stageIndex),
                    isStageUnlocked(node.countryIndex, node.stageIndex));
                _stageNodes.Add(node);
            }
        }

        public void RefreshCountries(
            Func<int, Sprite> getIcon,
            Func<int, string> getName,
            Func<int, Color>  getColor,
            Func<int, bool>   isUnlocked)
        {
            _countryNodes.Clear();

            var nodes = _content
                .GetComponentsInChildren<CountryNodeView>(includeInactive: true)
                .OrderBy(n => n.countryIndex)
                .ToList();

            foreach (var node in nodes)
            {
                var c = node.countryIndex;
                node.Refresh(getIcon(c), getName(c), getColor(c), isUnlocked(c));
                _countryNodes.Add(node);
            }
        }

        public void ScrollToNode(StageNodeView node)
        {
            Canvas.ForceUpdateCanvases();

            var nodeY    = node.GetComponent<RectTransform>().anchoredPosition.y;
            var contentH = _content.sizeDelta.y;
            var viewH    = _scrollRect.viewport.rect.height;

            _scrollRect.verticalNormalizedPosition =
                Mathf.Clamp01((nodeY - viewH * 0.5f) / (contentH - viewH));
        }
    }
}
