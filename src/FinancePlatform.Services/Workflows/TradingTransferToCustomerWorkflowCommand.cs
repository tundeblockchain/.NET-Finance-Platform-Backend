namespace FinancePlatform.Services.Workflows;

/// <summary>
/// Enqueues Trading.TransferToCustomer (7003) → Customer.ReceiveMoney (6003).
/// </summary>
public sealed class TradingTransferToCustomerWorkflowCommand
{
    public required int CustomerId { get; init; }

    public required Guid TradingAccountId { get; init; }

    public required Guid CustomerAccountId { get; init; }

    public required decimal Amount { get; init; }

    public string Currency { get; init; } = "GBP";

    public required string IdempotencyKey { get; init; }

    public Guid? RootWorkflowId { get; init; }
}
