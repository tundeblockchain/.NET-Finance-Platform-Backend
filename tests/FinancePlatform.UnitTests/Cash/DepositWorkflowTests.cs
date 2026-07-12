using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Worker.Handlers;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Cash;

public class DepositWorkflowTests
{
    [Fact]
    public async Task Deposit_handler_locks_credits_posts_ledger_and_unlocks()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var handler = new DepositCashHandler(cash, ledger);
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        var context = new TriggerContext
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

        result.ResultCode.Should().Be(TriggerResultCode.Success);
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
        var handler = new DepositCashHandler(cash, ledger);
        var accountId = Guid.NewGuid();

        cash.TryAcquireLock(accountId, "GBP", Guid.NewGuid(), null, TimeSpan.FromMinutes(5));

        var context = new TriggerContext
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

        result.ResultCode.Should().Be(TriggerResultCode.Retry);
        cash.GetSettled(accountId, "GBP").Should().Be(0m);
        ledger.EntryCount.Should().Be(0);
    }
}
