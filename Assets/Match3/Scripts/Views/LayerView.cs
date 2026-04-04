#nullable enable

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Match3.Views
{
    public sealed class LayerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _layerContainer = null!;
        [SerializeField] private GameObject    _layerCellPrefab = null!;

        private readonly Dictionary<Vector2Int, GameObject> _layerCells = new();

        public void SpawnLayerCell(Vector2Int cell, Vector2 anchoredPosition, float cellSize)
        {
            var go = Instantiate(_layerCellPrefab, _layerContainer);
            var rt = go.GetComponent<RectTransform>();

            rt.pivot            = new Vector2(0f, 1f);
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta        = new Vector2(cellSize, cellSize);

            _layerCells[cell] = go;
        }

        public void ClearLayerCell(Vector2Int cell)
        {
            if (!_layerCells.TryGetValue(cell, out var go)) return;
            _layerCells.Remove(cell);

            go.GetComponent<RectTransform>()
                .DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .SetLink(go)
                .OnComplete(() => Destroy(go));
        }

        public void ClearAll()
        {
            foreach (var go in _layerCells.Values)
                if (go != null) Destroy(go);
            _layerCells.Clear();
        }
    }
}
