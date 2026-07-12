using FinancePlatform.Models.Components;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Allocation;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Ledger;

namespace FinancePlatform.Services.Customer;

/// <summary>
/// Main customer component service. Uses cash, ledger, and allocation as supporting services.
/// </summary>
public sealed class CustomerService(
    ICashService cashService,
    ILedgerService ledgerService,
    IAllocationService allocationService) : ICustomerService
{
    private static readonly TimeSpan LockLease = TimeSpan.FromSeconds(30);

    public ComponentOperationResult DistributeMoney(
        TriggerContext context,
        DistributeMoneyRequest request,
        string rawPayloadJson)
    {
        var amount = request.EffectiveCashAmount;
        if (amount <= 0)
        {
            return ComponentOperationResult.Failure("Allocation requires a positive amount.");
        }

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Allocation requires ExternalId (Account).");

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        allocationService.EnsureStarted(
            allocationId,
            accountId,
            customerId: null,
            amount,
            request.Currency,
            context.RootWorkflowId,
            context.IdempotencyKey);

        var lockResult = cashService.TryAcquireLock(
            accountId,
            request.Currency,
            context.TriggerId,
            allocationId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return ComponentOperationResult.Retry("Cash balance is locked by another trigger.");
        }

        try
        {
            var deposit = cashService.TryDeposit(
                $"{context.IdempotencyKey}:deposit",
                accountId,
                request.Currency,
                amount,
                context.TriggerId);

            if (!deposit.Succeeded)
            {
                return ComponentOperationResult.Failure(deposit.Error ?? "Customer deposit failed.");
            }

            var ledger = ledgerService.TryPost(
                $"{context.IdempotencyKey}:ledger",
                accountId,
                request.Currency,
                amount,
                LedgerEntryType.Credit,
                "Customer distribute money",
                context.TriggerId,
                allocationId);

            if (!ledger.Succeeded)
            {
                return ComponentOperationResult.Failure(ledger.Error ?? "Ledger posting failed.");
            }

            allocationService.MarkProcessing(allocationId);

            return ComponentOperationResult.Success(
                resultJson: """{"status":"customer-distributed"}""",
                nextTriggers:
                [
                    new NextTriggerSpec
                    {
                        TriggerCode = TriggerCodes.TradingReceiveMoney,
                        QueueName = QueueNames.Trading,
                        TargetComponent = "Trading",
                        PayloadJson = rawPayloadJson,
                        IdempotencyKey = $"{context.IdempotencyKey}:7001"
                    }
                ]);
        }
        finally
        {
            cashService.TryReleaseLock(accountId, request.Currency, context.TriggerId);
        }
    }
}
