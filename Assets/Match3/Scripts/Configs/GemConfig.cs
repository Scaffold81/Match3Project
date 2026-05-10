#nullable enable

using System;
using Match3.Core.Enums;
using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "GemConfig", menuName = "Match3/Configs/Gem")]
    public sealed class GemConfig : ScriptableObject
    {
        [field: SerializeField]
        [field: Tooltip("Базовый префаб GemView — используется для спавна всех ячеек сетки")]
        public GameObject GemViewPrefab { get; private set; } = null!;

        [field: SerializeField] public GemVisualData[]      Gems          { get; private set; } = Array.Empty<GemVisualData>();
        [field: SerializeField] public SuperGemIconData[]   SuperGemIcons { get; private set; } = Array.Empty<SuperGemIconData>();
        [field: SerializeField] public ObstacleVisualData[] Obstacles     { get; private set; } = Array.Empty<ObstacleVisualData>();

        public GemVisualData? GetVisual(NodeType nodeType)
        {
            foreach (var gem in Gems)
                if (gem.NodeType == nodeType)
                    return gem;
            return null;
        }

        public SuperGemIconData? GetSuperGemIcon(SuperGemType superGemType)
        {
            foreach (var icon in SuperGemIcons)
                if (icon.SuperGemType == superGemType)
                    return icon;
            return null;
        }

        public ObstacleVisualData? GetObstacleVisual(ObstacleType obstacleType)
        {
            foreach (var obs in Obstacles)
                if (obs.ObstacleType == obstacleType)
                    return obs;
            return null;
        }
    }

    [Serializable]
    public sealed class GemVisualData
    {
        [field: SerializeField] public NodeType    NodeType { get; private set; }
        [field: SerializeField] public Sprite      Sprite   { get; private set; } = null!;
        [field: SerializeField] public Color       Color    { get; private set; } = Color.white;
        [field: SerializeField] public GameObject? Prefab   { get; private set; }
    }

    [Serializable]
    public sealed class SuperGemIconData
    {
        [field: SerializeField] public SuperGemType SuperGemType { get; private set; }
        [field: SerializeField] public Sprite       Icon         { get; private set; } = null!;
        [field: SerializeField] public Color        Tint         { get; private set; } = Color.white;
    }

    [Serializable]
    public sealed class ObstacleVisualData
    {
        [field: SerializeField] public ObstacleType ObstacleType { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Спрайт препятствия при полном HP (intact)")]
        public Sprite? SpriteFullHp { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Спрайт при повреждении (HP меньше максима, но > 1)")]
        public Sprite? SpriteDamaged { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Спрайт при HP == 1 (последний удар)")]
        public Sprite? SpriteCritical { get; private set; }

        [field: SerializeField]
        [field: Tooltip("Цвет в отсутствие спрайтов (дев-режим)")]
        public Color FallbackColor { get; private set; } = Color.white;

        /// <summary>
        /// Возвращает нужный спрайт для текущего состояния HP.
        /// Если спрайт не задан — возвращает null (вид использует FallbackColor).
        /// </summary>
        public Sprite? GetSprite(int currentHp, int maxHp)
        {
            if (currentHp >= maxHp) return SpriteFullHp;
            if (currentHp == 1)    return SpriteCritical;
            return SpriteDamaged;
        }
    }
}
