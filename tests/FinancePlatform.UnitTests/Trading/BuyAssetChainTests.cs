using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Brokers;
using FinancePlatform.Services.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class BuyAssetChainTests
{
    [Fact]
    public async Task Buy_executes_via_broker_and_holds_units_on_trading_account()
    {
        var harness = TriggerExecutionTestHarness.Create();
        var provisioned = harness.Customer.CreateCustomer(new CreateCustomerRequest
        {
            Email = "buy@example.com",
            FirstName = "Buy",
            LastName = "Chain",
            Currency = "GBP"
        });

        var tradingAccountId = provisioned.TradingAccount.Id;
        harness.Directory.TryCreditTradingAccount(tradingAccountId, 500m, Guid.NewGuid(), "seed-trading");
        var seedTriggerId = Guid.NewGuid();
        harness.Cash.TryAcquireLock(tradingAccountId, "GBP", seedTriggerId, Guid.NewGuid(), TimeSpan.FromMinutes(1));
        harness.Cash.TryDeposit("seed-cash", tradingAccountId, "GBP", 500m, seedTriggerId);
        harness.Cash.TryReleaseLock(tradingAccountId, "GBP", seedTriggerId);

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.BuyAsset,
            QueueName = QueueNames.Trading,
            PayloadJson = JsonSerializer.Serialize(new TradeAssetRequest
            {
                AssetSymbol = "VWRL",
                Quantity = 2m,
                Currency = "GBP"
            }),
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExternalId = tradingAccountId,
            ExternalType = ExternalEntityType.TradingAccount,
            SourceComponent = "Api",
            TargetComponent = "Trading",
            IdempotencyKey = "buy-chain-1"
        });

        await harness.DrainQueuesAsync(QueueNames.Trading);

        harness.Store.GetAll().Should().Contain(t => t.TriggerCode == TriggerCodes.BuyAsset && t.Status == TriggerStatus.Completed);
        harness.Trading.GetPosition(tradingAccountId, "VWRL").Should().Be(2m);
        harness.Cash.GetSettled(tradingAccountId, "GBP")
            .Should().Be(500m - (2m * SimulatedBrokerTradingProvider.DefaultUnitPrice));
        harness.Orders.GetByAccount(tradingAccountId).Should().ContainSingle(o =>
            o.Status == OrderStatus.Filled
            && o.FillPrice == SimulatedBrokerTradingProvider.DefaultUnitPrice);
    }
}
