using FinancePlatform.Services.Cash;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Cash;

public class CashBalanceAvailableTests
{
    [Fact]
    public void Available_equals_settled_minus_reserved()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", triggerId, allocationId, TimeSpan.FromMinutes(1));
        cash.TryDeposit("dep", accountId, "GBP", 100m, triggerId);
        cash.TryReserve("res", accountId, "GBP", 40m, triggerId, allocationId);
        cash.TryReleaseLock(accountId, "GBP", triggerId);

        cash.GetSettled(accountId, "GBP").Should().Be(100m);
        cash.GetAvailable(accountId, "GBP").Should().Be(60m);
        cash.GetOrCreateBalance(accountId, "GBP").Available.Should().Be(60m);
    }
}
