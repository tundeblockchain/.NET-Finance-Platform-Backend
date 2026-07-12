using System.Text.Json;
using FinancePlatform.Models.Cash;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Trade;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Worker.EventProcessors;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class TradeEPBuyTests
{
    [Fact]
    public async Task Buy_with_insufficient_available_cash_fails_and_leaves_position_unchanged()
    {
        var (cash, positions, ep) = CreateEp();
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", triggerId, Guid.NewGuid(), TimeSpan.FromMinutes(1));
        cash.TryDeposit("seed", accountId, "GBP", 25m, triggerId);
        cash.TryReleaseLock(accountId, "GBP", triggerId);

        var result = await ep.ProcessAsync(
            CreateContext(accountId, "buy-insufficient"),
            TriggerCodes.BuyAsset,
            JsonSerializer.Serialize(new TradeAssetRequest
            {
                AssetSymbol = "VWRL",
                Quantity = 1m,
                Currency = "GBP",
                CashAmount = 100m
            }),
            new TriggerRaiseBuffer(),
            CancellationToken.None);

        result.ResultCode.Should().Be(TriggerResultCode.Failure);
        result.Message.Should().Contain("Insufficient");
        positions.GetQuantity(accountId, "VWRL").Should().Be(0m);
        cash.GetSettled(accountId, "GBP").Should().Be(25m);
    }

    [Fact]
    public async Task Buy_reserves_consumes_cash_and_increases_position()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var positions = new InMemoryPositionService();
        var orders = new InMemoryOrderService();
        var trade = new TradeService(cash, ledger, orders, positions);
        var ep = new TradeEP(trade);
        var accountId = Guid.NewGuid();
        var seedTrigger = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", seedTrigger, Guid.NewGuid(), TimeSpan.FromMinutes(1));
        cash.TryDeposit("seed", accountId, "GBP", 200m, seedTrigger);
        cash.TryReleaseLock(accountId, "GBP", seedTrigger);

        var result = await ep.ProcessAsync(
            CreateContext(accountId, "buy-ok"),
            TriggerCodes.BuyAsset,
            JsonSerializer.Serialize(new TradeAssetRequest
            {
                AssetSymbol = "VWRL",
                Quantity = 2m,
                Currency = "GBP",
                CashAmount = 150m
            }),
            new TriggerRaiseBuffer(),
            CancellationToken.None);

        result.ResultCode.Should().Be(TriggerResultCode.Success);
        cash.GetSettled(accountId, "GBP").Should().Be(50m);
        positions.GetQuantity(accountId, "VWRL").Should().Be(2m);
        orders.OrderCount.Should().Be(1);
        ledger.EntryCount.Should().Be(1);
    }

    private static (InMemoryCashService Cash, InMemoryPositionService Positions, TradeEP Ep) CreateEp()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var positions = new InMemoryPositionService();
        var orders = new InMemoryOrderService();
        var trade = new TradeService(cash, ledger, orders, positions);
        return (cash, positions, new TradeEP(trade));
    }

    private static TriggerContext CreateContext(Guid accountId, string key) => new()
    {
        TriggerId = Guid.NewGuid(),
        RootWorkflowId = Guid.NewGuid(),
        CorrelationId = Guid.NewGuid(),
        ExternalId = accountId,
        ExternalType = ExternalEntityType.Account,
        SourceComponent = "Cash",
        TargetComponent = "Trading",
        IdempotencyKey = new(key)
    };
}
