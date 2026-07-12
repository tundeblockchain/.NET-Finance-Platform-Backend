using FinancePlatform.Services.Cash;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Cash;

public class IdempotentDepositTests
{
    [Fact]
    public void Idempotent_deposit_does_not_duplicate_side_effects()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", triggerId, null, TimeSpan.FromSeconds(30)).IsHeld.Should().BeTrue();
        cash.TryDeposit("dep-key", accountId, "GBP", 100m, triggerId).AlreadyApplied.Should().BeFalse();
        cash.TryDeposit("dep-key", accountId, "GBP", 100m, triggerId).AlreadyApplied.Should().BeTrue();
        cash.TryReleaseLock(accountId, "GBP", triggerId).Should().BeTrue();

        cash.GetSettled(accountId, "GBP").Should().Be(100m);
    }
}
