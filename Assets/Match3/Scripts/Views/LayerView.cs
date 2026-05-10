#nullable enable

using System.Collections.Generic;
using DG.Tweening;
using Match3.Configs;
using Match3.Core.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Match3.Views
{
    /// <summary>
    /// Отвечает за отображение препятствий (Ice, Box, Chain, Rock) на доске.
    /// Создаёт GameObject с Image программно — отдельный префаб не нужен.
    /// Визуал берётся из GemConfig.Obstacles; если спрайт не задан — FallbackColor.
    /// </summary>
    public sealed class LayerView : MonoBehaviour
    {
        [SerializeField] private RectTransform _layerContainer = null!;

        private readonly Dictionary<Vector2Int, (GameObject go, Image img)> _cells = new();

        // ── Инициализация контейнера ──────────────────────────────────────

        /// <summary>
        /// Приводит LayerContainer к тем же настройкам что GemContainer.
        /// Вызывать из LayerPresenter.RenderLayers до спавна препятствий.
        /// </summary>
        public void AlignToContainer(RectTransform reference)
        {
            if (_layerContainer == null || reference == null) return;

            _layerContainer.anchorMin        = reference.anchorMin;
            _layerContainer.anchorMax        = reference.anchorMax;
            _layerContainer.pivot            = reference.pivot;
            _layerContainer.anchoredPosition = reference.anchoredPosition;
            _layerContainer.sizeDelta        = reference.sizeDelta;
        }

        // ── Спавн ────────────────────────────────────────────────────────────

        public void SpawnObstacleCell(
            Vector2Int         pos,
            int                hp,
            int                maxHp,
            ObstacleVisualData visual,
            Vector2            anchoredPosition,
            float              cellSize)
        {
            if (_layerContainer == null)
            {
                Debug.LogError("[LayerView] LayerContainer не назначен в Inspector");
                return;
            }

            // Создаём UI-объект без префаба
            var go  = new GameObject($"Obstacle_{pos.x}_{pos.y}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(_layerContainer, worldPositionStays: false);

            var rt  = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>();

            rt.pivot            = new Vector2(0f, 1f);
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta        = new Vector2(cellSize, cellSize);

            ApplyVisual(img, visual, hp, maxHp);
            _cells[pos] = (go, img);
        }

        // ── Обновление HP ─────────────────────────────────────────────────────

        public void UpdateCellHp(Vector2Int pos, int newHp, int maxHp, ObstacleVisualData visual)
        {
            if (!_cells.TryGetValue(pos, out var entry)) return;

            var sprite = visual.GetSprite(newHp, maxHp);
            if (sprite != null)
            {
                entry.img.sprite = sprite;
            }
            else
            {
                entry.img.DOColor(visual.FallbackColor, 0.12f)
                    .SetEase(Ease.OutFlash)
                    .SetLink(entry.go);
            }

            entry.go.GetComponent<RectTransform>()
                .DOShakeScale(0.2f, 0.12f, 10, 90f)
                .SetLink(entry.go);
        }

        // ── Очистка ───────────────────────────────────────────────────────────

        public void ClearCell(Vector2Int pos)
        {
            if (!_cells.TryGetValue(pos, out var entry)) return;
            _cells.Remove(pos);

            entry.go.GetComponent<RectTransform>()
                .DOScale(Vector3.zero, 0.2f)
                .SetEase(Ease.InBack)
                .SetLink(entry.go)
                .OnComplete(() => Destroy(entry.go));
        }

        public void ClearAll()
        {
            foreach (var entry in _cells.Values)
                if (entry.go != null) Destroy(entry.go);
            _cells.Clear();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void ApplyVisual(Image img, ObstacleVisualData visual, int hp, int maxHp)
        {
            var sprite = visual.GetSprite(hp, maxHp);
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color  = Color.white;
            }
            else
            {
                img.color = visual.FallbackColor;
            }
        }
    }
}
