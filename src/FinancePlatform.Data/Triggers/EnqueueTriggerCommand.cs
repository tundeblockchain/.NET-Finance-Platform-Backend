using FinancePlatform.Models.Enums;

namespace FinancePlatform.Data.Triggers;

public sealed class EnqueueTriggerCommand
{
    public required int TriggerCode { get; init; }

    public required string QueueName { get; init; }

    public required string PayloadJson { get; init; }

    public required Guid RootWorkflowId { get; init; }

    public required Guid CorrelationId { get; init; }

    public Guid? ParentTriggerId { get; init; }

    public Guid? SourceTriggerId { get; init; }

    public Guid? AllocationRequestId { get; init; }

    public Guid? ExternalId { get; init; }

    public ExternalEntityType? ExternalType { get; init; }

    public required string SourceComponent { get; init; }

    public required string TargetComponent { get; init; }

    public required string IdempotencyKey { get; init; }

    public DateTimeOffset? NextAttemptUtc { get; init; }
}
