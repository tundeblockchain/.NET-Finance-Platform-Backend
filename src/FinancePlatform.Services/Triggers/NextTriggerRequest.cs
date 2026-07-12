namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Description of a subsequent trigger to enqueue after the current handler succeeds.
/// </summary>
public sealed class NextTriggerRequest
{
    public required int TriggerCode { get; init; }

    public required string QueueName { get; init; }

    public required string TargetComponent { get; init; }

    public required string PayloadJson { get; init; }

    public required string IdempotencyKey { get; init; }
}
