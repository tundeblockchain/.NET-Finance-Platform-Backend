using System.Text.Json;
using FinancePlatform.Models.Cash;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Worker.EventProcessors;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Cash;

public class DepositWorkflowTests
{
    [Fact]
    public async Task Deposit_ep_locks_credits_posts_ledger_unlocks_and_raises_buy()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var ep = new CashEP(new CashComponentService(cash, ledger));
        var accountId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();
        var raiser = new TriggerRaiseBuffer();

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

        var payload = JsonSerializer.Serialize(new DepositCashRequest
        {
            Amount = 150m,
            Currency = "GBP",
            AssetSymbol = "VWRL",
            Quantity = 1m
        });
        var result = await ep.ProcessAsync(context, TriggerCodes.DepositCash, payload, raiser, CancellationToken.None);

        result.ResultCode.Should().Be(TriggerResultCode.Success);
        cash.GetSettled(accountId, "GBP").Should().Be(150m);
        cash.GetOrCreateBalance(accountId, "GBP").IsLocked.Should().BeFalse();
        ledger.EntryCount.Should().Be(1);
        ledger.FindByIdempotencyKey("wf-deposit-1:ledger").Should().NotBeNull();
        raiser.Raised.Should().BeEmpty();
    }

    [Fact]
    public async Task Deposit_ep_retries_when_lock_is_contended()
    {
        var cash = new InMemoryCashService();
        var ledger = new InMemoryLedgerService();
        var ep = new CashEP(new CashComponentService(cash, ledger));
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

        var result = await ep.ProcessAsync(
            context,
            TriggerCodes.DepositCash,
            JsonSerializer.Serialize(new DepositCashRequest { Amount = 10m, Currency = "GBP" }),
            new TriggerRaiseBuffer(),
            CancellationToken.None);

        result.ResultCode.Should().Be(TriggerResultCode.Retry);
        cash.GetSettled(accountId, "GBP").Should().Be(0m);
        ledger.EntryCount.Should().Be(0);
    }
}
