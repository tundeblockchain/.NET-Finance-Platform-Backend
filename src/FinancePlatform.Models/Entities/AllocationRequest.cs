using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Entities;

public sealed class AllocationRequest : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid AccountId { get; set; }

    public required string IdempotencyKey { get; set; }

    public AllocationRequestStatus Status { get; set; } = AllocationRequestStatus.Created;

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public Guid RootWorkflowId { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? CompletedUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
