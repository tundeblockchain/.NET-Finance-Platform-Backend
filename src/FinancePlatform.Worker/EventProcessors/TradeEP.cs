using System.Text.Json;
using FinancePlatform.Models.Allocation;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Trade;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.EventProcessors;

/// <summary>
/// Trading component event processor — routes codes to <see cref="ITradeService"/>.
/// </summary>
public sealed class TradeEP(ITradeService tradeService) : ITriggerEventProcessor
{
    public string Name => "TradeEP";

    public ComponentType? ComponentType => Models.Enums.ComponentType.Trading;

    public bool CanProcess(int triggerCode)
    {
        var absolute = TriggerCodes.Absolute(triggerCode);
        return TriggerCodes.IsInRange(triggerCode, Models.Enums.ComponentType.Trading)
            || absolute is TriggerCodes.BuyAsset or TriggerCodes.SellAsset;
    }

    public Task<TriggerHandlerResult> ProcessAsync(
        TriggerContext context,
        int triggerCode,
        string payloadJson,
        ITriggerRaiser raiser,
        CancellationToken cancellationToken)
    {
        var absolute = TriggerCodes.Absolute(triggerCode);
        var isAction = TriggerCodes.IsAction(triggerCode);

        return Task.FromResult((absolute, isAction) switch
        {
            (TriggerCodes.TradingReceiveMoney, true) => EpResult.From(
                tradeService.ReceiveMoney(context, RequireAllocation(payloadJson), payloadJson), raiser),
            (TriggerCodes.TradingDistributeMoney, true) => EpResult.From(
                tradeService.DistributeMoney(context, RequireAllocation(payloadJson), payloadJson), raiser),
            (TriggerCodes.BuyAsset, true) => EpResult.From(
                tradeService.Buy(context, RequireTrade(payloadJson)), raiser),
            (TriggerCodes.BuyAsset, false) => EpResult.From(
                tradeService.ReverseBuy(context, RequireTrade(payloadJson)), raiser),
            (TriggerCodes.SellAsset, true) => EpResult.From(
                tradeService.Sell(context, RequireTrade(payloadJson)), raiser),
            (TriggerCodes.SellAsset, false) => EpResult.From(
                tradeService.ReverseSell(context, RequireTrade(payloadJson)), raiser),
            _ => TriggerHandlerResult.Failure($"TradeEP does not handle trigger code {triggerCode}.")
        });
    }

    private static AllocationMoneyRequest RequireAllocation(string payloadJson) =>
        JsonSerializer.Deserialize<AllocationMoneyRequest>(payloadJson)
        ?? throw new InvalidOperationException("Allocation payload is required.");

    private static TradeAssetRequest RequireTrade(string payloadJson) =>
        JsonSerializer.Deserialize<TradeAssetRequest>(payloadJson)
        ?? throw new InvalidOperationException("Trade payload is required.");
}
