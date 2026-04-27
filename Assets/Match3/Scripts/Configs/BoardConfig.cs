#nullable enable

using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "Match3/Configs/Board")]
    public sealed class BoardConfig : ScriptableObject
    {
        [field: SerializeField]
        [field: Tooltip("Отступ от краёв рамки доски до сетки фишек (в пикселях Canvas).\n" +
                        "Рекомендуемые значения: 4–16px.")]
        public float BoardPadding { get; private set; } = 8f;

        [field: SerializeField]
        [field: Tooltip("Зазор между фишками (в пикселях Canvas).\n" +
                        "Рекомендуемые значения: 4–10px.")]
        public float CellSpacing { get; private set; } = 6f;

        [field: SerializeField]
        [field: Tooltip("Внутренний отступ спрайта фишки от края её ячейки (в пикселях Canvas).\n" +
                        "0 = фишка занимает всю ячейку. Рекомендуемые значения: 0–4px.")]
        public float GemPadding { get; private set; } = 2f;
    }
}
