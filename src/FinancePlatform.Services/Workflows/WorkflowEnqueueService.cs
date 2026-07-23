using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Investment;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Brokers;
using FinancePlatform.Services.Cash;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Investment;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Services.Workflows;

public sealed class WorkflowEnqueueService(
    TriggerClaimService claimService,
    ICustomerDirectory customerDirectory,
    IInvestmentInstructionStore instructionStore,
    ICashService cashService,
    IBrokerTradingProvider broker) : IWorkflowEnqueueService
{
    private static readonly TimeSpan LockLease = TimeSpan.FromSeconds(30);

    public Task<SystemEventTrigger> EnqueueDepositAsync(
        DepositWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new
        {
            command.Amount,
            command.Currency,
            command.AssetSymbol,
            command.Quantity
        });

        return claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.DepositCash,
            QueueName = QueueNames.Cash,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            ExternalId = command.AccountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Cash",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }

    public async Task<SystemEventTrigger> EnqueueBuyAsync(
        BuyWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var tradingAccount = RequireTradingAccount(command.AccountId);
        var customerId = command.CustomerId > 0 ? command.CustomerId : tradingAccount.CustomerId;
        if (tradingAccount.CustomerId != customerId)
        {
            throw new InvalidOperationException("Trading account does not belong to this customer.");
        }

        var currency = string.IsNullOrWhiteSpace(command.Currency)
            ? tradingAccount.Currency
            : command.Currency.Trim().ToUpperInvariant();

        var investmentAccount = customerDirectory.EnsureInvestmentAccount(
            customerId,
            tradingAccount.Id,
            currency);
        customerDirectory.EnsureTradingToInvestmentDistribution(
            customerId,
            tradingAccount.Id,
            investmentAccount.Id);

        var quote = await broker.GetQuoteAsync(command.AssetSymbol, referencePrice: null, cancellationToken);
        var unitPrice = quote.Ask > 0 ? quote.Ask : quote.Mid;
        var estimatedCash = RoundMoney(unitPrice * command.Quantity);
        if (estimatedCash <= 0)
        {
            throw new InvalidOperationException("Unable to estimate cash required for buy.");
        }

        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var instructionKey = $"{command.IdempotencyKey}:instruction";
        var existing = instructionStore.GetByIdempotencyKey(instructionKey);

        if (existing is null)
        {
            var cashAvailable = cashService.GetAvailable(tradingAccount.Id, currency);
            if (cashAvailable < estimatedCash)
            {
                throw new InvalidOperationException(
                    $"Insufficient parked trading cash. Available={cashAvailable}, required={estimatedCash}.");
            }
        }

        // Idempotent debit/withdraw so retries after partial failure are safe.
        MoveParkedCashToInvestment(
            tradingAccount.Id,
            currency,
            estimatedCash,
            command.IdempotencyKey,
            rootId);

        InvestmentInstruction instruction;
        if (existing is not null)
        {
            instruction = existing;
        }
        else
        {
            var created = instructionStore.TryCreate(new InvestmentInstruction
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                TradingAccountId = tradingAccount.Id,
                InvestmentAccountId = investmentAccount.Id,
                AssetSymbol = command.AssetSymbol.Trim().ToUpperInvariant(),
                Quantity = command.Quantity,
                CashAmount = estimatedCash,
                Currency = currency,
                Side = OrderSide.Buy,
                Status = InvestmentInstructionStatus.Pending,
                IdempotencyKey = instructionKey,
                CreatedUtc = DateTimeOffset.UtcNow,
                DateModified = DateTimeOffset.UtcNow,
                ChangedBy = ChangeActors.System
            });

            if (!created.Succeeded || created.Instruction is null)
            {
                throw new InvalidOperationException(created.Error ?? "Unable to create investment instruction.");
            }

            instruction = created.Instruction;
        }

        var payload = JsonSerializer.Serialize(new InvestMoneyRequest
        {
            InstructionId = instruction.Id,
            CustomerId = customerId,
            TradingAccountId = tradingAccount.Id,
            InvestmentAccountId = investmentAccount.Id,
            Amount = instruction.CashAmount,
            CashAmount = instruction.CashAmount,
            Currency = currency,
            AssetSymbol = instruction.AssetSymbol,
            Quantity = instruction.Quantity
        });

        return await claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.InvestmentReceiveMoney,
            QueueName = QueueNames.Investment,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            AllocationRequestId = command.AllocationRequestId ?? rootId,
            ExternalId = investmentAccount.Id,
            ExternalType = ExternalEntityType.InvestmentAccount,
            SourceComponent = "Api",
            TargetComponent = "Investment",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }

    public async Task<SystemEventTrigger> EnqueueSellAsync(
        SellWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var tradingAccount = RequireTradingAccount(command.AccountId);
        var customerId = command.CustomerId > 0 ? command.CustomerId : tradingAccount.CustomerId;
        if (tradingAccount.CustomerId != customerId)
        {
            throw new InvalidOperationException("Trading account does not belong to this customer.");
        }

        var currency = string.IsNullOrWhiteSpace(command.Currency)
            ? tradingAccount.Currency
            : command.Currency.Trim().ToUpperInvariant();

        var investmentAccount = customerDirectory.EnsureInvestmentAccount(
            customerId,
            tradingAccount.Id,
            currency);
        customerDirectory.EnsureTradingToInvestmentDistribution(
            customerId,
            tradingAccount.Id,
            investmentAccount.Id);

        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var instructionKey = $"{command.IdempotencyKey}:instruction";
        var existing = instructionStore.GetByIdempotencyKey(instructionKey);
        InvestmentInstruction instruction;
        if (existing is not null)
        {
            instruction = existing;
        }
        else
        {
            var created = instructionStore.TryCreate(new InvestmentInstruction
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                TradingAccountId = tradingAccount.Id,
                InvestmentAccountId = investmentAccount.Id,
                AssetSymbol = command.AssetSymbol.Trim().ToUpperInvariant(),
                Quantity = command.Quantity,
                CashAmount = 0m,
                Currency = currency,
                Side = OrderSide.Sell,
                Status = InvestmentInstructionStatus.Pending,
                IdempotencyKey = instructionKey,
                CreatedUtc = DateTimeOffset.UtcNow,
                DateModified = DateTimeOffset.UtcNow,
                ChangedBy = ChangeActors.System
            });

            if (!created.Succeeded || created.Instruction is null)
            {
                throw new InvalidOperationException(created.Error ?? "Unable to create investment instruction.");
            }

            instruction = created.Instruction;
        }

        var payload = JsonSerializer.Serialize(new InvestMoneyRequest
        {
            InstructionId = instruction.Id,
            CustomerId = customerId,
            TradingAccountId = tradingAccount.Id,
            InvestmentAccountId = investmentAccount.Id,
            Amount = 0m,
            CashAmount = 0m,
            Currency = currency,
            AssetSymbol = instruction.AssetSymbol,
            Quantity = instruction.Quantity
        });

        return await claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.InvestmentInvestMoney,
            QueueName = QueueNames.Investment,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            AllocationRequestId = command.AllocationRequestId ?? rootId,
            ExternalId = investmentAccount.Id,
            ExternalType = ExternalEntityType.InvestmentAccount,
            SourceComponent = "Api",
            TargetComponent = "Investment",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }

    public Task<SystemEventTrigger> EnqueueCustomerDepositAsync(
        CustomerDepositWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new CustomerDepositRequest
        {
            CustomerId = command.CustomerId,
            CustomerAccountId = command.CustomerAccountId,
            Amount = command.Amount,
            Currency = command.Currency
        });

        return claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.CustomerDepositMoney,
            QueueName = QueueNames.Customer,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            ExternalId = command.CustomerAccountId,
            ExternalType = ExternalEntityType.CustomerAccount,
            SourceComponent = "Api",
            TargetComponent = "Customer",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }

    public Task<SystemEventTrigger> EnqueueCustomerDistributeAsync(
        CustomerDistributeWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var allocationId = command.AllocationRequestId ?? rootId;
        var payload = JsonSerializer.Serialize(new DistributeMoneyRequest
        {
            CustomerId = command.CustomerId,
            CustomerAccountId = command.CustomerAccountId,
            TradingAccountId = command.TradingAccountId,
            Amount = command.Amount,
            CashAmount = command.Amount,
            Currency = command.Currency
        });

        return claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.CustomerDistributeMoney,
            QueueName = QueueNames.Customer,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            AllocationRequestId = allocationId,
            ExternalId = command.CustomerAccountId,
            ExternalType = ExternalEntityType.CustomerAccount,
            SourceComponent = "Api",
            TargetComponent = "Customer",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }

    public Task<SystemEventTrigger> EnqueueTradingTransferToCustomerAsync(
        TradingTransferToCustomerWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new TradingTransferToCustomerRequest
        {
            CustomerId = command.CustomerId,
            TradingAccountId = command.TradingAccountId,
            CustomerAccountId = command.CustomerAccountId,
            Amount = command.Amount,
            Currency = command.Currency
        });

        return claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.TradingTransferToCustomer,
            QueueName = QueueNames.Trading,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            ExternalId = command.TradingAccountId,
            ExternalType = ExternalEntityType.TradingAccount,
            SourceComponent = "Api",
            TargetComponent = "Trading",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }

    [Obsolete("Use EnqueueCustomerDistributeAsync for park-only Customer → Trading.")]
    public Task<SystemEventTrigger> EnqueueAllocationAsync(
        AllocationWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        return EnqueueCustomerDistributeAsync(new CustomerDistributeWorkflowCommand
        {
            CustomerId = 0,
            CustomerAccountId = command.AccountId,
            Amount = command.Amount,
            Currency = command.Currency,
            IdempotencyKey = command.IdempotencyKey,
            RootWorkflowId = command.RootWorkflowId,
            AllocationRequestId = command.AllocationRequestId
        }, cancellationToken);
    }

    private TradingAccount RequireTradingAccount(Guid tradingAccountId)
    {
        var tradingAccount = customerDirectory.FindTradingAccount(tradingAccountId);
        if (tradingAccount is null)
        {
            throw new InvalidOperationException("Trading account was not found.");
        }

        return tradingAccount;
    }

    private void MoveParkedCashToInvestment(
        Guid tradingAccountId,
        string currency,
        decimal amount,
        string idempotencyKey,
        Guid allocationId)
    {
        var triggerId = Guid.NewGuid();
        var debited = customerDirectory.TryDebitTradingAccount(
            tradingAccountId,
            amount,
            triggerId,
            $"{idempotencyKey}:trading-invest-debit");

        if (!debited)
        {
            throw new InvalidOperationException("Unable to debit parked trading cash for investment.");
        }

        var lockResult = cashService.TryAcquireLock(
            tradingAccountId,
            currency,
            triggerId,
            allocationId,
            LockLease);

        if (!lockResult.IsHeld)
        {
            customerDirectory.TryCreditTradingAccount(
                tradingAccountId,
                amount,
                triggerId,
                $"{idempotencyKey}:trading-invest-debit-rollback");
            throw new InvalidOperationException("Unable to lock trading cash ledger for investment transfer.");
        }

        try
        {
            var reserveKey = $"{idempotencyKey}:trading-invest-withdraw";
            var reserve = cashService.TryReserve(
                reserveKey,
                tradingAccountId,
                currency,
                amount,
                triggerId,
                allocationId);
            if (!reserve.Succeeded)
            {
                customerDirectory.TryCreditTradingAccount(
                    tradingAccountId,
                    amount,
                    triggerId,
                    $"{idempotencyKey}:trading-invest-debit-rollback");
                throw new InvalidOperationException(
                    reserve.Error ?? "Unable to reserve trading cash for investment transfer.");
            }

            var consume = cashService.TryConsumeReservation(reserveKey, triggerId);
            if (!consume.Succeeded && !consume.AlreadyApplied)
            {
                cashService.TryReleaseReservation(reserveKey, triggerId);
                customerDirectory.TryCreditTradingAccount(
                    tradingAccountId,
                    amount,
                    triggerId,
                    $"{idempotencyKey}:trading-invest-debit-rollback");
                throw new InvalidOperationException(
                    consume.Error ?? "Unable to withdraw trading cash for investment transfer.");
            }
        }
        finally
        {
            cashService.TryReleaseLock(tradingAccountId, currency, triggerId);
        }
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
