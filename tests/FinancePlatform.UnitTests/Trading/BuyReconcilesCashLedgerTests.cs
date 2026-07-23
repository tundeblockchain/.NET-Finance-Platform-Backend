using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Brokers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class BuyReconcilesCashLedgerTests
{
    [Fact]
    public async Task Buy_reconciles_CashBalance_when_TradingAccount_has_parked_funds()
    {
        var harness = TriggerExecutionTestHarness.Create();
        var provisioned = harness.Customer.CreateCustomer(new CreateCustomerRequest
        {
            Email = "reconcile@example.com",
            FirstName = "Re",
            LastName = "Concile",
            Currency = "GBP"
        });

        var tradingAccountId = provisioned.TradingAccount.Id;

        // Simulate historical drift: TradingAccount credited without CashBalance sync.
        harness.Directory.TryCreditTradingAccount(tradingAccountId, 500m, Guid.NewGuid(), "drift-trading");
        harness.Cash.GetSettled(tradingAccountId, "GBP").Should().Be(0m);

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
            IdempotencyKey = "buy-reconcile-1"
        });

        await harness.DrainQueuesAsync(QueueNames.Trading);

        harness.Store.GetAll().Should().Contain(t =>
            t.TriggerCode == TriggerCodes.BuyAsset && t.Status == TriggerStatus.Completed);
        harness.Trading.GetPosition(tradingAccountId, "VWRL").Should().Be(2m);
        harness.Cash.GetSettled(tradingAccountId, "GBP")
            .Should().Be(500m - (2m * SimulatedBrokerTradingProvider.DefaultUnitPrice));
    }
}
