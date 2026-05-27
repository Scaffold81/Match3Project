#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Match3.Services.Ads
{
    public sealed class MockAdProvider : IAdProvider
    {
        public bool IsRewardedReady(string unitId)     => true;
        public bool IsInterstitialReady(string unitId) => true;

        public UniTask InitializeAsync(string appId, CancellationToken ct) => UniTask.CompletedTask;

        public async UniTask<AdResult> ShowRewardedAsync(string unitId, CancellationToken ct)
        {
            Debug.Log($"[MockAd] Rewarded shown: {unitId}");
            await UniTask.Delay(500, cancellationToken: ct);
            return AdResult.Success();
        }

        public async UniTask<bool> ShowInterstitialAsync(string unitId, CancellationToken ct)
        {
            Debug.Log($"[MockAd] Interstitial shown: {unitId}");
            await UniTask.Delay(300, cancellationToken: ct);
            return true;
        }
    }
}
