using FinancePlatform.Data.Sql;
using FinancePlatform.Models;
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
