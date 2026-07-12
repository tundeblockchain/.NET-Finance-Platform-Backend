namespace FinancePlatform.Models.Components;

/// <summary>
/// Follow-on trigger the EP should raise after a successful service call.
/// </summary>
public sealed class NextTriggerSpec
{
    public required int TriggerCode { get; init; }

    public required string QueueName { get; init; }

    public required string TargetComponent { get; init; }

    public required string PayloadJson { get; init; }

    public required string IdempotencyKey { get; init; }
}
