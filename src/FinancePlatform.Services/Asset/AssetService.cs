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
    public ComponentOperationResult Buy(TriggerContext context, AssetOrderRequest request)
    {
        var cashAmount = request.EffectiveCashAmount;
        if (cashAmount <= 0)
        {
            return ComponentOperationResult.Failure("Asset buy requires a positive cash amount.");
        }

        var result = tradeService.Buy(context, ToTradeRequest(request, cashAmount));
        if (result.ResultCode == TriggerResultCode.Success
            && context.AllocationRequestId is { } allocationId)
        {
            allocationService.MarkCompleted(allocationId);
        }

        return result;
    }

    public ComponentOperationResult Sell(TriggerContext context, AssetOrderRequest request)
    {
        var cashAmount = request.EffectiveCashAmount;
        if (cashAmount <= 0)
        {
            return ComponentOperationResult.Failure("Asset sell requires a positive cash amount.");
        }

        return tradeService.Sell(context, ToTradeRequest(request, cashAmount));
    }

    public ComponentOperationResult ReverseBuy(TriggerContext context, AssetOrderRequest request)
    {
        var cashAmount = request.EffectiveCashAmount;
        if (cashAmount <= 0)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"asset-buy-reversed"}""");
        }

        var result = tradeService.ReverseBuy(context, ToTradeRequest(request, cashAmount));
        if (result.ResultCode == TriggerResultCode.Success)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"asset-buy-reversed"}""");
        }

        return result;
    }

    private static TradeAssetRequest ToTradeRequest(AssetOrderRequest request, decimal cashAmount) => new()
    {
        AssetSymbol = request.AssetSymbol,
        Quantity = request.Quantity,
        Currency = request.Currency,
        CashAmount = cashAmount
    };
}
