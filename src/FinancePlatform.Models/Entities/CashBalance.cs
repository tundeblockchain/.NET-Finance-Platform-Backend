namespace FinancePlatform.Models.Entities;

public sealed class CashBalance : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public required string Currency { get; set; }

    public decimal Settled { get; set; }

    public decimal Reserved { get; set; }

    public decimal Available => Settled - Reserved;

    public bool IsLocked { get; set; }

    public Guid? LockedByAllocationId { get; set; }

    public Guid? LockedByTriggerId { get; set; }

    public DateTimeOffset? LockAcquiredUtc { get; set; }

    public DateTimeOffset? LockExpiresUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
