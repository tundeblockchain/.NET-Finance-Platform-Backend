using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Data;

public class AuditableEntityTests
{
    [Fact]
    public void Domain_models_implement_auditable_contract()
    {
        typeof(Account).Should().Implement<IAuditableEntity>();
        typeof(AllocationRequest).Should().Implement<IAuditableEntity>();
        typeof(CashBalance).Should().Implement<IAuditableEntity>();
        typeof(CashReservation).Should().Implement<IAuditableEntity>();
        typeof(Position).Should().Implement<IAuditableEntity>();
        typeof(Order).Should().Implement<IAuditableEntity>();
        typeof(LedgerEntry).Should().Implement<IAuditableEntity>();
        typeof(SystemEventTrigger).Should().Implement<IAuditableEntity>();
        typeof(SystemEventWorking).Should().Implement<IAuditableEntity>();
    }

    [Fact]
    public async Task In_memory_trigger_mutations_set_changed_by_broker()
    {
        var store = new FinancePlatform.Data.Triggers.InMemoryTriggerStore();
        var enqueued = await store.EnqueueAsync(new FinancePlatform.Data.Triggers.EnqueueTriggerCommand
        {
            TriggerCode = 1001,
            QueueName = "Cash",
            PayloadJson = "{}",
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = "audit-1"
        });

        enqueued.ChangedBy.Should().Be(ChangeActors.Broker);
        enqueued.DateModified.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        var claimed = await store.TryClaimAsync("Cash", "worker-1", TimeSpan.FromSeconds(30));
        claimed.Should().NotBeNull();
        claimed!.Trigger.ChangedBy.Should().Be(ChangeActors.Broker);
        claimed.Working.ChangedBy.Should().Be(ChangeActors.Broker);
    }
}
