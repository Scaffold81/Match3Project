#nullable enable

using DG.Tweening;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using Match3.Services;
using Match3.Services.Ads;
using Match3.Services.Inventory;
using R3;
using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Match3.Views
{
    /// <summary>
    /// Рюкзак с бустами. Работает в двух режимах:
    ///   — Попап на карте: _startVisible = false, _toggleButton назначен
    ///   — Панель в LevelSelectPopupView: _startVisible = true, _toggleButton пустой
    /// Оба экземпляра добавить в SceneContext → MonoBehaviours To Inject.
    /// </summary>
    public sealed class BackpackPopupView : MonoBehaviour
    {
        [Header("Режим")]
        [SerializeField] private bool   _startVisible = false;
        [SerializeField] private Button? _showButton;

        [Header("Кнопка закрытия (только для попапа)")]
        [SerializeField] private Button? _closeButton;

        [Header("Анимация")]
        [SerializeField] private CanvasGroup _canvasGroup = null!;

        [Header("Слоты")]
        [SerializeField] private BoostSlotView[] _slots = Array.Empty<BoostSlotView>();
        
        private ResourcePopupService _resourcePopupService = null!;
        private AdConfig             _adConfig             = null!;
        private ItemConfig           _itemConfig           = null!;

        private readonly CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(
            InventoryService     inventoryService,
            ResourcePopupService resourcePopupService,
            AdConfig             adConfig,
            ItemConfig           itemConfig)
        {
            _resourcePopupService = resourcePopupService;
            _adConfig             = adConfig;
            _itemConfig           = itemConfig;

            foreach (var slot in _slots)
            {
                var boostType = slot.BoostType;

                slot.SetIcon(_itemConfig.GetBoostIcon(boostType));

                inventoryService.GetCount(boostType)
                    .Subscribe(count => slot.UpdateCount(count))
                    .AddTo(_disposables);

                slot.OnClicked
                    .Subscribe(_ => OnBoostSlotClicked(boostType))
                    .AddTo(_disposables);
            }
        }

        private void Awake()
        {
            _canvasGroup.alpha          = _startVisible ? 1f : 0f;
            _canvasGroup.interactable   = _startVisible;
            _canvasGroup.blocksRaycasts = _startVisible;

            _showButton?.onClick.AddListener(Show);

            _closeButton?.onClick.AddListener(Hide);

            if (_startVisible)
            {
                if (_showButton != null) _showButton.gameObject.SetActive(false);
                if (_closeButton  != null) _closeButton.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }

        public void Show()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
           
            _canvasGroup
                .DOFade(1f, 0.25f)
                .OnComplete(() =>
                {
                });
        }

        public void Hide()
        {
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            
            _canvasGroup
                .DOFade(0f, 0.2f).OnComplete(() =>
                {
                });
        }

        // ── Приватное ─────────────────────────────────────────────────────────

        private void OnBoostSlotClicked(BoostType boostType)
        {
            var adEntry   = _adConfig.GetPlacement(AdPlacementId.RewardedBoost);
            var rewards   = adEntry?.Rewards ?? Array.Empty<RewardData>();
            var coinPrice = _itemConfig.GetBoostCoinPrice(boostType);

            var rewardIcons = new Sprite?[rewards.Length];
            for (var i = 0; i < rewards.Length; i++)
                rewardIcons[i] = _itemConfig.GetIcon(rewards[i].Type, rewards[i].Boost);

            var request = new ResourcePopupRequest
            {
                Title           = "Получить буст",
                CharacterDialog = "Смотри рекламу или купи за монеты!",
                DialogLocaleId  = "popup.get_boost.dialog",
                Rewards         = rewards,
                RewardIcons     = rewardIcons,
                AdPlacementId   = AdPlacementId.RewardedBoost,
                AdButtonLabel   = "👁 Получить буст",
                CoinPrice       = coinPrice > 0 ? coinPrice : null,
                CoinButtonLabel = coinPrice > 0 ? $"💰 {coinPrice} монет" : string.Empty,
            };

            _resourcePopupService.Request(request);
        }
    }
}
