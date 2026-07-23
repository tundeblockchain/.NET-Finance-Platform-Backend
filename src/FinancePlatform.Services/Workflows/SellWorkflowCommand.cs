using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Workflows;

public sealed class SellWorkflowCommand
{
    /// <summary>Customer id; when 0, resolved from the trading account.</summary>
    public int CustomerId { get; init; }

    /// <summary>Trading account whose investment account holds the position.</summary>
    public required Guid AccountId { get; init; }

    public required string AssetSymbol { get; init; }

    public required decimal Quantity { get; init; }

    public string Currency { get; init; } = "GBP";

    public required string IdempotencyKey { get; init; }

    public Guid? RootWorkflowId { get; init; }

    public Guid? AllocationRequestId { get; init; }

    public ExternalEntityType ExternalType { get; init; } = ExternalEntityType.TradingAccount;
}
