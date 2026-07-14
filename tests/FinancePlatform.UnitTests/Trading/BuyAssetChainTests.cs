using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class BuyAssetChainTests
{
    [Fact]
    public async Task Buy_chains_through_investment_to_asset_and_holds_units_on_investment_account()
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
                Currency = "GBP",
                CashAmount = 150m
            }),
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExternalId = tradingAccountId,
            ExternalType = ExternalEntityType.TradingAccount,
            SourceComponent = "Api",
            TargetComponent = "Trading",
            IdempotencyKey = "buy-chain-1"
        });

        await harness.DrainQueuesAsync(
            QueueNames.Trading,
            QueueNames.Investment,
            QueueNames.AssetTrading);

        var investmentAccount = harness.Directory.FindInvestmentAccountByTradingAccount(tradingAccountId);
        investmentAccount.Should().NotBeNull();
        harness.Positions.GetQuantity(investmentAccount!.Id, "VWRL").Should().Be(2m);
        harness.Directory.GetTradingSettled(tradingAccountId).Should().Be(350m);
        harness.Orders.GetByAccount(investmentAccount.Id).Should().ContainSingle(o => o.Status == OrderStatus.Filled);
    }
}
