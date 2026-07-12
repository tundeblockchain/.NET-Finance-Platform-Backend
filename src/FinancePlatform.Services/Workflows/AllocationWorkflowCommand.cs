namespace FinancePlatform.Services.Workflows;

public sealed class AllocationWorkflowCommand
{
    public required Guid AccountId { get; init; }

    public required decimal Amount { get; init; }

    public string Currency { get; init; } = "GBP";

    public string AssetSymbol { get; init; } = "VWRL";

    public decimal Quantity { get; init; } = 1m;

    public required string IdempotencyKey { get; init; }

    public Guid? RootWorkflowId { get; init; }

    public Guid? AllocationRequestId { get; init; }
}
