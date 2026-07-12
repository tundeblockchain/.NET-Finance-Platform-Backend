using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;

namespace FinancePlatform.Data.Triggers;

/// <summary>
/// Thread-safe in-memory trigger store.
/// </summary>
public sealed class InMemoryTriggerStore(TimeProvider? timeProvider = null) : ITriggerStore
{
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;
    private readonly object _gate = new();
    private readonly Dictionary<Guid, SystemEventTrigger> _triggers = new();
    private readonly Dictionary<Guid, SystemEventWorking> _working = new();
    private readonly Dictionary<string, Guid> _idempotencyIndex = new(StringComparer.Ordinal);

    private DateTimeOffset UtcNow => _clock.GetUtcNow();

    public Task<SystemEventTrigger> EnqueueAsync(EnqueueTriggerCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_idempotencyIndex.TryGetValue(command.IdempotencyKey, out var existingId)
                && _triggers.TryGetValue(existingId, out var existing))
            {
                return Task.FromResult(Clone(existing));
            }

            var now = UtcNow;
            var trigger = new SystemEventTrigger
            {
                Id = Guid.NewGuid(),
                TriggerCode = command.TriggerCode,
                QueueName = command.QueueName,
                Status = TriggerStatus.Pending,
                PayloadJson = command.PayloadJson,
                RootWorkflowId = command.RootWorkflowId,
                CorrelationId = command.CorrelationId,
                ParentTriggerId = command.ParentTriggerId,
                SourceTriggerId = command.SourceTriggerId,
                AllocationRequestId = command.AllocationRequestId,
                ExternalId = command.ExternalId,
                ExternalType = command.ExternalType,
                SourceComponent = command.SourceComponent,
                TargetComponent = command.TargetComponent,
                IdempotencyKey = command.IdempotencyKey,
                AttemptCount = 0,
                NextAttemptUtc = command.NextAttemptUtc ?? now,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.Broker
            };

            _triggers[trigger.Id] = trigger;
            _idempotencyIndex[command.IdempotencyKey] = trigger.Id;
            return Task.FromResult(Clone(trigger));
        }
    }

    public Task<ClaimedTrigger?> TryClaimAsync(
        string queueName,
        string workerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);
        ArgumentException.ThrowIfNullOrWhiteSpace(workerInstanceId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var now = UtcNow;
            var candidate = _triggers.Values
                .Where(t =>
                    t.QueueName == queueName
                    && t.Status == TriggerStatus.Pending
                    && (t.NextAttemptUtc is null || t.NextAttemptUtc <= now)
                    && !_working.ContainsKey(t.Id))
                .OrderBy(t => t.CreatedUtc)
                .FirstOrDefault();

            if (candidate is null)
            {
                return Task.FromResult<ClaimedTrigger?>(null);
            }

            TriggerStatusTransitions.EnsureCanTransition(candidate.Status, TriggerStatus.Claimed);
            candidate.Status = TriggerStatus.Claimed;
            TriggerStatusTransitions.EnsureCanTransition(candidate.Status, TriggerStatus.Running);
            candidate.Status = TriggerStatus.Running;
            candidate.AttemptCount += 1;
            TouchBroker(candidate, now);

            var working = new SystemEventWorking
            {
                TriggerId = candidate.Id,
                WorkerInstanceId = workerInstanceId,
                QueueName = queueName,
                ClaimedUtc = now,
                HeartbeatUtc = now,
                LeaseExpiresUtc = now.Add(leaseDuration),
                DateModified = now,
                ChangedBy = ChangeActors.Broker
            };

            _working[candidate.Id] = working;

            return Task.FromResult<ClaimedTrigger?>(new ClaimedTrigger
            {
                Trigger = Clone(candidate),
                Working = CloneWorking(working)
            });
        }
    }

    public Task CompleteAsync(Guid triggerId, string? resultJson, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var trigger = GetRequired(triggerId);
            TriggerStatusTransitions.EnsureCanTransition(trigger.Status, TriggerStatus.Completed);
            trigger.Status = TriggerStatus.Completed;
            trigger.ResultJson = resultJson;
            trigger.CompletedUtc = UtcNow;
            trigger.LastError = null;
            TouchBroker(trigger, trigger.CompletedUtc.Value);
            _working.Remove(triggerId);
        }

        return Task.CompletedTask;
    }

    public Task RetryAsync(Guid triggerId, string error, DateTimeOffset nextAttemptUtc, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var trigger = GetRequired(triggerId);
            TriggerStatusTransitions.EnsureCanTransition(trigger.Status, TriggerStatus.Retry);
            trigger.Status = TriggerStatus.Retry;
            trigger.LastError = error;
            TriggerStatusTransitions.EnsureCanTransition(trigger.Status, TriggerStatus.Pending);
            trigger.Status = TriggerStatus.Pending;
            trigger.NextAttemptUtc = nextAttemptUtc;
            TouchBroker(trigger, UtcNow);
            _working.Remove(triggerId);
        }

        return Task.CompletedTask;
    }

    public Task FailAsync(Guid triggerId, string error, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var trigger = GetRequired(triggerId);
            TriggerStatusTransitions.EnsureCanTransition(trigger.Status, TriggerStatus.Failed);
            trigger.Status = TriggerStatus.Failed;
            trigger.LastError = error;
            trigger.CompletedUtc = UtcNow;
            TouchBroker(trigger, trigger.CompletedUtc.Value);
            _working.Remove(triggerId);
        }

        return Task.CompletedTask;
    }

    public Task MarkCompensationAsync(Guid triggerId, string error, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var trigger = GetRequired(triggerId);
            TriggerStatusTransitions.EnsureCanTransition(trigger.Status, TriggerStatus.Compensation);
            trigger.Status = TriggerStatus.Compensation;
            trigger.LastError = error;
            TouchBroker(trigger, UtcNow);
            _working.Remove(triggerId);
        }

        return Task.CompletedTask;
    }

    public Task HeartbeatAsync(Guid triggerId, string workerInstanceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_working.TryGetValue(triggerId, out var working))
            {
                throw new InvalidOperationException($"Trigger {triggerId} is not in the working set.");
            }

            if (!string.Equals(working.WorkerInstanceId, workerInstanceId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Worker '{workerInstanceId}' does not own trigger {triggerId}.");
            }

            var now = UtcNow;
            working.HeartbeatUtc = now;
            working.LeaseExpiresUtc = now.Add(leaseDuration);
            working.DateModified = now;
            working.ChangedBy = ChangeActors.Broker;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RecoveredTrigger>> RecoverExpiredLeasesAsync(
        int batchSize,
        DateTimeOffset? nextAttemptUtc = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        batchSize = Math.Max(1, batchSize);

        lock (_gate)
        {
            var now = UtcNow;
            var attemptAt = nextAttemptUtc ?? now;
            var expired = _working.Values
                .Where(w => w.LeaseExpiresUtc <= now)
                .OrderBy(w => w.LeaseExpiresUtc)
                .Take(batchSize)
                .ToArray();

            var recovered = new List<RecoveredTrigger>(expired.Length);
            foreach (var working in expired)
            {
                if (!_triggers.TryGetValue(working.TriggerId, out var trigger))
                {
                    _working.Remove(working.TriggerId);
                    continue;
                }

                if (trigger.Status is not (TriggerStatus.Running or TriggerStatus.Claimed))
                {
                    _working.Remove(working.TriggerId);
                    continue;
                }

                var previousWorker = working.WorkerInstanceId;
                var previousLease = working.LeaseExpiresUtc;

                if (trigger.Status == TriggerStatus.Claimed)
                {
                    TriggerStatusTransitions.EnsureCanTransition(trigger.Status, TriggerStatus.Running);
                    trigger.Status = TriggerStatus.Running;
                }

                TriggerStatusTransitions.EnsureCanTransition(trigger.Status, TriggerStatus.Retry);
                trigger.Status = TriggerStatus.Retry;
                trigger.LastError = "lease expired";
                TriggerStatusTransitions.EnsureCanTransition(trigger.Status, TriggerStatus.Pending);
                trigger.Status = TriggerStatus.Pending;
                trigger.NextAttemptUtc = attemptAt;
                TouchBroker(trigger, now);
                _working.Remove(working.TriggerId);

                recovered.Add(new RecoveredTrigger
                {
                    Trigger = Clone(trigger),
                    PreviousWorkerInstanceId = previousWorker,
                    PreviousLeaseExpiresUtc = previousLease
                });
            }

            return Task.FromResult<IReadOnlyList<RecoveredTrigger>>(recovered);
        }
    }

    public Task<SystemEventTrigger?> GetByIdAsync(Guid triggerId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(
                _triggers.TryGetValue(triggerId, out var trigger) ? Clone(trigger) : null);
        }
    }

    public Task<SystemEventTrigger?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (_idempotencyIndex.TryGetValue(idempotencyKey, out var id)
                && _triggers.TryGetValue(id, out var trigger))
            {
                return Task.FromResult<SystemEventTrigger?>(Clone(trigger));
            }

            return Task.FromResult<SystemEventTrigger?>(null);
        }
    }

    public IReadOnlyList<SystemEventTrigger> GetAll()
    {
        lock (_gate)
        {
            return _triggers.Values.Select(Clone).OrderBy(t => t.CreatedUtc).ToArray();
        }
    }

    public IReadOnlyList<SystemEventWorking> GetWorking()
    {
        lock (_gate)
        {
            return _working.Values.Select(CloneWorking).ToArray();
        }
    }

    private SystemEventTrigger GetRequired(Guid triggerId)
    {
        if (!_triggers.TryGetValue(triggerId, out var trigger))
        {
            throw new KeyNotFoundException($"Trigger {triggerId} was not found.");
        }

        return trigger;
    }

    private static void TouchBroker(IAuditableEntity entity, DateTimeOffset when)
    {
        entity.DateModified = when;
        entity.ChangedBy = ChangeActors.Broker;
    }

    private static SystemEventTrigger Clone(SystemEventTrigger source) => new()
    {
        Id = source.Id,
        TriggerCode = source.TriggerCode,
        QueueName = source.QueueName,
        Status = source.Status,
        PayloadJson = source.PayloadJson,
        ResultJson = source.ResultJson,
        RootWorkflowId = source.RootWorkflowId,
        CorrelationId = source.CorrelationId,
        ParentTriggerId = source.ParentTriggerId,
        SourceTriggerId = source.SourceTriggerId,
        AllocationRequestId = source.AllocationRequestId,
        ExternalId = source.ExternalId,
        ExternalType = source.ExternalType,
        SourceComponent = source.SourceComponent,
        TargetComponent = source.TargetComponent,
        IdempotencyKey = source.IdempotencyKey,
        AttemptCount = source.AttemptCount,
        NextAttemptUtc = source.NextAttemptUtc,
        LastError = source.LastError,
        CreatedUtc = source.CreatedUtc,
        CompletedUtc = source.CompletedUtc,
        DateModified = source.DateModified,
        ChangedBy = source.ChangedBy
    };

    private static SystemEventWorking CloneWorking(SystemEventWorking source) => new()
    {
        TriggerId = source.TriggerId,
        WorkerInstanceId = source.WorkerInstanceId,
        QueueName = source.QueueName,
        ClaimedUtc = source.ClaimedUtc,
        HeartbeatUtc = source.HeartbeatUtc,
        LeaseExpiresUtc = source.LeaseExpiresUtc,
        DateModified = source.DateModified,
        ChangedBy = source.ChangedBy
    };
}
