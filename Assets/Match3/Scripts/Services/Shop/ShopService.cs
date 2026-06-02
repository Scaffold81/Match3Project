#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Configs;
using Match3.Core.Enums;
using Match3.Core.Models;
using R3;

namespace Match3.Services.Shop
{
    public sealed class ShopService : IDisposable
    {
        private readonly ShopConfig     _shopConfig;
        private readonly IIAPProvider   _iapProvider;
        private readonly CoinService    _coinService;
        private readonly RewardService  _rewardService;

        private readonly Subject<RewardData[]> _onPurchaseSuccess = new();
        public Observable<RewardData[]> OnPurchaseSuccess => _onPurchaseSuccess;

        public ShopService(
            ShopConfig    shopConfig,
            IIAPProvider  iapProvider,
            CoinService   coinService,
            RewardService rewardService)
        {
            _shopConfig    = shopConfig;
            _iapProvider   = iapProvider;
            _coinService   = coinService;
            _rewardService = rewardService;
        }

        public async UniTask<PurchaseResult> BuyWithCoinsAsync(string purchaseId, CancellationToken ct)
        {
            var item = FindItem(purchaseId);

            if (!_coinService.TrySpend(item.CoinCost))
                return PurchaseResult.NotEnoughCoins;

            _rewardService.GrantAll(item.Rewards);
            _onPurchaseSuccess.OnNext(item.Rewards);
            return PurchaseResult.Success;
        }

        public async UniTask<PurchaseResult> BuyWithIAPAsync(string purchaseId, CancellationToken ct)
        {
            var result = await _iapProvider.PurchaseAsync(purchaseId, ct);

            if (result != PurchaseResult.Success)
                return result;

            var item = FindItem(purchaseId);
            _rewardService.GrantAll(item.Rewards);
            _onPurchaseSuccess.OnNext(item.Rewards);
            return PurchaseResult.Success;
        }

        private ShopItemData FindItem(string purchaseId)
        {
            foreach (var item in _shopConfig.Items)
            {
                if (item.PurchaseId == purchaseId)
                    return item;
            }

            throw new ArgumentException($"ShopItem not found for {nameof(purchaseId)}: {purchaseId}");
        }

        public void Dispose() => _onPurchaseSuccess.Dispose();
    }
}
