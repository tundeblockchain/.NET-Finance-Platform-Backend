using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Ledger;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Cash;

public class LedgerServiceTests
{
    [Fact]
    public void Duplicate_idempotency_key_does_not_create_second_ledger_entry()
    {
        var ledger = new InMemoryLedgerService();
        var accountId = Guid.NewGuid();

        ledger.TryPost("led-1", accountId, "GBP", 50m, LedgerEntryType.Credit, "deposit", Guid.NewGuid(), null)
            .AlreadyApplied.Should().BeFalse();
        ledger.TryPost("led-1", accountId, "GBP", 50m, LedgerEntryType.Credit, "deposit", Guid.NewGuid(), null)
            .AlreadyApplied.Should().BeTrue();

        ledger.EntryCount.Should().Be(1);
    }
}
