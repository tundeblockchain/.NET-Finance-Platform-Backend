using FinancePlatform.Data.DataLayer;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Allocation;

/// <summary>
/// SQL-backed allocation request store.
/// </summary>
public sealed class SqlAllocationService(IAllocationRequestRepository allocationRepository) : IAllocationService
{
    public AllocationRequest EnsureStarted(
        Guid allocationRequestId,
        Guid accountId,
        Guid? customerId,
        decimal amount,
        string currency,
        Guid rootWorkflowId,
        string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var existing = allocationRepository.GetAsync(allocationRequestId).GetAwaiter().GetResult();
        if (existing is not null)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        return allocationRepository
            .UpsertAsync(new AllocationRequest
            {
                Id = allocationRequestId,
                CustomerId = customerId ?? Guid.Empty,
                AccountId = accountId,
                IdempotencyKey = idempotencyKey,
                Status = AllocationRequestStatus.Processing,
                Amount = amount,
                Currency = currency.ToUpperInvariant(),
                RootWorkflowId = rootWorkflowId,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.Broker
            })
            .GetAwaiter()
            .GetResult();
    }

    public AllocationRequest? Find(Guid allocationRequestId) =>
        allocationRepository.GetAsync(allocationRequestId).GetAwaiter().GetResult();

    public void MarkProcessing(Guid allocationRequestId)
    {
        var request = allocationRepository.GetAsync(allocationRequestId).GetAwaiter().GetResult();
        if (request is null)
        {
            return;
        }

        request.Status = AllocationRequestStatus.Processing;
        request.DateModified = DateTimeOffset.UtcNow;
        request.ChangedBy = ChangeActors.Broker;
        allocationRepository.UpsertAsync(request).GetAwaiter().GetResult();
    }

    public void MarkCompleted(Guid allocationRequestId)
    {
        var request = allocationRepository.GetAsync(allocationRequestId).GetAwaiter().GetResult();
        if (request is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        request.Status = AllocationRequestStatus.Completed;
        request.CompletedUtc = now;
        request.DateModified = now;
        request.ChangedBy = ChangeActors.Broker;
        allocationRepository.UpsertAsync(request).GetAwaiter().GetResult();
    }
}
