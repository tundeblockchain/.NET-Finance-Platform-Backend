namespace FinancePlatform.Models.Entities;

public sealed class SystemEventWorking
{
    public Guid TriggerId { get; set; }

    public required string WorkerInstanceId { get; set; }

    public required string QueueName { get; set; }

    public DateTimeOffset ClaimedUtc { get; set; }

    public DateTimeOffset HeartbeatUtc { get; set; }

    public DateTimeOffset LeaseExpiresUtc { get; set; }
}
