#nullable enable

using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "Match3/Configs/Board")]
    public sealed class BoardConfig : ScriptableObject
    {
        [field: SerializeField] public float CellSize { get; private set; } = 1f;
        [field: SerializeField] public float CellSpacing { get; private set; } = 0f;
    }
}
