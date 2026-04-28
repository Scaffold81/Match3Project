#nullable enable

using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "AnimationConfig", menuName = "Match3/Configs/Animation")]
    public sealed class AnimationConfig : ScriptableObject
    {
        [field: SerializeField] public float SwapDuration         { get; private set; } = 0.2f;
        [field: SerializeField] public float SwapReturnDuration   { get; private set; } = 0.15f;
        [field: SerializeField] public float FallDuration         { get; private set; } = 0.3f;
        [field: SerializeField] public float MatchDestroyDuration { get; private set; } = 0.25f;
        [field: SerializeField] public float SelectDuration       { get; private set; } = 0.15f;
        [field: SerializeField] public float SelectScale          { get; private set; } = 1.15f;

        [field: SerializeField]
        [field: Tooltip("Длительность анимации сжатия/разжатия фишек при перемешивании (сек)")]
        public float ShuffleFoldDuration { get; private set; } = 0.2f;
    }
}
