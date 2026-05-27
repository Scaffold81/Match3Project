#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Match3.Services.Ads
{
    public interface IAdProvider
    {
        bool IsRewardedReady(string unitId);
        bool IsInterstitialReady(string unitId);

        UniTask           InitializeAsync(string appId, CancellationToken ct);
        UniTask<AdResult> ShowRewardedAsync(string unitId, CancellationToken ct);
        UniTask<bool>     ShowInterstitialAsync(string unitId, CancellationToken ct);
    }
}
