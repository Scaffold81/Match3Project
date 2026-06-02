#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using Match3.Core.Enums;

namespace Match3.Services.Shop
{
    public interface IIAPProvider
    {
        UniTask<PurchaseResult> PurchaseAsync(string purchaseId, CancellationToken ct);
    }
}
