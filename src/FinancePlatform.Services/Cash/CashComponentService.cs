using FinancePlatform.Models.Cash;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Ledger;

namespace FinancePlatform.Services.Cash;

public sealed class CashComponentService(
    ICashService cashService,
    ILedgerService ledgerService) : ICashComponentService
{
    private static readonly TimeSpan LockLease = TimeSpan.FromSeconds(30);

    public ComponentOperationResult Deposit(TriggerContext context, DepositCashRequest request)
    {
        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Deposit requires ExternalId (Account).");

        var lockResult = cashService.TryAcquireLock(
            accountId,
            request.Currency,
            context.TriggerId,
            context.AllocationRequestId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return ComponentOperationResult.Retry("Cash balance is locked by another trigger.");
        }

        try
        {
            var deposit = cashService.TryDeposit(
                context.IdempotencyKey,
                accountId,
                request.Currency,
                request.Amount,
                context.TriggerId);

            if (!deposit.Succeeded)
            {
                return ComponentOperationResult.Failure(deposit.Error ?? "Deposit failed.");
            }

            var ledger = ledgerService.TryPost(
                idempotencyKey: $"{context.IdempotencyKey}:ledger",
                accountId: accountId,
                currency: request.Currency,
                amount: request.Amount,
                entryType: LedgerEntryType.Credit,
                description: "Cash deposit",
                triggerId: context.TriggerId,
                allocationRequestId: context.AllocationRequestId);

            if (!ledger.Succeeded)
            {
                return ComponentOperationResult.Failure(ledger.Error ?? "Ledger posting failed.");
            }

            return ComponentOperationResult.Success(resultJson: """{"status":"deposited"}""");
        }
        finally
        {
            cashService.TryReleaseLock(accountId, request.Currency, context.TriggerId);
        }
    }
}
