using FinancePlatform.Models.Asset;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Services.Allocation;
using FinancePlatform.Services.Trade;

namespace FinancePlatform.Services.Asset;

/// <summary>
/// Main asset-trading component service. Delegates cash/order/position work to <see cref="ITradeService"/>.
/// </summary>
public sealed class AssetService(
    ITradeService tradeService,
    IAllocationService allocationService) : IAssetService
{
    public async Task<ComponentOperationResult> BuyAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return ComponentOperationResult.Failure("Asset buy requires a positive quantity.");
        }

        var result = await tradeService.BuyAsync(context, ToTradeRequest(request), cancellationToken);
        if (result.ResultCode == TriggerResultCode.Success
            && context.AllocationRequestId is { } allocationId)
        {
            allocationService.MarkCompleted(allocationId);
        }

        return result;
    }

    public Task<ComponentOperationResult> SellAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return Task.FromResult(ComponentOperationResult.Failure("Asset sell requires a positive quantity."));
        }

        return tradeService.SellAsync(context, ToTradeRequest(request), cancellationToken);
    }

    public async Task<ComponentOperationResult> ReverseBuyAsync(
        TriggerContext context,
        AssetOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"asset-buy-reversed"}""");
        }

        var result = await tradeService.ReverseBuyAsync(context, ToTradeRequest(request), cancellationToken);
        if (result.ResultCode == TriggerResultCode.Success)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"asset-buy-reversed"}""");
        }

        return result;
    }

    private static TradeAssetRequest ToTradeRequest(AssetOrderRequest request)
    {
        var cashHint = request.EffectiveCashAmount;
        return new TradeAssetRequest
        {
            AssetSymbol = request.AssetSymbol,
            Quantity = request.Quantity,
            Currency = request.Currency,
            CashAmount = cashHint > 0 ? cashHint : null
        };
    }
}
