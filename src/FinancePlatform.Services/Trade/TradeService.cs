using System.Text.Json;
using FinancePlatform.Models.Allocation;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;

namespace FinancePlatform.Services.Trade;

/// <summary>
/// Main trading component service. Uses cash, orders, positions, ledger, and customer directory.
/// </summary>
public sealed class TradeService(
    ICashService cashService,
    ILedgerService ledgerService,
    IOrderService orderService,
    IPositionService positionService,
    ICustomerDirectory customerDirectory) : ITradeService
{
    private static readonly TimeSpan LockLease = TimeSpan.FromSeconds(30);

    public decimal GetPosition(Guid accountId, string assetSymbol) =>
        positionService.GetQuantity(accountId, assetSymbol);

    public ComponentOperationResult ReceiveMoney(TriggerContext context, TradingReceiveMoneyRequest request)
    {
        if (request.Amount <= 0)
        {
            return ComponentOperationResult.Failure("Receive amount must be positive.");
        }

        var tradingAccount = customerDirectory.FindTradingAccount(request.TradingAccountId);
        if (tradingAccount is null || tradingAccount.CustomerId != request.CustomerId)
        {
            return ComponentOperationResult.Failure("Trading account was not found.");
        }

        var credited = customerDirectory.TryCreditTradingAccount(
            tradingAccount.Id,
            request.Amount,
            context.TriggerId,
            $"{context.IdempotencyKey}:trading-credit");

        if (!credited)
        {
            return ComponentOperationResult.Failure("Unable to credit trading account.");
        }

        // Keep executable cash ledger in sync so Buy/Sell can reserve against the trading account id.
        SyncCashDeposit(tradingAccount.Id, request.Currency, request.Amount, context);

        // Point A: park-only — Trading UI will later create an investment instruction and distribute.
        if (request.ParkOnly)
        {
            return ComponentOperationResult.Success(
                resultJson: $$"""{"status":"trading-parked","tradingAccountId":"{{tradingAccount.Id}}","amount":{{request.Amount}}}""");
        }

        return ComponentOperationResult.Success(
            resultJson: """{"status":"trading-received"}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = TriggerCodes.TradingDistributeMoney,
                    QueueName = QueueNames.Trading,
                    TargetComponent = "Trading",
                    PayloadJson = JsonSerializer.Serialize(request),
                    IdempotencyKey = $"{context.IdempotencyKey}:7002"
                }
            ]);
    }

    public ComponentOperationResult DistributeMoney(
        TriggerContext context,
        AllocationMoneyRequest request,
        string rawPayloadJson)
    {
        _ = request;
        return ComponentOperationResult.Success(
            resultJson: """{"status":"trading-distributed"}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = TriggerCodes.InvestmentReceiveMoney,
                    QueueName = QueueNames.Investment,
                    TargetComponent = "Investment",
                    PayloadJson = rawPayloadJson,
                    IdempotencyKey = $"{context.IdempotencyKey}:8001"
                }
            ]);
    }

    public ComponentOperationResult TransferToCustomer(TriggerContext context, TradingTransferToCustomerRequest request)
    {
        if (request.Amount <= 0)
        {
            return ComponentOperationResult.Failure("Transfer amount must be positive.");
        }

        var tradingAccount = customerDirectory.FindTradingAccount(request.TradingAccountId);
        if (tradingAccount is null || tradingAccount.CustomerId != request.CustomerId)
        {
            return ComponentOperationResult.Failure("Trading account was not found.");
        }

        if (!string.Equals(tradingAccount.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return ComponentOperationResult.Failure("Currency mismatch for trading account.");
        }

        var customerAccount = customerDirectory.FindCustomerAccount(request.CustomerAccountId);
        if (customerAccount is null || customerAccount.CustomerId != request.CustomerId)
        {
            return ComponentOperationResult.Failure("Customer account was not found.");
        }

        var debited = customerDirectory.TryDebitTradingAccount(
            tradingAccount.Id,
            request.Amount,
            context.TriggerId,
            $"{context.IdempotencyKey}:trading-transfer-debit");

        if (!debited)
        {
            return ComponentOperationResult.Failure("Insufficient funds in trading account.");
        }

        SyncCashWithdraw(tradingAccount.Id, request.Currency, request.Amount, context);

        var receivePayload = JsonSerializer.Serialize(new CustomerReceiveMoneyRequest
        {
            CustomerId = request.CustomerId,
            CustomerAccountId = request.CustomerAccountId,
            SourceTradingAccountId = tradingAccount.Id,
            Amount = request.Amount,
            Currency = request.Currency
        });

        return ComponentOperationResult.Success(
            resultJson: """{"status":"trading-transferred-to-customer"}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = TriggerCodes.CustomerReceiveMoney,
                    QueueName = QueueNames.Customer,
                    TargetComponent = "Customer",
                    PayloadJson = receivePayload,
                    IdempotencyKey = $"{context.IdempotencyKey}:6003"
                }
            ]);
    }

    public ComponentOperationResult Buy(TriggerContext context, TradeAssetRequest request)
    {
        Validate(request);

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Buy requires ExternalId (Account).");

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var reserveKey = $"{context.IdempotencyKey}:reserve";
        var orderKey = $"{context.IdempotencyKey}:order";
        var positionKey = $"{context.IdempotencyKey}:position";
        var ledgerKey = $"{context.IdempotencyKey}:ledger";

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

        var reserved = false;
        try
        {
            var reserve = cashService.TryReserve(
                reserveKey,
                accountId,
                request.Currency,
                request.CashAmount,
                context.TriggerId,
                allocationId);

            if (!reserve.Succeeded)
            {
                return ComponentOperationResult.Failure(reserve.Error ?? "Unable to reserve cash for buy.");
            }

            reserved = true;

            var order = orderService.TrySubmit(
                orderKey,
                accountId,
                context.TriggerId,
                allocationId,
                request.AssetSymbol,
                OrderSide.Buy,
                request.Quantity,
                limitPrice: null);

            if (!order.Succeeded)
            {
                cashService.TryReleaseReservation(reserveKey, context.TriggerId);
                return ComponentOperationResult.Failure(order.Error ?? "Order submit failed.");
            }

            positionService.TryApplyBuy(positionKey, accountId, request.AssetSymbol, request.Quantity);

            var consume = cashService.TryConsumeReservation(reserveKey, context.TriggerId);
            if (!consume.Succeeded && !consume.AlreadyApplied)
            {
                return ComponentOperationResult.Failure(consume.Error ?? "Failed to consume cash reservation.");
            }

            reserved = false;

            var ledger = ledgerService.TryPost(
                ledgerKey,
                accountId,
                request.Currency,
                request.CashAmount,
                LedgerEntryType.Debit,
                $"Buy {request.Quantity} {request.AssetSymbol}",
                context.TriggerId,
                allocationId);

            if (!ledger.Succeeded)
            {
                return ComponentOperationResult.Failure(ledger.Error ?? "Ledger debit failed.");
            }

            SyncTradingAccountDebit(accountId, request.CashAmount, context);
            return ComponentOperationResult.Success(resultJson: """{"status":"bought"}""");
        }
        finally
        {
            if (reserved)
            {
                cashService.TryReleaseReservation(reserveKey, context.TriggerId);
            }

            cashService.TryReleaseLock(accountId, request.Currency, context.TriggerId);
        }
    }

    public ComponentOperationResult Sell(TriggerContext context, TradeAssetRequest request)
    {
        Validate(request);

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Sell requires ExternalId (Account).");

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var orderKey = $"{context.IdempotencyKey}:order";
        var positionKey = $"{context.IdempotencyKey}:position";
        var creditKey = $"{context.IdempotencyKey}:credit";
        var ledgerKey = $"{context.IdempotencyKey}:ledger";

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
            var sell = positionService.TryApplySell(
                positionKey,
                accountId,
                request.AssetSymbol,
                request.Quantity);

            if (!sell.Succeeded)
            {
                return ComponentOperationResult.Failure(sell.Error ?? "Sell failed.");
            }

            var order = orderService.TrySubmit(
                orderKey,
                accountId,
                context.TriggerId,
                allocationId,
                request.AssetSymbol,
                OrderSide.Sell,
                request.Quantity,
                limitPrice: null);

            if (!order.Succeeded)
            {
                return ComponentOperationResult.Failure(order.Error ?? "Order submit failed.");
            }

            var credit = cashService.TryDeposit(
                creditKey,
                accountId,
                request.Currency,
                request.CashAmount,
                context.TriggerId);

            if (!credit.Succeeded)
            {
                return ComponentOperationResult.Failure(credit.Error ?? "Cash credit failed.");
            }

            var ledger = ledgerService.TryPost(
                ledgerKey,
                accountId,
                request.Currency,
                request.CashAmount,
                LedgerEntryType.Credit,
                $"Sell {request.Quantity} {request.AssetSymbol}",
                context.TriggerId,
                allocationId);

            if (!ledger.Succeeded)
            {
                return ComponentOperationResult.Failure(ledger.Error ?? "Ledger credit failed.");
            }

            SyncTradingAccountCredit(accountId, request.CashAmount, context);
            return ComponentOperationResult.Success(resultJson: """{"status":"sold"}""");
        }
        finally
        {
            cashService.TryReleaseLock(accountId, request.Currency, context.TriggerId);
        }
    }

    public ComponentOperationResult ReverseBuy(TriggerContext context, TradeAssetRequest request)
    {
        Validate(request);

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Reverse buy requires ExternalId (Account).");

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var positionKey = $"{context.IdempotencyKey}:reverse-position";
        var creditKey = $"{context.IdempotencyKey}:reverse-credit";
        var ledgerKey = $"{context.IdempotencyKey}:reverse-ledger";

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
            var sell = positionService.TryApplySell(
                positionKey,
                accountId,
                request.AssetSymbol,
                request.Quantity);

            if (sell.Succeeded)
            {
                cashService.TryDeposit(
                    creditKey,
                    accountId,
                    request.Currency,
                    request.CashAmount,
                    context.TriggerId);

                ledgerService.TryPost(
                    ledgerKey,
                    accountId,
                    request.Currency,
                    request.CashAmount,
                    LedgerEntryType.Credit,
                    $"Reverse buy {request.Quantity} {request.AssetSymbol}",
                    context.TriggerId,
                    allocationId);
            }

            return ComponentOperationResult.Success(resultJson: """{"status":"buy-reversed"}""");
        }
        finally
        {
            cashService.TryReleaseLock(accountId, request.Currency, context.TriggerId);
        }
    }

    public ComponentOperationResult ReverseSell(TriggerContext context, TradeAssetRequest request)
    {
        var result = Buy(context, request);
        if (result.ResultCode == TriggerResultCode.Success)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"sell-reversed"}""");
        }

        return result;
    }

    private void SyncCashDeposit(Guid tradingAccountId, string currency, decimal amount, TriggerContext context)
    {
        var lockResult = cashService.TryAcquireLock(
            tradingAccountId,
            currency,
            context.TriggerId,
            context.AllocationRequestId ?? context.RootWorkflowId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return;
        }

        try
        {
            cashService.TryDeposit(
                $"{context.IdempotencyKey}:cash-sync-deposit",
                tradingAccountId,
                currency,
                amount,
                context.TriggerId);
        }
        finally
        {
            cashService.TryReleaseLock(tradingAccountId, currency, context.TriggerId);
        }
    }

    private void SyncCashWithdraw(Guid tradingAccountId, string currency, decimal amount, TriggerContext context)
    {
        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var lockResult = cashService.TryAcquireLock(
            tradingAccountId,
            currency,
            context.TriggerId,
            allocationId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return;
        }

        var reserveKey = $"{context.IdempotencyKey}:cash-sync-withdraw";
        try
        {
            var reserve = cashService.TryReserve(
                reserveKey,
                tradingAccountId,
                currency,
                amount,
                context.TriggerId,
                allocationId);

            if (!reserve.Succeeded)
            {
                return;
            }

            cashService.TryConsumeReservation(reserveKey, context.TriggerId);
        }
        finally
        {
            cashService.TryReleaseLock(tradingAccountId, currency, context.TriggerId);
        }
    }

    private void SyncTradingAccountDebit(Guid accountId, decimal amount, TriggerContext context)
    {
        if (customerDirectory.FindTradingAccount(accountId) is null)
        {
            return;
        }

        customerDirectory.TryDebitTradingAccount(
            accountId,
            amount,
            context.TriggerId,
            $"{context.IdempotencyKey}:trading-debit");
    }

    private void SyncTradingAccountCredit(Guid accountId, decimal amount, TriggerContext context)
    {
        if (customerDirectory.FindTradingAccount(accountId) is null)
        {
            return;
        }

        customerDirectory.TryCreditTradingAccount(
            accountId,
            amount,
            context.TriggerId,
            $"{context.IdempotencyKey}:trading-credit-sell");
    }

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
