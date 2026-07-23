using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Brokers;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Services.Workflows;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancePlatform.UnitTests.Trading;

public class BuyAssetChainTests
{
    [Fact]
    public async Task Buy_flows_investment_instruction_to_asset_and_holds_units_on_investment_account()
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

        var claim = new TriggerClaimService(harness.Store, NullLogger<TriggerClaimService>.Instance);
        var workflows = new WorkflowEnqueueService(
            claim,
            harness.Directory,
            harness.Instructions,
            harness.Cash,
            new SimulatedBrokerTradingProvider());

        await workflows.EnqueueBuyAsync(new BuyWorkflowCommand
        {
            CustomerId = provisioned.Customer.Id,
            AccountId = tradingAccountId,
            AssetSymbol = "VWRL",
            Quantity = 2m,
            Currency = "GBP",
            IdempotencyKey = "buy-chain-1"
        });

        await harness.DrainQueuesAsync(QueueNames.Investment, QueueNames.AssetTrading);

        var investmentAccountId = harness.Directory.FindInvestmentAccountByTradingAccount(tradingAccountId)!.Id;
        var fillNotional = 2m * SimulatedBrokerTradingProvider.DefaultUnitPrice;

        harness.Store.GetAll().Should().Contain(t =>
            t.TriggerCode == TriggerCodes.InvestmentReceiveMoney && t.Status == TriggerStatus.Completed);
        harness.Store.GetAll().Should().Contain(t =>
            t.TriggerCode == TriggerCodes.InvestmentInvestMoney && t.Status == TriggerStatus.Completed);
        harness.Store.GetAll().Should().Contain(t =>
            t.TriggerCode == TriggerCodes.AssetBuyAsset && t.Status == TriggerStatus.Completed);
        harness.Store.GetAll().Should().NotContain(t => t.TriggerCode == TriggerCodes.BuyAsset);

        harness.Trading.GetPosition(investmentAccountId, "VWRL").Should().Be(2m);
        harness.Cash.GetSettled(tradingAccountId, "GBP").Should().Be(500m - fillNotional);
        harness.Cash.GetSettled(investmentAccountId, "GBP").Should().Be(0m);

        var instruction = harness.Instructions.GetByIdempotencyKey("buy-chain-1:instruction");
        instruction.Should().NotBeNull();
        instruction!.Status.Should().Be(InvestmentInstructionStatus.Completed);
        instruction.OrderId.Should().NotBeNull();

        harness.Orders.GetByAccount(investmentAccountId).Should().ContainSingle(o =>
            o.Status == OrderStatus.Filled
            && o.FillPrice == SimulatedBrokerTradingProvider.DefaultUnitPrice
            && o.Id == instruction.OrderId);

        harness.Orders.GetById(instruction.OrderId!.Value)!.Status.Should().Be(OrderStatus.Filled);
        harness.Orders.OrderCount.Should().Be(1);
    }
}
