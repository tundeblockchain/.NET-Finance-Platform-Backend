using System.Collections.Concurrent;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Allocation;

public sealed class AllocationService : IAllocationService
{
    private readonly ConcurrentDictionary<Guid, AllocationRequest> _requests = new();

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

        return _requests.GetOrAdd(allocationRequestId, id =>
        {
            var now = DateTimeOffset.UtcNow;
            return new AllocationRequest
            {
                Id = id,
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
            };
        });
    }

    public AllocationRequest? Find(Guid allocationRequestId) =>
        _requests.TryGetValue(allocationRequestId, out var request) ? request : null;

    public void MarkProcessing(Guid allocationRequestId)
    {
        if (_requests.TryGetValue(allocationRequestId, out var request))
        {
            request.Status = AllocationRequestStatus.Processing;
            request.DateModified = DateTimeOffset.UtcNow;
            request.ChangedBy = ChangeActors.Broker;
        }
    }

    public void MarkCompleted(Guid allocationRequestId)
    {
        if (_requests.TryGetValue(allocationRequestId, out var request))
        {
            request.Status = AllocationRequestStatus.Completed;
            request.CompletedUtc = DateTimeOffset.UtcNow;
            request.DateModified = request.CompletedUtc.Value;
            request.ChangedBy = ChangeActors.Broker;
        }
    }
}
