using System.Text.Json;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Brokers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Triggers;
using FinancePlatform.UnitTests.Support;
using FinancePlatform.Worker.EventProcessors;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class TradeEPSellTests
{
    [Fact]
    public async Task Sell_updates_position_and_is_idempotent()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var positions = new InMemoryPositionService();
        var orders = new InMemoryOrderService();
        var trade = TradeServiceTestFactory.Create(cash, ledger, orders, positions);
        var ep = new TradeEP(trade);
        var accountId = Guid.NewGuid();
        var seedTrigger = Guid.NewGuid();

        // Simulated default unit price is 100 → buy 3 costs 300; sell 2 credits 200.
        cash.TryAcquireLock(accountId, "GBP", seedTrigger, Guid.NewGuid(), TimeSpan.FromMinutes(1));
        cash.TryDeposit("seed", accountId, "GBP", 300m, seedTrigger);
        cash.TryReleaseLock(accountId, "GBP", seedTrigger);

        var buyContext = CreateContext(accountId, "buy-1");
        (await ep.ProcessAsync(
            buyContext,
            TriggerCodes.BuyAsset,
            JsonSerializer.Serialize(new TradeAssetRequest
            {
                AssetSymbol = "VWRL",
                Quantity = 3m,
                Currency = "GBP"
            }),
            new TriggerRaiseBuffer(),
            CancellationToken.None)).ResultCode.Should().Be(TriggerResultCode.Success);

        var sellPayload = JsonSerializer.Serialize(new TradeAssetRequest
        {
            AssetSymbol = "VWRL",
            Quantity = 2m,
            Currency = "GBP"
        });

        var sellContext = CreateContext(accountId, "sell-1");
        (await ep.ProcessAsync(sellContext, TriggerCodes.SellAsset, sellPayload, new TriggerRaiseBuffer(), CancellationToken.None))
            .ResultCode.Should().Be(TriggerResultCode.Success);

        var repeat = await ep.ProcessAsync(sellContext, TriggerCodes.SellAsset, sellPayload, new TriggerRaiseBuffer(), CancellationToken.None);
        repeat.ResultCode.Should().Be(TriggerResultCode.Success);

        positions.GetQuantity(accountId, "VWRL").Should().Be(1m);
        cash.GetSettled(accountId, "GBP").Should().Be(2m * SimulatedBrokerTradingProvider.DefaultUnitPrice);
        orders.OrderCount.Should().Be(2);
    }

    private static TriggerContext CreateContext(Guid accountId, string idempotencyKey) => new()
    {
        TriggerId = Guid.NewGuid(),
        RootWorkflowId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        ExternalId = accountId,
        ExternalType = ExternalEntityType.Account,
        SourceComponent = "Api",
        TargetComponent = "Trading",
        IdempotencyKey = new(idempotencyKey)
    };
}
