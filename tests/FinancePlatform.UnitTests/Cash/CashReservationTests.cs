using FinancePlatform.Services.Cash;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Cash;

public class CashReservationTests
{
    [Fact]
    public void Reserve_increases_reserved_and_release_restores_available()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", triggerId, allocationId, TimeSpan.FromMinutes(1));
        cash.TryDeposit("dep", accountId, "GBP", 200m, triggerId);

        var reserved = cash.TryReserve("res-1", accountId, "GBP", 75m, triggerId, allocationId);
        reserved.Succeeded.Should().BeTrue();
        cash.GetAvailable(accountId, "GBP").Should().Be(125m);

        cash.TryReleaseReservation("res-1", triggerId).Succeeded.Should().BeTrue();
        cash.GetAvailable(accountId, "GBP").Should().Be(200m);
        cash.GetSettled(accountId, "GBP").Should().Be(200m);
        cash.TryReleaseLock(accountId, "GBP", triggerId);
    }

    [Fact]
    public void Consume_reservation_reduces_settled_and_reserved()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", triggerId, allocationId, TimeSpan.FromMinutes(1));
        cash.TryDeposit("dep", accountId, "GBP", 100m, triggerId);
        cash.TryReserve("res-1", accountId, "GBP", 30m, triggerId, allocationId);

        cash.TryConsumeReservation("res-1", triggerId).Succeeded.Should().BeTrue();

        cash.GetSettled(accountId, "GBP").Should().Be(70m);
        cash.GetAvailable(accountId, "GBP").Should().Be(70m);
        cash.TryReleaseLock(accountId, "GBP", triggerId);
    }

    [Fact]
    public void Insufficient_available_cash_fails_reservation()
    {
        var cash = new InMemoryCashService();
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", triggerId, Guid.NewGuid(), TimeSpan.FromMinutes(1));
        cash.TryDeposit("dep", accountId, "GBP", 20m, triggerId);

        var result = cash.TryReserve("res", accountId, "GBP", 50m, triggerId, Guid.NewGuid());

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Contain("Insufficient");
    }
}
