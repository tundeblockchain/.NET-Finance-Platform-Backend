using FinancePlatform.Models.Allocation;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Trade;

namespace FinancePlatform.Services.Trade;

public interface ITradeService
{
    ComponentOperationResult ReceiveMoney(TriggerContext context, AllocationMoneyRequest request, string rawPayloadJson);

    ComponentOperationResult DistributeMoney(TriggerContext context, AllocationMoneyRequest request, string rawPayloadJson);

    ComponentOperationResult Buy(TriggerContext context, TradeAssetRequest request);

    ComponentOperationResult Sell(TriggerContext context, TradeAssetRequest request);

    ComponentOperationResult ReverseBuy(TriggerContext context, TradeAssetRequest request);

    ComponentOperationResult ReverseSell(TriggerContext context, TradeAssetRequest request);

    decimal GetPosition(Guid accountId, string assetSymbol);
}
