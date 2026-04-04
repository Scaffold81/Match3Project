#nullable enable

using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "Match3/Configs/Board")]
    public sealed class BoardConfig : ScriptableObject
    {
        [field: SerializeField]
        [field: Tooltip("Отступ от левого и правого края Board в пикселях (canvas units)")]
        public float BoardPadding { get; private set; } = 8f;

        [field: SerializeField]
        [field: Tooltip("Отступ между фишками в пикселях")]
        public float CellSpacing { get; private set; } = 4f;
    }
}
