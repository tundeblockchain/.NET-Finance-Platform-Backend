using FinancePlatform.Models.Asset;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;

namespace FinancePlatform.Services.Asset;

public interface IAssetService
{
    Task<ComponentOperationResult> BuyAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<ComponentOperationResult> SellAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default);

    Task<ComponentOperationResult> ReverseBuyAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default);
}
