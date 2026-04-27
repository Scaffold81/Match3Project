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
}
