using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Entities;

/// <summary>
/// Immutable price observation captured at quote or trade-fill time.
/// Used for portfolio valuation (last known mark per symbol).
/// </summary>
public sealed class AssetPrice : IAuditableEntity
{
    public Guid Id { get; set; }

    public required string AssetSymbol { get; set; }

    public decimal Price { get; set; }

    public required string Currency { get; set; }

    public AssetPriceSource Source { get; set; }

    public required string Provider { get; set; }

    public Guid? OrderId { get; set; }

    public string? ExternalOrderId { get; set; }

    public DateTimeOffset ObservedUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
