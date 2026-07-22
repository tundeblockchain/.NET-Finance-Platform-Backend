using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Cash;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Triggers;

public class TriggerExecutionServiceTests
{
    [Fact]
    public async Task Successful_processor_completes_and_raises_child_triggers()
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

        var buyClaimed = await harness.Store.TryClaimAsync("Trading", "worker-a", TimeSpan.FromSeconds(30));
        buyClaimed.Should().NotBeNull();
        await harness.Execution.ExecuteAsync(buyClaimed!);

        var all = harness.Store.GetAll();
        all.Should().Contain(t => t.Id == root.Id && t.Status == TriggerStatus.Completed);
        all.Should().Contain(t =>
            t.TriggerCode == TriggerCodes.BuyAsset
            && t.QueueName == "Trading"
            && t.ParentTriggerId == root.Id
            && t.Status == TriggerStatus.Completed);

        harness.Cash.GetSettled(accountId, "GBP").Should().Be(50m);
        harness.Trading.GetPosition(accountId, "VWRL").Should().Be(2m);
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
    public async Task Deposit_then_buy_chain_processes_end_to_end()
    {
        var harness = TriggerExecutionTestHarness.Create();
        var accountId = Guid.NewGuid();
        var rootId = Guid.NewGuid();

        await harness.Store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = JsonSerializer.Serialize(new DepositCashRequest
            {
                Amount = 500m,
                Currency = "GBP",
                AssetSymbol = "VWRL",
                Quantity = 3m
            }),
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            ExternalId = accountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "chain-deposit"
        });

        for (var i = 0; i < 5; i++)
        {
            var cashClaim = await harness.Store.TryClaimAsync("Cash", "w-cash", TimeSpan.FromSeconds(30));
            if (cashClaim is not null)
            {
                await harness.Execution.ExecuteAsync(cashClaim);
            }

            var tradeClaim = await harness.Store.TryClaimAsync("Trading", "w-trade", TimeSpan.FromSeconds(30));
            if (tradeClaim is not null)
            {
                await harness.Execution.ExecuteAsync(tradeClaim);
            }
        }

        harness.Store.GetAll().Should().Contain(t => t.TriggerCode == TriggerCodes.DepositCash && t.Status == TriggerStatus.Completed);
        harness.Store.GetAll().Should().Contain(t => t.TriggerCode == TriggerCodes.BuyAsset && t.Status == TriggerStatus.Completed);
        harness.Cash.GetSettled(accountId, "GBP").Should().Be(200m);
        harness.Trading.GetPosition(accountId, "VWRL").Should().Be(3m);
    }
}
