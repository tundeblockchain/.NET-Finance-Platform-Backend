using System.Text.Json;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Worker.Handlers;

/// <summary>
/// Deposit workflow: lock → credit settled → ledger posting → unlock.
/// Contended lock returns Retry (no wait).
/// </summary>
public sealed class DepositCashHandler(
    ICashService cashService,
    ILedgerService ledgerService) : ITriggerHandler
{
    private static readonly TimeSpan LockLease = TimeSpan.FromSeconds(30);

    public int TriggerCode => TriggerCodes.DepositCash;

    public Task<TriggerHandlerResult> ExecuteAsync(
        TriggerContext context,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DepositCashPayload>(payloadJson)
            ?? throw new InvalidOperationException("Deposit payload is required.");

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Deposit requires ExternalId (Account).");

        var lockResult = cashService.TryAcquireLock(
            accountId,
            payload.Currency,
            context.TriggerId,
            context.AllocationRequestId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return Task.FromResult(TriggerHandlerResult.Retry("Cash balance is locked by another trigger."));
        }

        try
        {
            var deposit = cashService.TryDeposit(
                context.IdempotencyKey,
                accountId,
                payload.Currency,
                payload.Amount,
                context.TriggerId);

            if (!deposit.Succeeded)
            {
                return Task.FromResult(TriggerHandlerResult.Failure(deposit.Error ?? "Deposit failed."));
            }

            var ledger = ledgerService.TryPost(
                idempotencyKey: $"{context.IdempotencyKey}:ledger",
                accountId: accountId,
                currency: payload.Currency,
                amount: payload.Amount,
                entryType: LedgerEntryType.Credit,
                description: "Cash deposit",
                triggerId: context.TriggerId,
                allocationRequestId: context.AllocationRequestId);

            if (!ledger.Succeeded)
            {
                return Task.FromResult(TriggerHandlerResult.Failure(ledger.Error ?? "Ledger posting failed."));
            }

            var next = new NextTriggerRequest
            {
                TriggerCode = TriggerCodes.BuyAsset,
                QueueName = "Trading",
                TargetComponent = "Trading",
                PayloadJson = JsonSerializer.Serialize(new BuyAssetPayload
                {
                    AssetSymbol = payload.AssetSymbol,
                    Quantity = payload.Quantity
                }),
                IdempotencyKey = $"{context.IdempotencyKey}:buy"
            };

            return Task.FromResult(TriggerHandlerResult.Success(
                resultJson: """{"status":"deposited"}""",
                nextTriggers: [next]));
        }
        finally
        {
            cashService.TryReleaseLock(accountId, payload.Currency, context.TriggerId);
        }
    }
}
