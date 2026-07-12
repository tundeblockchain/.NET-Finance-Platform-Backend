namespace FinancePlatform.Services.Workflows;

public sealed class CustomerDepositWorkflowCommand
{
    public required int CustomerId { get; init; }

    public required Guid CustomerAccountId { get; init; }

    public required decimal Amount { get; init; }

    public string Currency { get; init; } = "GBP";

    public required string IdempotencyKey { get; init; }

    public Guid? RootWorkflowId { get; init; }
}
