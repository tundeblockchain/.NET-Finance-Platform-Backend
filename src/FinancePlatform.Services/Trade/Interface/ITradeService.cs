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

    /// <summary>
    /// Moves parked funds from the trading account back to the customer account (7003 → 6003).
    /// </summary>
    ComponentOperationResult TransferToCustomer(TriggerContext context, TradingTransferToCustomerRequest request);

    Task<ComponentOperationResult> BuyAsync(
        TriggerContext context,
        TradeAssetRequest request,
        CancellationToken cancellationToken = default);

    Task<ComponentOperationResult> SellAsync(
        TriggerContext context,
        TradeAssetRequest request,
        CancellationToken cancellationToken = default);

    Task<ComponentOperationResult> ReverseBuyAsync(
        TriggerContext context,
        TradeAssetRequest request,
        CancellationToken cancellationToken = default);

    Task<ComponentOperationResult> ReverseSellAsync(
        TriggerContext context,
        TradeAssetRequest request,
        CancellationToken cancellationToken = default);

    decimal GetPosition(Guid accountId, string assetSymbol);
}
