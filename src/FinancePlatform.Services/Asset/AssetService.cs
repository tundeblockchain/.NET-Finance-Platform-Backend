using FinancePlatform.Models.Asset;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Services.Allocation;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;

namespace FinancePlatform.Services.Asset;

/// <summary>
/// Asset-trading component — the only place buy/sell units are executed.
/// </summary>
public sealed class AssetService(
    ICashService cashService,
    ILedgerService ledgerService,
    IOrderService orderService,
    IPositionService positionService,
    ICustomerDirectory customerDirectory,
    IInvestmentInstructionStore instructionStore,
    IAllocationService allocationService) : IAssetService
{
    private static readonly TimeSpan LockLease = TimeSpan.FromSeconds(30);

    public ComponentOperationResult Buy(TriggerContext context, AssetOrderRequest request)
    {
        var cashAmount = request.EffectiveCashAmount;
        if (cashAmount <= 0)
        {
            return ComponentOperationResult.Failure("Asset buy requires a positive cash amount.");
        }

        var result = ExecuteBuy(context, request.InvestmentAccountId, request, cashAmount);
        if (result.ResultCode == TriggerResultCode.Success)
        {
            instructionStore.TryUpdateStatus(request.InstructionId, InvestmentInstructionStatus.Completed);
            if (context.AllocationRequestId is { } allocationId)
            {
                allocationService.MarkCompleted(allocationId);
            }
        }

        return result;
    }

    public ComponentOperationResult Sell(TriggerContext context, AssetOrderRequest request)
    {
        var cashAmount = request.EffectiveCashAmount;
        if (cashAmount <= 0)
        {
            return ComponentOperationResult.Failure("Asset sell requires a positive cash amount.");
        }

        var result = ExecuteSell(context, request.InvestmentAccountId, request, cashAmount);
        if (result.ResultCode == TriggerResultCode.Success)
        {
            instructionStore.TryUpdateStatus(request.InstructionId, InvestmentInstructionStatus.Completed);
        }

        return result;
    }

    public ComponentOperationResult ReverseBuy(TriggerContext context, AssetOrderRequest request)
    {
        var cashAmount = request.EffectiveCashAmount;
        if (cashAmount <= 0)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"asset-buy-reversed"}""");
        }

        var tradeRequest = ToTradeRequest(request, cashAmount);
        var accountId = request.InvestmentAccountId;
        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var positionKey = $"{context.IdempotencyKey}:reverse-position";
        var creditKey = $"{context.IdempotencyKey}:reverse-credit";
        var ledgerKey = $"{context.IdempotencyKey}:reverse-ledger";

        var lockResult = cashService.TryAcquireLock(
            accountId,
            tradeRequest.Currency,
            context.TriggerId,
            allocationId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return ComponentOperationResult.Retry("Cash balance is locked by another trigger.");
        }

        try
        {
            positionService.TryApplySell(positionKey, accountId, tradeRequest.AssetSymbol, tradeRequest.Quantity);
            cashService.TryDeposit(creditKey, accountId, tradeRequest.Currency, tradeRequest.CashAmount, context.TriggerId);
            ledgerService.TryPost(
                ledgerKey,
                accountId,
                tradeRequest.Currency,
                tradeRequest.CashAmount,
                LedgerEntryType.Credit,
                $"Reverse buy {tradeRequest.Quantity} {tradeRequest.AssetSymbol}",
                context.TriggerId,
                allocationId);

            return ComponentOperationResult.Success(resultJson: """{"status":"asset-buy-reversed"}""");
        }
        finally
        {
            cashService.TryReleaseLock(accountId, tradeRequest.Currency, context.TriggerId);
        }
    }

    private ComponentOperationResult ExecuteBuy(
        TriggerContext context,
        Guid accountId,
        AssetOrderRequest request,
        decimal cashAmount)
    {
        var tradeRequest = ToTradeRequest(request, cashAmount);
        Validate(tradeRequest);

        var order = orderService.GetById(request.OrderId);
        if (order is null)
        {
            return ComponentOperationResult.Failure("Asset order was not found.");
        }

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var reserveKey = $"{context.IdempotencyKey}:reserve";
        var positionKey = $"{context.IdempotencyKey}:position";
        var ledgerKey = $"{context.IdempotencyKey}:ledger";

        var lockResult = cashService.TryAcquireLock(
            accountId,
            tradeRequest.Currency,
            context.TriggerId,
            allocationId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return ComponentOperationResult.Retry("Cash balance is locked by another trigger.");
        }

        var reserved = false;
        try
        {
            var reserve = cashService.TryReserve(
                reserveKey,
                accountId,
                tradeRequest.Currency,
                tradeRequest.CashAmount,
                context.TriggerId,
                allocationId);

            if (!reserve.Succeeded)
            {
                return ComponentOperationResult.Failure(reserve.Error ?? "Unable to reserve cash for asset buy.");
            }

            reserved = true;
            positionService.TryApplyBuy(positionKey, accountId, tradeRequest.AssetSymbol, tradeRequest.Quantity);

            var consume = cashService.TryConsumeReservation(reserveKey, context.TriggerId);
            if (!consume.Succeeded && !consume.AlreadyApplied)
            {
                return ComponentOperationResult.Failure(consume.Error ?? "Failed to consume cash reservation.");
            }

            reserved = false;

            var ledger = ledgerService.TryPost(
                ledgerKey,
                accountId,
                tradeRequest.Currency,
                tradeRequest.CashAmount,
                LedgerEntryType.Debit,
                $"Buy {tradeRequest.Quantity} {tradeRequest.AssetSymbol}",
                context.TriggerId,
                allocationId);

            if (!ledger.Succeeded)
            {
                return ComponentOperationResult.Failure(ledger.Error ?? "Ledger debit failed.");
            }

            orderService.TryMarkFilled(order.Id);
            customerDirectory.TryDebitInvestmentAccount(
                accountId,
                tradeRequest.CashAmount,
                context.TriggerId,
                $"{context.IdempotencyKey}:investment-debit");
            return ComponentOperationResult.Success(resultJson: """{"status":"asset-bought"}""");
        }
        finally
        {
            if (reserved)
            {
                cashService.TryReleaseReservation(reserveKey, context.TriggerId);
            }

            cashService.TryReleaseLock(accountId, tradeRequest.Currency, context.TriggerId);
        }
    }

    private ComponentOperationResult ExecuteSell(
        TriggerContext context,
        Guid accountId,
        AssetOrderRequest request,
        decimal cashAmount)
    {
        var tradeRequest = ToTradeRequest(request, cashAmount);
        Validate(tradeRequest);

        var order = orderService.GetById(request.OrderId);
        if (order is null)
        {
            return ComponentOperationResult.Failure("Asset order was not found.");
        }

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var positionKey = $"{context.IdempotencyKey}:position";
        var creditKey = $"{context.IdempotencyKey}:credit";
        var ledgerKey = $"{context.IdempotencyKey}:ledger";

        var lockResult = cashService.TryAcquireLock(
            accountId,
            tradeRequest.Currency,
            context.TriggerId,
            allocationId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return ComponentOperationResult.Retry("Cash balance is locked by another trigger.");
        }

        try
        {
            var sell = positionService.TryApplySell(
                positionKey,
                accountId,
                tradeRequest.AssetSymbol,
                tradeRequest.Quantity);

            if (!sell.Succeeded)
            {
                return ComponentOperationResult.Failure(sell.Error ?? "Asset sell failed.");
            }

            var credit = cashService.TryDeposit(
                creditKey,
                accountId,
                tradeRequest.Currency,
                tradeRequest.CashAmount,
                context.TriggerId);

            if (!credit.Succeeded)
            {
                return ComponentOperationResult.Failure(credit.Error ?? "Cash credit failed.");
            }

            var ledger = ledgerService.TryPost(
                ledgerKey,
                accountId,
                tradeRequest.Currency,
                tradeRequest.CashAmount,
                LedgerEntryType.Credit,
                $"Sell {tradeRequest.Quantity} {tradeRequest.AssetSymbol}",
                context.TriggerId,
                allocationId);

            if (!ledger.Succeeded)
            {
                return ComponentOperationResult.Failure(ledger.Error ?? "Ledger credit failed.");
            }

            orderService.TryMarkFilled(order.Id);
            customerDirectory.TryCreditInvestmentAccount(
                accountId,
                tradeRequest.CashAmount,
                context.TriggerId,
                $"{context.IdempotencyKey}:investment-credit-sell");
            return ComponentOperationResult.Success(resultJson: """{"status":"asset-sold"}""");
        }
        finally
        {
            cashService.TryReleaseLock(accountId, tradeRequest.Currency, context.TriggerId);
        }
    }

    private static TradeAssetRequest ToTradeRequest(AssetOrderRequest request, decimal cashAmount) => new()
    {
        AssetSymbol = request.AssetSymbol,
        Quantity = request.Quantity,
        Currency = request.Currency,
        CashAmount = cashAmount
    };

    private static void Validate(TradeAssetRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AssetSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Quantity must be positive.");
        }

        if (request.CashAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "CashAmount must be positive.");
        }
    }
}
