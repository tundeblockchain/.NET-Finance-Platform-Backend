namespace FinancePlatform.Models.Entities;

public sealed class Position
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public required string AssetSymbol { get; set; }

    public decimal Quantity { get; set; }

    public decimal AverageCost { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}
