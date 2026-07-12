namespace FinancePlatform.Models.Entities;

/// <summary>
/// Currently claimed trigger lease. No archive table.
/// </summary>
public sealed class SystemEventWorking : IAuditableEntity
{
    public Guid TriggerId { get; set; }

    public required string WorkerInstanceId { get; set; }

    public required string QueueName { get; set; }

    public DateTimeOffset ClaimedUtc { get; set; }

    public DateTimeOffset HeartbeatUtc { get; set; }

    public DateTimeOffset LeaseExpiresUtc { get; set; }

    public DateTimeOffset DateModified { get; set; }

    public string ChangedBy { get; set; } = ChangeActors.Broker;
}
