#nullable enable

using System;
using Match3.Configs;
using Match3.Core;
using Match3.Core.Enums;
using Match3.Services.Boost;
using Match3.Services.Inventory;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class BoostPresenter : IInitializable, IDisposable
    {
        private readonly BoostService     _boostService;
        private readonly InventoryService _inventoryService;
        private readonly BackpackView     _backpackView;
        private readonly ActiveBoostView  _activeBoostView;
        private readonly GemConfig        _gemConfig;
        private readonly ItemConfig       _itemConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public BoostPresenter(
            BoostService     boostService,
            InventoryService inventoryService,
            BackpackView     backpackView,
            ActiveBoostView  activeBoostView,
            GemConfig        gemConfig,
            ItemConfig       itemConfig)
        {
            _boostService     = boostService;
            _inventoryService = inventoryService;
            _backpackView     = backpackView;
            _activeBoostView  = activeBoostView;
            _gemConfig        = gemConfig;
            _itemConfig       = itemConfig;
        }

        public void Initialize()
        {
            _backpackView.SetIcons(_itemConfig);

            foreach (var boost in InventoryService.AllBoosts)
            {
                var captured = boost;
                _inventoryService.GetCount(boost)
                    .Subscribe(count => _backpackView.UpdateCount(captured, count))
                    .AddTo(_disposables);
            }

            _backpackView.OnBoostClicked
                .Subscribe(boost => _boostService.SelectBoost(boost))
                .AddTo(_disposables);

            _boostService.OnBoostSelected
                .Subscribe(OnBoostSelected)
                .AddTo(_disposables);

            _boostService.OnBoostCancelled
                .Subscribe(_ => _activeBoostView.HideBoost())
                .AddTo(_disposables);

            _boostService.OnBoostApplied
                .Subscribe(_ => _activeBoostView.HideBoost())
                .AddTo(_disposables);

            _activeBoostView.OnCancelClicked
                .Subscribe(_ => _boostService.CancelBoost())
                .AddTo(_disposables);

            _boostService.ActiveBoost
                .Subscribe(boost => _backpackView.SetAllInteractable(boost == BoostType.None))
                .AddTo(_disposables);
        }

        private void OnBoostSelected(BoostType boost)
        {
            var icon = _itemConfig.GetBoostIcon(boost);

            if (icon == null)
                Debug.LogWarning($"[BoostPresenter] Иконка для {boost} не назначена в ItemConfig");

            var fromPos = _backpackView.GetIconWorldPosition(boost);
            _activeBoostView.ShowBoost(
                icon ?? Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f),
                fromPos);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
