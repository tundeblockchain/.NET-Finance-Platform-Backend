using FinancePlatform.Models.Enums;

namespace FinancePlatform.Models.Entities;

public sealed class SystemEventTrigger
{
    public Guid Id { get; set; }

    public int TriggerCode { get; set; }

    public required string QueueName { get; set; }

    public TriggerStatus Status { get; set; } = TriggerStatus.Pending;

    public required string PayloadJson { get; set; }

    public string? ResultJson { get; set; }

    public Guid RootWorkflowId { get; set; }

    public Guid CorrelationId { get; set; }

    public Guid? ParentTriggerId { get; set; }

    public Guid? SourceTriggerId { get; set; }

    public Guid? AllocationRequestId { get; set; }

    public Guid? ExternalId { get; set; }

    public ExternalEntityType? ExternalType { get; set; }

    public required string SourceComponent { get; set; }

    public required string TargetComponent { get; set; }

    public required string IdempotencyKey { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? NextAttemptUtc { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset? CompletedUtc { get; set; }
}
