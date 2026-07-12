using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Services.Workflows;

public sealed class WorkflowEnqueueService(TriggerClaimService claimService) : IWorkflowEnqueueService
{
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

    public Task<SystemEventTrigger> EnqueueBuyAsync(
        BuyWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new
        {
            command.AssetSymbol,
            command.Quantity,
            command.Currency,
            command.CashAmount
        });

        return claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.BuyAsset,
            QueueName = QueueNames.Trading,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            AllocationRequestId = command.AllocationRequestId,
            ExternalId = command.AccountId,
            ExternalType = command.ExternalType,
            SourceComponent = "Api",
            TargetComponent = "Trading",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }

    public Task<SystemEventTrigger> EnqueueSellAsync(
        SellWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var payload = JsonSerializer.Serialize(new
        {
            command.AssetSymbol,
            command.Quantity,
            command.Currency,
            command.CashAmount
        });

        return claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.SellAsset,
            QueueName = QueueNames.Trading,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            AllocationRequestId = command.AllocationRequestId,
            ExternalId = command.AccountId,
            ExternalType = command.ExternalType,
            SourceComponent = "Api",
            TargetComponent = "Trading",
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
}
