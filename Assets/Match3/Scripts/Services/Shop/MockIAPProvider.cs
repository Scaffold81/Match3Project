#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Core.Enums;

namespace Match3.Services.Shop
{
    public sealed class MockIAPProvider : IIAPProvider
    {
        public async UniTask<PurchaseResult> PurchaseAsync(string purchaseId, CancellationToken ct)
        {
            await UniTask.Delay(500, cancellationToken: ct);
            return PurchaseResult.Success;
        }
    }
}
