#nullable enable

using System;
using TMPro;
using UnityEngine;

namespace Match3.Views
{
    /// <summary>
    /// HUD кошелька: монеты, жизни, таймер восстановления жизней.
    /// Спавнится из ProjectContext — живёт всю игру (DontDestroyOnLoad).
    ///
    /// Настройка префаба:
    ///   — Canvas: Screen Space – Overlay, Sort Order = 10
    ///   — _timerContainer: скрывается когда жизни на максимуме
    /// </summary>
    public sealed class WalletView : MonoBehaviour
    {
        [SerializeField] private TMP_Text   _coinsText       = null!;
        [SerializeField] private TMP_Text   _livesText       = null!;
        [SerializeField] private TMP_Text   _timerText       = null!;
        [SerializeField] private GameObject _timerContainer  = null!;

        // ── Монеты ────────────────────────────────────────────────────────────

        public void SetCoins(int amount)
        {
            _coinsText.text = amount.ToString();
        }

        // ── Жизни ─────────────────────────────────────────────────────────────

        public void SetLives(int current, int max)
        {
            _livesText.text = $"{current}/{max}";
        }

        // ── Таймер ────────────────────────────────────────────────────────────

        public void ShowTimer(TimeSpan remaining)
        {
            _timerContainer.SetActive(true);
            _timerText.text = FormatTime(remaining);
        }

        public void HideTimer()
        {
            _timerContainer.SetActive(false);
        }

        // ── Утилиты ───────────────────────────────────────────────────────────

        private static string FormatTime(TimeSpan t) =>
            t.TotalHours >= 1
                ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}"
                : $"{t.Minutes:D2}:{t.Seconds:D2}";
    }
}
