using FinancePlatform.Models.Allocation;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Trade;

namespace FinancePlatform.Services.Trade;

public interface ITradeService
{
    /// <summary>
    /// Parks funds into the trading account. When <see cref="TradingReceiveMoneyRequest.ParkOnly"/> is true,
    /// no further distribute trigger is raised (invest is a separate trading-UI action).
    /// </summary>
    ComponentOperationResult ReceiveMoney(TriggerContext context, TradingReceiveMoneyRequest request);

    ComponentOperationResult DistributeMoney(TriggerContext context, AllocationMoneyRequest request, string rawPayloadJson);

    ComponentOperationResult Buy(TriggerContext context, TradeAssetRequest request);

    ComponentOperationResult Sell(TriggerContext context, TradeAssetRequest request);

    ComponentOperationResult ReverseBuy(TriggerContext context, TradeAssetRequest request);

    ComponentOperationResult ReverseSell(TriggerContext context, TradeAssetRequest request);

    decimal GetPosition(Guid accountId, string assetSymbol);
}
