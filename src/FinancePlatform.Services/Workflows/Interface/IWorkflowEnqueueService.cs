using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Workflows;

public interface IWorkflowEnqueueService
{
    Task<SystemEventTrigger> EnqueueDepositAsync(DepositWorkflowCommand command, CancellationToken cancellationToken = default);

    Task<SystemEventTrigger> EnqueueBuyAsync(BuyWorkflowCommand command, CancellationToken cancellationToken = default);

    Task<SystemEventTrigger> EnqueueSellAsync(SellWorkflowCommand command, CancellationToken cancellationToken = default);

    Task<SystemEventTrigger> EnqueueCustomerDepositAsync(
        CustomerDepositWorkflowCommand command,
        CancellationToken cancellationToken = default);

    Task<SystemEventTrigger> EnqueueCustomerDistributeAsync(
        CustomerDistributeWorkflowCommand command,
        CancellationToken cancellationToken = default);

    Task<SystemEventTrigger> EnqueueTradingTransferToCustomerAsync(
        TradingTransferToCustomerWorkflowCommand command,
        CancellationToken cancellationToken = default);

    [Obsolete("Use EnqueueCustomerDistributeAsync for park-only Customer → Trading.")]
    Task<SystemEventTrigger> EnqueueAllocationAsync(AllocationWorkflowCommand command, CancellationToken cancellationToken = default);
}
