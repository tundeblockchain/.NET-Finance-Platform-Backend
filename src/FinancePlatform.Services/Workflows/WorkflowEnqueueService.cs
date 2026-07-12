using System.Text.Json;
using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
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
            ExternalType = ExternalEntityType.Account,
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
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Trading",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }

    public Task<SystemEventTrigger> EnqueueAllocationAsync(
        AllocationWorkflowCommand command,
        CancellationToken cancellationToken = default)
    {
        var rootId = command.RootWorkflowId ?? Guid.NewGuid();
        var allocationId = command.AllocationRequestId ?? rootId;
        var payload = JsonSerializer.Serialize(new
        {
            Amount = command.Amount,
            CashAmount = command.Amount,
            command.Currency,
            command.AssetSymbol,
            command.Quantity
        });

        return claimService.EnqueueAsync(new EnqueueTriggerCommand
        {
            TriggerCode = TriggerCodes.CustomerDistributeMoney,
            QueueName = QueueNames.Customer,
            PayloadJson = payload,
            RootWorkflowId = rootId,
            CorrelationId = rootId,
            AllocationRequestId = allocationId,
            ExternalId = command.AccountId,
            ExternalType = ExternalEntityType.Account,
            SourceComponent = "Api",
            TargetComponent = "Customer",
            IdempotencyKey = command.IdempotencyKey
        }, cancellationToken);
    }
}
