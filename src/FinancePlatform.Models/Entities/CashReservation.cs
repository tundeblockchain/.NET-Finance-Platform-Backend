namespace FinancePlatform.Models.Entities;

public sealed class CashReservation
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid AllocationRequestId { get; set; }

    public Guid TriggerId { get; set; }

    public required string Currency { get; set; }

    public decimal Amount { get; set; }

    public required string IdempotencyKey { get; set; }

    public bool IsReleased { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? ReleasedUtc { get; set; }
}
