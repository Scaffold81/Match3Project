#nullable enable

using System;
using Match3.Configs;
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
        private readonly BoostService      _boostService;
        private readonly InventoryService  _inventoryService;
        private readonly BackpackView      _backpackView;
        private readonly ActiveBoostView   _activeBoostView;
        private readonly GemConfig         _gemConfig;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public BoostPresenter(
            BoostService     boostService,
            InventoryService inventoryService,
            BackpackView     backpackView,
            ActiveBoostView  activeBoostView,
            GemConfig        gemConfig)
        {
            _boostService     = boostService;
            _inventoryService = inventoryService;
            _backpackView     = backpackView;
            _activeBoostView  = activeBoostView;
            _gemConfig        = gemConfig;
        }

        public void Initialize()
        {
            // Инициализируем счётчики на кнопках
            foreach (var boost in InventoryService.AllBoosts)
            {
                var captured = boost;
                _inventoryService.GetCount(boost)
                    .Subscribe(count => _backpackView.UpdateCount(captured, count))
                    .AddTo(_disposables);
            }

            // Клик по кнопке в рюкзаке
            _backpackView.OnBoostClicked
                .Subscribe(boost => _boostService.SelectBoost(boost))
                .AddTo(_disposables);

            // Буст выбран → показываем иконку в шапке
            _boostService.OnBoostSelected
                .Subscribe(boost => OnBoostSelected(boost))
                .AddTo(_disposables);

            // Буст отменён → скрываем иконку
            _boostService.OnBoostCancelled
                .Subscribe(_ => _activeBoostView.HideBoost())
                .AddTo(_disposables);

            // Буст применён → скрываем иконку
            _boostService.OnBoostApplied
                .Subscribe(_ => _activeBoostView.HideBoost())
                .AddTo(_disposables);

            // Нажатие на иконку в шапке — отмена буста
            _activeBoostView.OnCancelClicked
                .Subscribe(_ => _boostService.CancelBoost())
                .AddTo(_disposables);

            // Активный буст — блокируем рюкзак
            _boostService.ActiveBoost
                .Subscribe(boost => _backpackView.SetAllInteractable(boost == BoostType.None))
                .AddTo(_disposables);
        }

        private void OnBoostSelected(BoostType boost)
        {
            // Получаем иконку буста из GemConfig
            Sprite? icon = null;

            if (boost == BoostType.Hint || boost == BoostType.Shuffle)
            {
                // TODO: добавить иконки Hint/Shuffle в GemConfig или отдельный BoostConfig
                Debug.LogWarning($"[BoostPresenter] Иконка для {boost} не настроена в GemConfig");
            }
            else
            {
                // SuperGem — берём иконку из SuperGemIcons
                var superType = BoostTypeToSuperGemType(boost);
                icon = _gemConfig.GetSuperGemIcon(superType)?.Icon;
            }

            var fromPos = _backpackView.GetIconWorldPosition(boost);
            _activeBoostView.ShowBoost(icon ?? Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, 4, 4), Vector2.one * 0.5f), fromPos);

            Debug.LogWarning($"[BoostPresenter] Буст {boost} активирован — иконка в шапке");
        }

        private static SuperGemType BoostTypeToSuperGemType(BoostType boost) => boost switch
        {
            BoostType.HorizontalArrow => SuperGemType.HorizontalArrow,
            BoostType.VerticalArrow   => SuperGemType.VerticalArrow,
            BoostType.ColorBomb       => SuperGemType.ColorBomb,
            BoostType.Bomb            => SuperGemType.Bomb,
            BoostType.MegaBomb        => SuperGemType.MegaBomb,
            _                         => SuperGemType.None,
        };

        public void Dispose() => _disposables.Dispose();
    }
}
