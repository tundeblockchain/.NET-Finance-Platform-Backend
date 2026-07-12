using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;
using FinancePlatform.UnitTests.Triggers.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FinancePlatform.UnitTests.Triggers;

public class TriggerRecoveryServiceTests
{
    [Fact]
    public async Task Expired_lease_is_recovered_once_and_becomes_claimable()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-07-12T12:00:00Z"));
        var store = new InMemoryTriggerStore(clock);
        var recovery = CreateRecovery(store, clock);

        await store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = QueueNames.Cash,
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "recover-1"
        });

        var claimed = await store.TryClaimAsync("Cash", "worker-a", TimeSpan.FromSeconds(10));
        claimed.Should().NotBeNull();
        claimed!.Trigger.Status.Should().Be(TriggerStatus.Running);

        clock.Advance(TimeSpan.FromSeconds(11));

        var first = await recovery.RecoverOnceAsync();
        first.Should().ContainSingle();
        first[0].Trigger.Id.Should().Be(claimed.Trigger.Id);
        first[0].Trigger.Status.Should().Be(TriggerStatus.Pending);
        first[0].Trigger.LastError.Should().Be("lease expired");
        store.GetWorking().Should().BeEmpty();

        var second = await recovery.RecoverOnceAsync();
        second.Should().BeEmpty();

        var reclaimed = await store.TryClaimAsync("Cash", "worker-b", TimeSpan.FromSeconds(30));
        reclaimed.Should().NotBeNull();
        reclaimed!.Trigger.Id.Should().Be(claimed.Trigger.Id);
        reclaimed.Working.WorkerInstanceId.Should().Be("worker-b");
    }

    [Fact]
    public async Task Recovery_does_not_steal_active_non_expired_leases()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-07-12T12:00:00Z"));
        var store = new InMemoryTriggerStore(clock);
        var recovery = CreateRecovery(store, clock);

        await store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.BuyAsset,
            QueueName = QueueNames.Trading,
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Cash",
            TargetComponent = "Trading",
            IdempotencyKey = "active-1"
        });

        var claimed = await store.TryClaimAsync("Trading", "worker-a", TimeSpan.FromSeconds(30));
        claimed.Should().NotBeNull();

        clock.Advance(TimeSpan.FromSeconds(5));
        var recovered = await recovery.RecoverOnceAsync();

        recovered.Should().BeEmpty();
        store.GetWorking().Should().ContainSingle(w => w.TriggerId == claimed!.Trigger.Id);
        var stillRunning = await store.GetByIdAsync(claimed!.Trigger.Id);
        stillRunning!.Status.Should().Be(TriggerStatus.Running);
    }

    [Fact]
    public async Task Completed_trigger_is_not_double_completed_after_recovery_window()
    {
        var clock = new MutableTimeProvider(DateTimeOffset.Parse("2026-07-12T12:00:00Z"));
        var store = new InMemoryTriggerStore(clock);
        var recovery = CreateRecovery(store, clock);

        await store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = QueueNames.Cash,
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "complete-1"
        });

        var claimed = await store.TryClaimAsync("Cash", "worker-a", TimeSpan.FromSeconds(10));
        await store.CompleteAsync(claimed!.Trigger.Id, """{"ok":true}""");

        clock.Advance(TimeSpan.FromSeconds(60));
        var recovered = await recovery.RecoverOnceAsync();
        recovered.Should().BeEmpty();

        var trigger = await store.GetByIdAsync(claimed.Trigger.Id);
        trigger!.Status.Should().Be(TriggerStatus.Completed);
        trigger.ResultJson.Should().Contain("ok");
    }

    private static TriggerRecoveryService CreateRecovery(InMemoryTriggerStore store, TimeProvider clock) =>
        new(
            store,
            Options.Create(new TriggerRecoveryOptions { RecoveryBatchSize = 10 }),
            clock,
            NullLogger<TriggerRecoveryService>.Instance);
}
