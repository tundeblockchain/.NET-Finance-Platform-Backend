using FinancePlatform.Services.Cash;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Cash;

public class CashLockTests
{
    [Fact]
    public void Acquire_lock_is_exclusive_for_same_account_currency()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", owner, null, TimeSpan.FromMinutes(1))
            .Status.Should().Be(CashLockStatus.Acquired);

        cash.TryAcquireLock(accountId, "GBP", other, null, TimeSpan.FromMinutes(1))
            .Status.Should().Be(CashLockStatus.Contended);

        cash.TryAcquireLock(accountId, "GBP", owner, null, TimeSpan.FromMinutes(1))
            .Status.Should().Be(CashLockStatus.AlreadyOwned);
    }

    [Fact]
    public void Release_lock_only_succeeds_for_owning_trigger()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", owner, null, TimeSpan.FromMinutes(1));

        cash.TryReleaseLock(accountId, "GBP", other).Should().BeFalse();
        cash.TryReleaseLock(accountId, "GBP", owner).Should().BeTrue();
        cash.TryAcquireLock(accountId, "GBP", other, null, TimeSpan.FromMinutes(1))
            .Status.Should().Be(CashLockStatus.Acquired);
    }

    [Fact]
    public void Deposit_without_owned_lock_fails()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();

        var result = cash.TryDeposit("dep", accountId, "GBP", 10m, Guid.NewGuid());

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("lock");
    }
}
