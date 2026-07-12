namespace FinancePlatform.Services.Workflows;

/// <summary>
/// Enqueues Customer.DistributeMoney (6002) → Trading.ReceiveMoney (7001 park-only).
/// </summary>
public sealed class CustomerDistributeWorkflowCommand
{
    public required int CustomerId { get; init; }

    public required Guid CustomerAccountId { get; init; }

    public Guid TradingAccountId { get; init; }

    public required decimal Amount { get; init; }

    public string Currency { get; init; } = "GBP";

    public required string IdempotencyKey { get; init; }

    public Guid? RootWorkflowId { get; init; }

    public Guid? AllocationRequestId { get; init; }
}
