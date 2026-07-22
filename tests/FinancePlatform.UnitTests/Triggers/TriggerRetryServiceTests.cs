using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FinancePlatform.UnitTests.Triggers;

public class TriggerRetryServiceTests
{
    [Fact]
    public async Task ScheduleRetryAsync_fails_when_max_attempts_reached()
    {
        var store = new InMemoryTriggerStore();
        var options = Options.Create(new TriggerRetryOptions
        {
            BaseDelayMilliseconds = 10,
            MaxDelayMilliseconds = 100,
            JitterFactor = 0,
            MaxAttempts = 10
        });
        var sut = new TriggerRetryService(
            store,
            options,
            TimeProvider.System,
            NullLogger<TriggerRetryService>.Instance);

        var enqueued = await store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "max-attempts-1"
        });

        // Claim 10 times via Running state so AttemptCount == MaxAttempts.
        ClaimedTrigger? claimed = null;
        for (var i = 0; i < 10; i++)
        {
            if (i > 0)
            {
                await store.RetryAsync(enqueued.Id, "prep", DateTimeOffset.UtcNow.AddMilliseconds(-1));
            }

            claimed = await store.TryClaimAsync("Cash", $"worker-{i}", TimeSpan.FromSeconds(30));
            claimed.Should().NotBeNull();
        }

        claimed!.Trigger.AttemptCount.Should().Be(10);

        await sut.ScheduleRetryAsync(claimed.Trigger, "still busy");

        var trigger = store.GetAll().Single();
        trigger.Status.Should().Be(TriggerStatus.Failed);
        trigger.LastError.Should().Contain("max attempts 10 exceeded");
        trigger.CompletedUtc.Should().NotBeNull();
        store.GetWorking().Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleRetryAsync_reschedules_when_under_max_attempts()
    {
        var store = new InMemoryTriggerStore();
        var options = Options.Create(new TriggerRetryOptions
        {
            BaseDelayMilliseconds = 50,
            MaxDelayMilliseconds = 100,
            JitterFactor = 0,
            MaxAttempts = 10
        });
        var sut = new TriggerRetryService(
            store,
            options,
            TimeProvider.System,
            NullLogger<TriggerRetryService>.Instance);

        await store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "under-max-1"
        });

        var claimed = await store.TryClaimAsync("Cash", "worker-1", TimeSpan.FromSeconds(30));
        claimed!.Trigger.AttemptCount.Should().Be(1);

        await sut.ScheduleRetryAsync(claimed.Trigger, "transient");

        var trigger = store.GetAll().Single();
        trigger.Status.Should().Be(TriggerStatus.Pending);
        trigger.LastError.Should().Be("transient");
        trigger.NextAttemptUtc.Should().NotBeNull();
    }
}

public class TriggerRequeueTests
{
    [Fact]
    public async Task RequeueAsync_moves_failed_trigger_to_pending_and_resets_attempts()
    {
        var store = new InMemoryTriggerStore();
        await store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "requeue-1"
        });

        var claimed = await store.TryClaimAsync("Cash", "worker-1", TimeSpan.FromSeconds(30));
        await store.FailAsync(claimed!.Trigger.Id, "downstream unavailable");

        var failed = store.GetAll().Single();
        failed.Status.Should().Be(TriggerStatus.Failed);
        failed.AttemptCount.Should().Be(1);

        var requeued = await store.RequeueAsync(failed.Id, changedBy: "ops");

        requeued.Status.Should().Be(TriggerStatus.Pending);
        requeued.AttemptCount.Should().Be(0);
        requeued.CompletedUtc.Should().BeNull();
        requeued.LastError.Should().Be("requeued after failure");
        requeued.ChangedBy.Should().Be("ops");
        requeued.NextAttemptUtc.Should().NotBeNull();

        var claimable = await store.TryClaimAsync("Cash", "worker-2", TimeSpan.FromSeconds(30));
        claimable.Should().NotBeNull();
        claimable!.Trigger.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task RequeueAsync_rejects_non_failed_trigger()
    {
        var store = new InMemoryTriggerStore();
        var enqueued = await store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = "Cash",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "requeue-reject-1"
        });

        var act = async () => await store.RequeueAsync(enqueued.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pending*Pending*");
    }
}
