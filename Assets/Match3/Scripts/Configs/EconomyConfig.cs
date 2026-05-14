#nullable enable

using UnityEngine;

namespace Match3.Configs
{
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "Match3/Configs/Economy")]
    public sealed class EconomyConfig : ScriptableObject
    {
        // ── Жизни ────────────────────────────────────────────────────────────

        [field: SerializeField]
        [field: Tooltip("Максимальное количество жизней у игрока.")]
        public int MaxLives { get; private set; } = 5;

        [field: SerializeField]
        [field: Tooltip("Время восстановления одной жизни в секундах.\n1800 = 30 минут.")]
        public float LifeRegenSeconds { get; private set; } = 1800f;

        [field: SerializeField]
        [field: Tooltip("Стоимость покупки жизней в монетах.")]
        public int LivesPurchasePrice { get; private set; } = 300;

        [field: SerializeField]
        [field: Tooltip("Количество жизней при покупке за монеты.")]
        public int LivesPurchaseAmount { get; private set; } = 5;

        // ── Монеты ───────────────────────────────────────────────────────────

        [field: SerializeField]
        [field: Tooltip("Количество монет при первом запуске игры.")]
        public int InitialCoins { get; private set; } = 500;
    }
}
