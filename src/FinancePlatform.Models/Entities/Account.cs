namespace FinancePlatform.Models.Entities;

public sealed class Account
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public required string AccountNumber { get; set; }

    public required string Currency { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedUtc { get; set; }
}
