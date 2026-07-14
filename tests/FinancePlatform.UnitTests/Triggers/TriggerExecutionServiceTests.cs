using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Cash;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

public class TriggerExecutionServiceTests
{
    [Fact]
    public async Task Successful_deposit_processor_completes_without_auto_buy()
    {
        var harness = TriggerExecutionTestHarness.Create();
        var accountId = Guid.NewGuid();

        var root = await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = JsonSerializer.Serialize(new DepositCashRequest
            {
                Amount = 250m,
                Currency = "GBP",
                AssetSymbol = "VWRL",
                Quantity = 2m
            }),
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExternalId = accountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "deposit-1"
        });

        var claimed = await harness.Store.TryClaimAsync("Cash", "worker-a", TimeSpan.FromSeconds(30));
        claimed.Should().NotBeNull();

        await harness.Execution.ExecuteAsync(claimed!);

        var all = harness.Store.GetAll();
        all.Should().Contain(t => t.Id == root.Id && t.Status == TriggerStatus.Completed);
        all.Should().NotContain(t => t.TriggerCode == TriggerCodes.BuyAsset);

        harness.Cash.GetSettled(accountId, "GBP").Should().Be(250m);
    }

    [Fact]
    public async Task Failure_enqueues_compensation_trigger_with_negative_code()
    {
        var harness = TriggerExecutionTestHarness.Create(registerDefaults: false);
        harness.Registry.Register(new AlwaysFailProcessor(TriggerCodes.BuyAsset));

        var trigger = await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.BuyAsset,
            QueueName = "Trading",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Cash",
            TargetComponent = "Trading",
            IdempotencyKey = "buy-fail-1"
        });

        var claimed = await harness.Store.TryClaimAsync("Trading", "worker-b", TimeSpan.FromSeconds(30));
        await harness.Execution.ExecuteAsync(claimed!);

        var all = harness.Store.GetAll();
        all.Should().Contain(t => t.Id == trigger.Id && t.Status == TriggerStatus.Failed);
        all.Should().Contain(t =>
            t.TriggerCode == TriggerCodes.Compensate(TriggerCodes.BuyAsset)
            && t.QueueName == "Reversal");
    }

    [Fact]
    public async Task Retry_result_reschedules_pending_trigger()
    {
        var harness = TriggerExecutionTestHarness.Create(registerDefaults: false);
        harness.Registry.Register(new AlwaysRetryProcessor(TriggerCodes.DepositCash));

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "retry-1"
        });

        var claimed = await harness.Store.TryClaimAsync("Cash", "worker-c", TimeSpan.FromSeconds(30));
        await harness.Execution.ExecuteAsync(claimed!);

        var trigger = harness.Store.GetAll().Single();
        trigger.Status.Should().Be(TriggerStatus.Pending);
        trigger.NextAttemptUtc.Should().NotBeNull();
        trigger.NextAttemptUtc.Should().BeAfter(DateTimeOffset.UtcNow.AddMilliseconds(-50));
        trigger.LastError.Should().Contain("busy");
        harness.Store.GetWorking().Should().BeEmpty();
    }

    [Fact]
    public async Task Trading_buy_chains_through_investment_and_asset()
    {
        var harness = TriggerExecutionTestHarness.Create();
        var provisioned = harness.Customer.CreateCustomer(new CreateCustomerRequest
        {
            Email = "chain@example.com",
            FirstName = "Chain",
            LastName = "Test",
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
                Quantity = 3m,
                Currency = "GBP",
                CashAmount = 500m
            }),
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExternalId = tradingAccountId,
            ExternalType = ExternalEntityType.TradingAccount,
            SourceComponent = "Api",
            TargetComponent = "Trading",
            IdempotencyKey = "chain-buy"
        });

        await harness.DrainQueuesAsync(
            QueueNames.Trading,
            QueueNames.Investment,
            QueueNames.AssetTrading);

        harness.Store.GetAll().Should().Contain(t => t.TriggerCode == TriggerCodes.BuyAsset && t.Status == TriggerStatus.Completed);
        harness.Store.GetAll().Should().Contain(t => t.TriggerCode == TriggerCodes.AssetBuyAsset && t.Status == TriggerStatus.Completed);

        var investmentAccount = harness.Directory.FindInvestmentAccountByTradingAccount(tradingAccountId);
        investmentAccount.Should().NotBeNull();
        harness.Positions.GetQuantity(investmentAccount!.Id, "VWRL").Should().Be(3m);
    }
}
