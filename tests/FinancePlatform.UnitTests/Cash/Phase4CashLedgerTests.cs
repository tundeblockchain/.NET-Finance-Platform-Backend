using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Ledger;
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

public class DepositWorkflowTests
{
    [Fact]
    public async Task Deposit_handler_locks_credits_posts_ledger_and_unlocks()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var handler = new FinancePlatform.Worker.Handlers.DepositCashHandler(cash, ledger);
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        var context = new FinancePlatform.Models.Dtos.TriggerContext
        {
            TriggerId = triggerId,
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExternalId = accountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = new("wf-deposit-1")
        };

        var payload = """{"Amount":150.0,"Currency":"GBP","AssetSymbol":"VWRL","Quantity":1.0}""";
        var result = await handler.ExecuteAsync(context, payload, CancellationToken.None);

        result.ResultCode.Should().Be(FinancePlatform.Models.Enums.TriggerResultCode.Success);
        cash.GetSettled(accountId, "GBP").Should().Be(150m);
        cash.GetOrCreateBalance(accountId, "GBP").IsLocked.Should().BeFalse();
        ledger.EntryCount.Should().Be(1);
        ledger.FindByIdempotencyKey("wf-deposit-1:ledger").Should().NotBeNull();
    }

    [Fact]
    public async Task Deposit_handler_retries_when_lock_is_contended()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var handler = new FinancePlatform.Worker.Handlers.DepositCashHandler(cash, ledger);
        var accountId = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", Guid.NewGuid(), null, TimeSpan.FromMinutes(5));

        var context = new FinancePlatform.Models.Dtos.TriggerContext
        {
            TriggerId = Guid.NewGuid(),
            RootWorkflowId = Guid.NewGuid(),
            CorrelationId = Guid.NewGuid(),
            ExternalId = accountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = new("wf-deposit-retry")
        };

        var result = await handler.ExecuteAsync(
            context,
            """{"Amount":10.0,"Currency":"GBP"}""",
            CancellationToken.None);

        result.ResultCode.Should().Be(FinancePlatform.Models.Enums.TriggerResultCode.Retry);
        cash.GetSettled(accountId, "GBP").Should().Be(0m);
        ledger.EntryCount.Should().Be(0);
    }
}
