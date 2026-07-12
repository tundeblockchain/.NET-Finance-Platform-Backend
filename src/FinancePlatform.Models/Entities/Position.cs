namespace FinancePlatform.Models.Entities;

public sealed class Position : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public required string AssetSymbol { get; set; }

    public decimal Quantity { get; set; }

    public decimal AverageCost { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
