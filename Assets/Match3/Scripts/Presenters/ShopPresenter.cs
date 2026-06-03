#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Services.Shop;
using Match3.Views;
using R3;
using UnityEngine;
using Zenject;

namespace Match3.Presenters
{
    public sealed class ShopPresenter : IInitializable, IDisposable
    {
        private readonly ShopView    _view;
        private readonly ShopService _shopService;
        private readonly ShopConfig  _shopConfig;
        private readonly ItemConfig  _itemConfig;

        private readonly CompositeDisposable     _disposables = new();
        private readonly CancellationTokenSource _cts         = new();

        [Inject]
        public ShopPresenter(
            ShopView    view,
            ShopService shopService,
            ShopConfig  shopConfig,
            ItemConfig  itemConfig)
        {
            _view        = view;
            _shopService = shopService;
            _shopConfig  = shopConfig;
            _itemConfig  = itemConfig;
        }

        public void Initialize()
        {
            _view.Bind(_shopConfig, _itemConfig);

            _view.OnBuyClicked
                .Subscribe(purchaseId => HandleBuyAsync(purchaseId, _cts.Token).Forget())
                .AddTo(_disposables);
        }

        private async UniTaskVoid HandleBuyAsync(string purchaseId, CancellationToken ct)
        {
            _view.SetAllCardsInteractable(false);

            try
            {
                var result = await _shopService.BuyWithIAPAsync(purchaseId, ct);

                if (result != PurchaseResult.NotEnoughCoins && result != PurchaseResult.Success)
                    Debug.LogError($"ShopPresenter: purchase failed for '{purchaseId}' result={result}");
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"ShopPresenter: purchase failed: {e.Message}");
            }
            finally
            {
                _view.SetAllCardsInteractable(true);
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
            _disposables.Dispose();
        }
    }
}
