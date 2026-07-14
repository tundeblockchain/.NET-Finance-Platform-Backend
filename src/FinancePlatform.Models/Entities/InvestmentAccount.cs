namespace FinancePlatform.Models.Entities;

/// <summary>
/// Investment-component cash account. Trading distributes buy proceeds here before asset execution.
/// </summary>
public sealed class InvestmentAccount : IAuditableEntity
{
    public Guid Id { get; set; }

    public int CustomerId { get; set; }

    public Guid TradingAccountId { get; set; }

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
