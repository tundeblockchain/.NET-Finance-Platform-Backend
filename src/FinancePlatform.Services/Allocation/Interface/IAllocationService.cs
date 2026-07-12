using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Allocation;

public interface IAllocationService
{
    /// <summary>
    /// Creates or returns an in-flight allocation request for the workflow.
    /// </summary>
    AllocationRequest EnsureStarted(
        Guid allocationRequestId,
        Guid accountId,
        Guid? customerId,
        decimal amount,
        string currency,
        Guid rootWorkflowId,
        string idempotencyKey);

    AllocationRequest? Find(Guid allocationRequestId);

    void MarkProcessing(Guid allocationRequestId);

    void MarkCompleted(Guid allocationRequestId);
}
