namespace FinancePlatform.Models.Entities;

public sealed class Account : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public required string AccountNumber { get; set; }

    public required string Currency { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
