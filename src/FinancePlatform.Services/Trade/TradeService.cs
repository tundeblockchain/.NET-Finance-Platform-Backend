using System.Globalization;
using System.Text.Json;
using FinancePlatform.Models.Allocation;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Brokers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Ledger;
using FinancePlatform.Services.Orders;
using FinancePlatform.Services.Positions;
using FinancePlatform.Services.Pricing;

namespace FinancePlatform.Services.Trade;

/// <summary>
/// Main trading component service. Uses cash, orders, positions, ledger, broker, and pricing.
/// External broker I/O never runs while a cash lock is held.
/// </summary>
public sealed class TradeService(
    ICashService cashService,
    ILedgerService ledgerService,
    IOrderService orderService,
    IPositionService positionService,
    ICustomerDirectory customerDirectory,
    IBrokerTradingProvider broker,
    IAssetPriceService assetPrices) : ITradeService
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
        if (!TrySyncCashDeposit(tradingAccount.Id, request.Currency, request.Amount, context))
        {
            customerDirectory.TryDebitTradingAccount(
                tradingAccount.Id,
                request.Amount,
                context.TriggerId,
                $"{context.IdempotencyKey}:trading-credit-rollback");
            return ComponentOperationResult.Failure(
                "Unable to sync executable cash ledger for trading account.");
        }

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

        if (!TrySyncCashWithdraw(tradingAccount.Id, request.Currency, request.Amount, context))
        {
            customerDirectory.TryCreditTradingAccount(
                tradingAccount.Id,
                request.Amount,
                context.TriggerId,
                $"{context.IdempotencyKey}:trading-transfer-debit-rollback");
            return ComponentOperationResult.Failure(
                "Unable to sync executable cash ledger for trading transfer.");
        }

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

    public async Task<ComponentOperationResult> BuyAsync(
        TriggerContext context,
        TradeAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Buy requires ExternalId (Account).");

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var reserveKey = $"{context.IdempotencyKey}:reserve";
        var settleKey = $"{context.IdempotencyKey}:settle";
        var positionKey = $"{context.IdempotencyKey}:position";
        var ledgerKey = $"{context.IdempotencyKey}:ledger";
        var referencePrice = ResolveReferencePrice(request);

        if (!TryResolveOrderKey(context, request, OrderSide.Buy, out var orderKey, out var existingOrder, out var resolveError))
        {
            return ComponentOperationResult.Failure(resolveError ?? "Unable to resolve order.");
        }

        if (existingOrder?.Status == OrderStatus.Filled)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"bought","idempotent":true}""");
        }

        BrokerQuote quote;
        try
        {
            quote = await broker.GetQuoteAsync(request.AssetSymbol, referencePrice, cancellationToken);
        }
        catch (Exception ex)
        {
            return ComponentOperationResult.Failure($"Quote failed: {ex.Message}");
        }

        var unitQuote = quote.Ask > 0 ? quote.Ask : quote.Mid;
        var estimatedCash = RoundMoney(unitQuote * request.Quantity);

        assetPrices.Record(
            quote.AssetSymbol,
            unitQuote,
            request.Currency,
            AssetPriceSource.Quote,
            quote.Provider,
            observedUtc: quote.ObservedUtc);

        // Heal TradingAccount → CashBalance drift so parked funds remain buyable.
        if (!TryReconcileExecutableCash(accountId, request.Currency, context))
        {
            return ComponentOperationResult.Retry("Unable to reconcile executable cash ledger.");
        }

        var reserved = false;
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
            var reserve = cashService.TryReserve(
                reserveKey,
                accountId,
                request.Currency,
                estimatedCash,
                context.TriggerId,
                allocationId);

            if (!reserve.Succeeded)
            {
                return ComponentOperationResult.Failure(reserve.Error ?? "Unable to reserve cash for buy.");
            }

            reserved = true;
        }
        finally
        {
            cashService.TryReleaseLock(accountId, request.Currency, context.TriggerId);
        }

        BrokerOrderExecution execution;
        try
        {
            execution = await broker.PlaceMarketOrderAsync(
                new BrokerMarketOrderRequest(
                    request.AssetSymbol,
                    OrderSide.Buy,
                    request.Quantity,
                    orderKey,
                    unitQuote),
                cancellationToken);
        }
        catch (Exception ex)
        {
            ReleaseReservationSafe(accountId, request.Currency, reserveKey, context.TriggerId, allocationId);
            return ComponentOperationResult.Failure($"Broker buy failed: {ex.Message}");
        }

        var fillNotional = RoundMoney(execution.AverageFillPrice * execution.Quantity);

        lockResult = cashService.TryAcquireLock(
            accountId,
            request.Currency,
            context.TriggerId,
            allocationId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return ComponentOperationResult.Retry("Cash balance is locked by another trigger after broker fill.");
        }

        try
        {
            var order = RecordOrderFill(
                context,
                request,
                accountId,
                allocationId,
                orderKey,
                existingOrder,
                OrderSide.Buy,
                execution);

            if (!order.Succeeded)
            {
                cashService.TryReleaseReservation(reserveKey, context.TriggerId);
                return ComponentOperationResult.Failure(order.Error ?? "Order submit failed.");
            }

            assetPrices.Record(
                execution.AssetSymbol,
                execution.AverageFillPrice,
                request.Currency,
                AssetPriceSource.TradeFill,
                execution.Provider,
                orderId: order.Order?.Id,
                externalOrderId: execution.ExternalOrderId,
                observedUtc: execution.FilledUtc);

            positionService.TryApplyBuy(positionKey, accountId, request.AssetSymbol, request.Quantity);

            if (fillNotional == estimatedCash)
            {
                var consume = cashService.TryConsumeReservation(reserveKey, context.TriggerId);
                if (!consume.Succeeded && !consume.AlreadyApplied)
                {
                    return ComponentOperationResult.Failure(consume.Error ?? "Failed to consume cash reservation.");
                }
            }
            else
            {
                cashService.TryReleaseReservation(reserveKey, context.TriggerId);
                var settleReserve = cashService.TryReserve(
                    settleKey,
                    accountId,
                    request.Currency,
                    fillNotional,
                    context.TriggerId,
                    allocationId);
                if (!settleReserve.Succeeded)
                {
                    return ComponentOperationResult.Failure(
                        settleReserve.Error ?? "Unable to settle cash for broker fill amount.");
                }

                var settleConsume = cashService.TryConsumeReservation(settleKey, context.TriggerId);
                if (!settleConsume.Succeeded && !settleConsume.AlreadyApplied)
                {
                    return ComponentOperationResult.Failure(
                        settleConsume.Error ?? "Failed to consume settlement cash reservation.");
                }
            }

            reserved = false;

            var ledger = ledgerService.TryPost(
                ledgerKey,
                accountId,
                request.Currency,
                fillNotional,
                LedgerEntryType.Debit,
                $"Buy {request.Quantity} {request.AssetSymbol}",
                context.TriggerId,
                allocationId);

            if (!ledger.Succeeded)
            {
                return ComponentOperationResult.Failure(ledger.Error ?? "Ledger debit failed.");
            }

            SyncTradingAccountDebit(accountId, fillNotional, context);
            var fill = execution.AverageFillPrice.ToString(CultureInfo.InvariantCulture);
            var notional = fillNotional.ToString(CultureInfo.InvariantCulture);
            return ComponentOperationResult.Success(
                resultJson: $"{{\"status\":\"bought\",\"provider\":\"{execution.Provider}\",\"fillPrice\":{fill},\"cashAmount\":{notional},\"externalOrderId\":\"{execution.ExternalOrderId}\"}}");
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

    public async Task<ComponentOperationResult> SellAsync(
        TriggerContext context,
        TradeAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Sell requires ExternalId (Account).");

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var positionKey = $"{context.IdempotencyKey}:position";
        var creditKey = $"{context.IdempotencyKey}:credit";
        var ledgerKey = $"{context.IdempotencyKey}:ledger";
        var referencePrice = ResolveReferencePrice(request);

        if (!TryResolveOrderKey(context, request, OrderSide.Sell, out var orderKey, out var existingOrder, out var resolveError))
        {
            return ComponentOperationResult.Failure(resolveError ?? "Unable to resolve order.");
        }

        if (existingOrder?.Status == OrderStatus.Filled)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"sold","idempotent":true}""");
        }

        var available = positionService.GetQuantity(accountId, request.AssetSymbol);
        if (available < request.Quantity)
        {
            return ComponentOperationResult.Failure(
                $"Insufficient position for {request.AssetSymbol}. Available={available}, requested={request.Quantity}.");
        }

        BrokerQuote quote;
        try
        {
            quote = await broker.GetQuoteAsync(request.AssetSymbol, referencePrice, cancellationToken);
        }
        catch (Exception ex)
        {
            return ComponentOperationResult.Failure($"Quote failed: {ex.Message}");
        }

        var unitQuote = quote.Bid > 0 ? quote.Bid : quote.Mid;

        assetPrices.Record(
            quote.AssetSymbol,
            unitQuote,
            request.Currency,
            AssetPriceSource.Quote,
            quote.Provider,
            observedUtc: quote.ObservedUtc);

        BrokerOrderExecution execution;
        try
        {
            execution = await broker.PlaceMarketOrderAsync(
                new BrokerMarketOrderRequest(
                    request.AssetSymbol,
                    OrderSide.Sell,
                    request.Quantity,
                    orderKey,
                    unitQuote),
                cancellationToken);
        }
        catch (Exception ex)
        {
            return ComponentOperationResult.Failure($"Broker sell failed: {ex.Message}");
        }

        var fillNotional = RoundMoney(execution.AverageFillPrice * execution.Quantity);

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

            var order = RecordOrderFill(
                context,
                request,
                accountId,
                allocationId,
                orderKey,
                existingOrder,
                OrderSide.Sell,
                execution);

            if (!order.Succeeded)
            {
                return ComponentOperationResult.Failure(order.Error ?? "Order submit failed.");
            }

            assetPrices.Record(
                execution.AssetSymbol,
                execution.AverageFillPrice,
                request.Currency,
                AssetPriceSource.TradeFill,
                execution.Provider,
                orderId: order.Order?.Id,
                externalOrderId: execution.ExternalOrderId,
                observedUtc: execution.FilledUtc);

            var credit = cashService.TryDeposit(
                creditKey,
                accountId,
                request.Currency,
                fillNotional,
                context.TriggerId);

            if (!credit.Succeeded)
            {
                return ComponentOperationResult.Failure(credit.Error ?? "Cash credit failed.");
            }

            var ledger = ledgerService.TryPost(
                ledgerKey,
                accountId,
                request.Currency,
                fillNotional,
                LedgerEntryType.Credit,
                $"Sell {request.Quantity} {request.AssetSymbol}",
                context.TriggerId,
                allocationId);

            if (!ledger.Succeeded)
            {
                return ComponentOperationResult.Failure(ledger.Error ?? "Ledger credit failed.");
            }

            SyncTradingAccountCredit(accountId, fillNotional, context);
            var fill = execution.AverageFillPrice.ToString(CultureInfo.InvariantCulture);
            var notional = fillNotional.ToString(CultureInfo.InvariantCulture);
            return ComponentOperationResult.Success(
                resultJson: $"{{\"status\":\"sold\",\"provider\":\"{execution.Provider}\",\"fillPrice\":{fill},\"cashAmount\":{notional},\"externalOrderId\":\"{execution.ExternalOrderId}\"}}");
        }
        finally
        {
            cashService.TryReleaseLock(accountId, request.Currency, context.TriggerId);
        }
    }

    public Task<ComponentOperationResult> ReverseBuyAsync(
        TriggerContext context,
        TradeAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        Validate(request);

        var accountId = context.ExternalId
            ?? throw new InvalidOperationException("Reverse buy requires ExternalId (Account).");

        var allocationId = context.AllocationRequestId ?? context.RootWorkflowId;
        var positionKey = $"{context.IdempotencyKey}:reverse-position";
        var creditKey = $"{context.IdempotencyKey}:reverse-credit";
        var ledgerKey = $"{context.IdempotencyKey}:reverse-ledger";
        var cashAmount = ResolveCashForReversal(request);

        var lockResult = cashService.TryAcquireLock(
            accountId,
            request.Currency,
            context.TriggerId,
            allocationId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return Task.FromResult(ComponentOperationResult.Retry("Cash balance is locked by another trigger."));
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
                    cashAmount,
                    context.TriggerId);

                ledgerService.TryPost(
                    ledgerKey,
                    accountId,
                    request.Currency,
                    cashAmount,
                    LedgerEntryType.Credit,
                    $"Reverse buy {request.Quantity} {request.AssetSymbol}",
                    context.TriggerId,
                    allocationId);
            }

            return Task.FromResult(ComponentOperationResult.Success(resultJson: """{"status":"buy-reversed"}"""));
        }
        finally
        {
            cashService.TryReleaseLock(accountId, request.Currency, context.TriggerId);
        }
    }

    public async Task<ComponentOperationResult> ReverseSellAsync(
        TriggerContext context,
        TradeAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await BuyAsync(context, request, cancellationToken);
        if (result.ResultCode == TriggerResultCode.Success)
        {
            return ComponentOperationResult.Success(resultJson: """{"status":"sell-reversed"}""");
        }

        return result;
    }

    private decimal ResolveCashForReversal(TradeAssetRequest request)
    {
        if (request.CashAmount is > 0)
        {
            return RoundMoney(request.CashAmount.Value);
        }

        var latest = assetPrices.GetLatest(request.AssetSymbol);
        if (latest is null || latest.Price <= 0)
        {
            throw new InvalidOperationException(
                $"Cannot reverse buy for {request.AssetSymbol} without a stored price or CashAmount.");
        }

        return RoundMoney(latest.Price * request.Quantity);
    }

    private static decimal? ResolveReferencePrice(TradeAssetRequest request) =>
        request.CashAmount is > 0 && request.Quantity > 0
            ? request.CashAmount.Value / request.Quantity
            : null;

    private static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, 8, MidpointRounding.AwayFromZero);

    private void ReleaseReservationSafe(
        Guid accountId,
        string currency,
        string reserveKey,
        Guid triggerId,
        Guid allocationId)
    {
        var lockResult = cashService.TryAcquireLock(accountId, currency, triggerId, allocationId, LockLease);
        if (!lockResult.IsHeld)
        {
            return;
        }

        try
        {
            cashService.TryReleaseReservation(reserveKey, triggerId);
        }
        finally
        {
            cashService.TryReleaseLock(accountId, currency, triggerId);
        }
    }

    private bool TryResolveOrderKey(
        TriggerContext context,
        TradeAssetRequest request,
        OrderSide side,
        out string orderKey,
        out Order? existingOrder,
        out string? error)
    {
        _ = side;
        existingOrder = null;
        error = null;

        if (request.OrderId is { } orderId && orderId != Guid.Empty)
        {
            existingOrder = orderService.GetById(orderId);
            if (existingOrder is null)
            {
                orderKey = string.Empty;
                error = "Investment order was not found.";
                return false;
            }

            orderKey = existingOrder.IdempotencyKey;
            return true;
        }

        orderKey = $"{context.IdempotencyKey}:order";
        existingOrder = orderService.FindByIdempotencyKey(orderKey);
        return true;
    }

    private OrderSubmitResult RecordOrderFill(
        TriggerContext context,
        TradeAssetRequest request,
        Guid accountId,
        Guid allocationId,
        string orderKey,
        Order? existingOrder,
        OrderSide side,
        BrokerOrderExecution execution)
    {
        if (existingOrder is not null || (request.OrderId is { } id && id != Guid.Empty))
        {
            var orderId = existingOrder?.Id ?? request.OrderId!.Value;
            return orderService.TryFill(
                orderId,
                execution.AverageFillPrice,
                execution.ExternalOrderId,
                execution.Provider,
                execution.FilledUtc);
        }

        return orderService.TrySubmit(
            orderKey,
            accountId,
            context.TriggerId,
            allocationId,
            request.AssetSymbol,
            side,
            request.Quantity,
            limitPrice: null,
            fillPrice: execution.AverageFillPrice,
            externalOrderId: execution.ExternalOrderId,
            provider: execution.Provider,
            filledUtc: execution.FilledUtc);
    }

    private bool TryReconcileExecutableCash(Guid accountId, string currency, TriggerContext context)
    {
        decimal settled;
        if (customerDirectory.FindTradingAccount(accountId) is { } tradingAccount)
        {
            settled = tradingAccount.Settled;
        }
        else if (customerDirectory.FindInvestmentAccount(accountId) is { } investmentAccount)
        {
            settled = investmentAccount.Settled;
        }
        else
        {
            return true;
        }

        var gap = settled - cashService.GetSettled(accountId, currency);
        if (gap <= 0)
        {
            return true;
        }

        return TrySyncCashDeposit(
            accountId,
            currency,
            gap,
            context,
            idempotencySuffix: $"cash-reconcile:{settled:0.####}");
    }

    private bool TrySyncCashDeposit(
        Guid tradingAccountId,
        string currency,
        decimal amount,
        TriggerContext context,
        string? idempotencySuffix = null)
    {
        var lockResult = cashService.TryAcquireLock(
            tradingAccountId,
            currency,
            context.TriggerId,
            context.AllocationRequestId ?? context.RootWorkflowId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            return false;
        }

        try
        {
            var deposit = cashService.TryDeposit(
                $"{context.IdempotencyKey}:{idempotencySuffix ?? "cash-sync-deposit"}",
                tradingAccountId,
                currency,
                amount,
                context.TriggerId);
            return deposit.Succeeded;
        }
        finally
        {
            cashService.TryReleaseLock(tradingAccountId, currency, context.TriggerId);
        }
    }

    private bool TrySyncCashWithdraw(Guid tradingAccountId, string currency, decimal amount, TriggerContext context)
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
            return false;
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
                return false;
            }

            var consume = cashService.TryConsumeReservation(reserveKey, context.TriggerId);
            return consume.Succeeded;
        }
        finally
        {
            cashService.TryReleaseLock(tradingAccountId, currency, context.TriggerId);
        }
    }

    private void SyncTradingAccountDebit(Guid accountId, decimal amount, TriggerContext context)
    {
        if (customerDirectory.FindTradingAccount(accountId) is not null)
        {
            customerDirectory.TryDebitTradingAccount(
                accountId,
                amount,
                context.TriggerId,
                $"{context.IdempotencyKey}:trading-debit");
            return;
        }

        if (customerDirectory.FindInvestmentAccount(accountId) is not null)
        {
            customerDirectory.TryDebitInvestmentAccount(
                accountId,
                amount,
                context.TriggerId,
                $"{context.IdempotencyKey}:investment-debit");
        }
    }

    private void SyncTradingAccountCredit(Guid accountId, decimal amount, TriggerContext context)
    {
        if (customerDirectory.FindTradingAccount(accountId) is not null)
        {
            customerDirectory.TryCreditTradingAccount(
                accountId,
                amount,
                context.TriggerId,
                $"{context.IdempotencyKey}:trading-credit-sell");
            return;
        }

        if (customerDirectory.FindInvestmentAccount(accountId) is not null)
        {
            customerDirectory.TryCreditInvestmentAccount(
                accountId,
                amount,
                context.TriggerId,
                $"{context.IdempotencyKey}:investment-credit-sell");
        }
    }

    private static void Validate(TradeAssetRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.AssetSymbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Currency);
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Quantity must be positive.");
        }
    }
}
