using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Triggers;

namespace FinancePlatform.Services.Workflows;

public interface IWorkflowEnqueueService
{
    Task<SystemEventTrigger> EnqueueDepositAsync(DepositWorkflowCommand command, CancellationToken cancellationToken = default);

    Task<SystemEventTrigger> EnqueueBuyAsync(BuyWorkflowCommand command, CancellationToken cancellationToken = default);

    Task<SystemEventTrigger> EnqueueSellAsync(SellWorkflowCommand command, CancellationToken cancellationToken = default);

    Task<SystemEventTrigger> EnqueueAllocationAsync(AllocationWorkflowCommand command, CancellationToken cancellationToken = default);
}
