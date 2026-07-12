namespace FinancePlatform.Models.Entities;

/// <summary>
/// Customer-component cash account. Deposits land here before any distribute to trading.
/// </summary>
public sealed class CustomerAccount : IAuditableEntity
{
    public Guid Id { get; set; }

    public int CustomerId { get; set; }

    public required string Currency { get; set; }

    public decimal Settled { get; set; }

    public decimal Reserved { get; set; }

    public decimal Available => Settled - Reserved;

    public bool IsLocked { get; set; }

    public Guid? LockedByTriggerId { get; set; }

    public DateTimeOffset? LockExpiresUtc { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
