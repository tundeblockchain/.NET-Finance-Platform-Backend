using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.Triggers;

/// <summary>
/// Persistence port for durable triggers.
/// </summary>
public interface ITriggerStore
{
    Task<SystemEventTrigger> EnqueueAsync(EnqueueTriggerCommand command, CancellationToken cancellationToken = default);

    Task<ClaimedTrigger?> TryClaimAsync(
        string queueName,
        string workerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(Guid triggerId, string? resultJson, CancellationToken cancellationToken = default);

    Task RetryAsync(Guid triggerId, string error, DateTimeOffset nextAttemptUtc, CancellationToken cancellationToken = default);

    Task FailAsync(Guid triggerId, string error, CancellationToken cancellationToken = default);

    Task MarkCompensationAsync(Guid triggerId, string error, CancellationToken cancellationToken = default);

    Task HeartbeatAsync(Guid triggerId, string workerInstanceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requeues triggers whose working lease has expired (Running → Pending via Retry).
    /// Safe to call concurrently; each expired lease is recovered at most once.
    /// </summary>
    Task<IReadOnlyList<RecoveredTrigger>> RecoverExpiredLeasesAsync(
        int batchSize,
        DateTimeOffset? nextAttemptUtc = null,
        CancellationToken cancellationToken = default);

    Task<SystemEventTrigger?> GetByIdAsync(Guid triggerId, CancellationToken cancellationToken = default);

    Task<SystemEventTrigger?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    IReadOnlyList<SystemEventTrigger> GetAll();

    IReadOnlyList<SystemEventWorking> GetWorking();
}
