#nullable enable

using System;
using Match3.Core.Enums;
using Match3.Services.Hint;
using Match3.Services.Inventory;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Services.Boost
{
    /// <summary>
    /// Управляет выбором и применением бустов во время игры.
    /// Живёт в SceneContext.
    /// </summary>
    public sealed class BoostService : IDisposable
    {
        private readonly InventoryService _inventoryService;
        private readonly HintService      _hintService;

        // ── Активный буст ────────────────────────────────────────────────────
        private readonly ReactiveProperty<BoostType> _activeBoost = new(BoostType.None);
        public ReadOnlyReactiveProperty<BoostType> ActiveBoost => _activeBoost;
        public bool HasActiveBoost => _activeBoost.Value != BoostType.None;

        // ── События ──────────────────────────────────────────────────────────
        private readonly Subject<BoostType>    _onBoostSelected   = new();
        private readonly Subject<BoostType>    _onBoostCancelled  = new();
        private readonly Subject<(BoostType boost, Vector2Int pos)> _onBoostApplied = new();
        private readonly Subject<Unit>         _onShuffleApplied  = new();
        private readonly Subject<(Vector2Int from, Vector2Int to)> _onHintApplied = new();

        public Observable<BoostType>    OnBoostSelected   => _onBoostSelected;
        public Observable<BoostType>    OnBoostCancelled  => _onBoostCancelled;
        public Observable<(BoostType boost, Vector2Int pos)> OnBoostApplied => _onBoostApplied;
        public Observable<Unit>         OnShuffleApplied  => _onShuffleApplied;
        public Observable<(Vector2Int from, Vector2Int to)> OnHintApplied => _onHintApplied;

        [Inject]
        public BoostService(InventoryService inventoryService, HintService hintService)
        {
            _inventoryService = inventoryService;
            _hintService      = hintService;
        }

        // ── Выбор буста ───────────────────────────────────────────────────────

        /// <summary>
        /// Вызывается при нажатии на буст в рюкзаке.
        /// Hint/Shuffle — применяются сразу.
        /// SuperGem — переходим в режим ожидания клика на поле.
        /// </summary>
        public void SelectBoost(BoostType boost)
        {
            if (!_inventoryService.HasAny(boost))
            {
                Debug.LogWarning($"[BoostService] Нет {boost} в инвентаре — отказ");
                return;
            }

            // Мгновенные бусты — применяем сразу
            if (boost == BoostType.Hint)
            {
                ApplyHint();
                return;
            }

            if (boost == BoostType.Shuffle)
            {
                ApplyShuffle();
                return;
            }

            // Супер-фишки — ждём клик на поле
            _activeBoost.Value = boost;
            _onBoostSelected.OnNext(boost);
            Debug.LogWarning($"[BoostService] Буст выбран: {boost} — ждём клик на поле");
        }

        /// <summary>
        /// Отменяет выбранный буст (нажатие на иконку в шапке или второй тап на кнопку).
        /// </summary>
        public void CancelBoost()
        {
            if (_activeBoost.Value == BoostType.None) return;
            var cancelled = _activeBoost.Value;
            _activeBoost.Value = BoostType.None;
            _onBoostCancelled.OnNext(cancelled);
            Debug.LogWarning($"[BoostService] Буст отменён: {cancelled}");
        }

        // ── Применение на поле ────────────────────────────────────────────────

        /// <summary>
        /// Вызывается GameLoopController при клике на ячейку, если HasActiveBoost.
        /// Списывает буст и публикует событие.
        /// </summary>
        public bool TryApplyBoostAt(Vector2Int pos)
        {
            var boost = _activeBoost.Value;
            if (boost == BoostType.None) return false;

            if (!_inventoryService.TrySpend(boost))
            {
                CancelBoost();
                return false;
            }

            _activeBoost.Value = BoostType.None;
            _onBoostApplied.OnNext((boost, pos));
            Debug.LogWarning($"[BoostService] Буст применён: {boost} в {pos}");
            return true;
        }

        // ── Мгновенные бусты ─────────────────────────────────────────────────

        private void ApplyHint()
        {
            var swaps = _hintService.GetPossibleSwaps();
            if (swaps.Count == 0)
            {
                Debug.LogWarning("[BoostService] Hint: нет доступных ходов");
                return;
            }

            if (!_inventoryService.TrySpend(BoostType.Hint)) return;

            var hint = swaps[UnityEngine.Random.Range(0, swaps.Count)];
            _onHintApplied.OnNext(hint);
            Debug.LogWarning($"[BoostService] Hint применён: {hint.from} → {hint.to}");
        }

        private void ApplyShuffle()
        {
            if (!_inventoryService.TrySpend(BoostType.Shuffle)) return;
            _onShuffleApplied.OnNext(Unit.Default);
            Debug.LogWarning("[BoostService] Shuffle применён");
        }

        public void Dispose()
        {
            _activeBoost.Dispose();
            _onBoostSelected.Dispose();
            _onBoostCancelled.Dispose();
            _onBoostApplied.Dispose();
            _onShuffleApplied.Dispose();
            _onHintApplied.Dispose();
        }
    }
}
