using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Entities;

public sealed class Order : IAuditableEntity
{
    public Guid Id { get; set; }

    public Guid AccountId { get; set; }

    public Guid? AllocationRequestId { get; set; }

    public Guid TriggerId { get; set; }

    public required string AssetSymbol { get; set; }

    public OrderSide Side { get; set; }

    public decimal Quantity { get; set; }

    public decimal? LimitPrice { get; set; }

    public decimal? FillPrice { get; set; }

    public string? ExternalOrderId { get; set; }

    public string? Provider { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Created;

    public required string IdempotencyKey { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? SubmittedUtc { get; set; }

    public DateTimeOffset? FilledUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.System;
}
