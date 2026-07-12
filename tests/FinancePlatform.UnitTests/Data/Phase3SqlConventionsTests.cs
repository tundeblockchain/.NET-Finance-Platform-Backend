using FinancePlatform.Data.Sql;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Data;

public class SqlObjectNamesTests
{
    [Theory]
    [InlineData("Account", "get_Account_f", "Account_u", "Account_a")]
    [InlineData("AllocationRequest", "get_AllocationRequest_f", "AllocationRequest_u", "AllocationRequest_a")]
    [InlineData("CashBalance", "get_CashBalance_f", "CashBalance_u", "CashBalance_a")]
    [InlineData("Order", "get_Order_f", "Order_u", "Order_a")]
    [InlineData("LedgerEntry", "get_LedgerEntry_f", "LedgerEntry_u", "LedgerEntry_a")]
    public void Archived_models_follow_get_f_u_and_archive_naming(
        string model,
        string getProc,
        string upsertProc,
        string archiveTable)
    {
        SqlObjectNames.HasArchive(model).Should().BeTrue();
        SqlObjectNames.GetProc(model).Should().Be(getProc);
        SqlObjectNames.UpsertProc(model).Should().Be(upsertProc);
        SqlObjectNames.ArchiveTable(model).Should().Be(archiveTable);
    }

    [Theory]
    [InlineData("SystemEventTrigger")]
    [InlineData("SystemEventWorking")]
    public void Trigger_tables_do_not_use_archive_tables(string model)
    {
        SqlObjectNames.HasArchive(model).Should().BeFalse();
        SqlObjectNames.NonArchivedModels.Should().Contain(model);
    }

    [Fact]
    public void Broker_changed_by_constant_is_broker()
    {
        ChangeActors.Broker.Should().Be("broker");
        SqlObjectNames.BrokerChangedBy.Should().Be(ChangeActors.Broker);
    }
}

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
