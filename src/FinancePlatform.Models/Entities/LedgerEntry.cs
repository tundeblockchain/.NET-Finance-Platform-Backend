using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Entities;

public sealed class LedgerEntry : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid? TriggerId { get; set; }

    public Guid? AllocationRequestId { get; set; }

    public LedgerEntryType EntryType { get; set; }

    public decimal Amount { get; set; }

    public required string Currency { get; set; }

    public required string IdempotencyKey { get; set; }

    public required string Description { get; set; }

    public DateTimeOffset PostedUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
