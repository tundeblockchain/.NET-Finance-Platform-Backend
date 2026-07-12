namespace FinancePlatform.Models.Entities;

/// <summary>
/// Trading-component cash account. Customer distributes park funds here; invest is a later trading action.
/// </summary>
public sealed class TradingAccount : IAuditableEntity
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
