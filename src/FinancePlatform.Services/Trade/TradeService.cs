using System.Text.Json;
using FinancePlatform.Models.Allocation;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Investment;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Positions;

namespace FinancePlatform.Services.Trade;

/// <summary>
/// Main trading component service. Uses cash, orders, positions, ledger, and customer directory.
/// </summary>
public sealed class TradeService(
    ICashService cashService,
    ILedgerService ledgerService,
    IPositionService positionService,
    ICustomerDirectory customerDirectory,
    IInvestmentInstructionStore instructionStore) : ITradeService
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

        var tradingAccount = customerDirectory.FindTradingAccount(accountId);
        if (tradingAccount is null)
        {
            return ComponentOperationResult.Failure("Buy requires a registered trading account.");
        }

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var reserveKey = $"{context.IdempotencyKey}:reserve";

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

            var debited = customerDirectory.TryDebitTradingAccount(
                accountId,
                request.CashAmount,
                context.TriggerId,
                $"{context.IdempotencyKey}:trading-debit");

            if (!debited)
            {
                cashService.TryReleaseReservation(reserveKey, context.TriggerId);
                return ComponentOperationResult.Failure("Insufficient funds in trading account.");
            }

            var consume = cashService.TryConsumeReservation(reserveKey, context.TriggerId);
            if (!consume.Succeeded && !consume.AlreadyApplied)
            {
                return ComponentOperationResult.Failure(consume.Error ?? "Failed to consume cash reservation.");
            }

            reserved = false;

            var investmentAccount = customerDirectory.EnsureInvestmentAccount(
                tradingAccount.CustomerId,
                tradingAccount.Id,
                request.Currency);

            customerDirectory.EnsureTradingToInvestmentDistribution(
                tradingAccount.CustomerId,
                tradingAccount.Id,
                investmentAccount.Id);

            var instructionResult = instructionStore.TryCreate(new InvestmentInstruction
            {
                CustomerId = tradingAccount.CustomerId,
                TradingAccountId = tradingAccount.Id,
                InvestmentAccountId = investmentAccount.Id,
                AssetSymbol = request.AssetSymbol,
                Quantity = request.Quantity,
                CashAmount = request.CashAmount,
                Currency = request.Currency,
                Side = OrderSide.Buy,
                Status = InvestmentInstructionStatus.Pending,
                IdempotencyKey = context.IdempotencyKey.Value
            });

            if (!instructionResult.Succeeded || instructionResult.Instruction is null)
            {
                return ComponentOperationResult.Failure(instructionResult.Error ?? "Unable to create investment instruction.");
            }

            var instruction = instructionResult.Instruction;
            instructionStore.TryUpdateStatus(instruction.Id, InvestmentInstructionStatus.Processing);

            var investPayload = JsonSerializer.Serialize(new InvestMoneyRequest
            {
                InstructionId = instruction.Id,
                CustomerId = tradingAccount.CustomerId,
                TradingAccountId = tradingAccount.Id,
                InvestmentAccountId = investmentAccount.Id,
                Amount = request.CashAmount,
                CashAmount = request.CashAmount,
                Currency = request.Currency,
                AssetSymbol = request.AssetSymbol,
                Quantity = request.Quantity
            });

            return ComponentOperationResult.Success(
                resultJson: $$"""{"status":"buy-instruction-created","instructionId":"{{instruction.Id}}"}""",
                nextTriggers:
                [
                    new NextTriggerSpec
                    {
                        TriggerCode = TriggerCodes.InvestmentReceiveMoney,
                        QueueName = QueueNames.Investment,
                        TargetComponent = "Investment",
                        PayloadJson = investPayload,
                        IdempotencyKey = $"{context.IdempotencyKey}:8001"
                    }
                ]);
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

        var tradingAccount = customerDirectory.FindTradingAccount(accountId);
        if (tradingAccount is null)
        {
            return ComponentOperationResult.Failure("Sell requires a registered trading account.");
        }

        var investmentAccount = customerDirectory.EnsureInvestmentAccount(
            tradingAccount.CustomerId,
            tradingAccount.Id,
            request.Currency);

        customerDirectory.EnsureTradingToInvestmentDistribution(
            tradingAccount.CustomerId,
            tradingAccount.Id,
            investmentAccount.Id);

        var instructionResult = instructionStore.TryCreate(new InvestmentInstruction
        {
            CustomerId = tradingAccount.CustomerId,
            TradingAccountId = tradingAccount.Id,
            InvestmentAccountId = investmentAccount.Id,
            AssetSymbol = request.AssetSymbol,
            Quantity = request.Quantity,
            CashAmount = request.CashAmount,
            Currency = request.Currency,
            Side = OrderSide.Sell,
            Status = InvestmentInstructionStatus.Pending,
            IdempotencyKey = context.IdempotencyKey.Value
        });

        if (!instructionResult.Succeeded || instructionResult.Instruction is null)
        {
            return ComponentOperationResult.Failure(instructionResult.Error ?? "Unable to create investment instruction.");
        }

        var instruction = instructionResult.Instruction;
        instructionStore.TryUpdateStatus(instruction.Id, InvestmentInstructionStatus.Processing);

        var investPayload = JsonSerializer.Serialize(new InvestMoneyRequest
        {
            InstructionId = instruction.Id,
            CustomerId = tradingAccount.CustomerId,
            TradingAccountId = tradingAccount.Id,
            InvestmentAccountId = investmentAccount.Id,
            Amount = request.CashAmount,
            CashAmount = request.CashAmount,
            Currency = request.Currency,
            AssetSymbol = request.AssetSymbol,
            Quantity = request.Quantity
        });

        return ComponentOperationResult.Success(
            resultJson: $$"""{"status":"sell-instruction-created","instructionId":"{{instruction.Id}}"}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = TriggerCodes.InvestmentInvestMoney,
                    QueueName = QueueNames.Investment,
                    TargetComponent = "Investment",
                    PayloadJson = investPayload,
                    IdempotencyKey = $"{context.IdempotencyKey}:8002"
                }
            ]);
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
