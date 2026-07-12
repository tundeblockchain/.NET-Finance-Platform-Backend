using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FinancePlatform.UnitTests.Triggers;

public class TriggerHeartbeatServiceTests
{
    [Fact]
    public async Task Heartbeat_failure_marks_worker_unhealthy()
    {
        var store = new InMemoryTriggerStore();
        var health = new WorkerHealthTracker();
        var heartbeat = new TriggerHeartbeatService(
            store,
            TimeProvider.System,
            Options.Create(new TriggerRecoveryOptions()),
            health,
            NullLogger<TriggerHeartbeatService>.Instance);

        health.IsHealthy.Should().BeTrue();

        var act = () => heartbeat.BeatTriggerAsync(
            Guid.NewGuid(),
            "worker-a",
            TimeSpan.FromSeconds(30));

        await act.Should().ThrowAsync<InvalidOperationException>();
        health.IsHealthy.Should().BeFalse();
        health.UnhealthyReason.Should().Contain("Trigger heartbeat failed");
    }

    [Fact]
    public async Task Successful_heartbeat_marks_worker_healthy_again()
    {
        var store = new InMemoryTriggerStore();
        var health = new WorkerHealthTracker();
        health.MarkUnhealthy("previous failure");

        var heartbeat = new TriggerHeartbeatService(
            store,
            TimeProvider.System,
            Options.Create(new TriggerRecoveryOptions()),
            health,
            NullLogger<TriggerHeartbeatService>.Instance);

        await store.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = QueueNames.Cash,
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "hb-1"
        });

        var claimed = await store.TryClaimAsync("Cash", "worker-a", TimeSpan.FromSeconds(30));
        await heartbeat.BeatTriggerAsync(claimed!.Trigger.Id, "worker-a", TimeSpan.FromSeconds(30));

        health.IsHealthy.Should().BeTrue();
        health.UnhealthyReason.Should().BeNull();
    }
}
