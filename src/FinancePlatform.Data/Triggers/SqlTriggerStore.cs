using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.Triggers;

/// <summary>
/// SQL Server-backed trigger store using ClaimTrigger / CompleteTrigger / RetryTrigger SPs.
/// </summary>
public sealed class SqlTriggerStore(IDbConnectionFactory connectionFactory) : ITriggerStore
{
    public async Task<SystemEventTrigger> EnqueueAsync(
        EnqueueTriggerCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var trigger = await connection.QuerySingleAsync<SystemEventTrigger>(
            new CommandDefinition(
                TriggerProcedureNames.Enqueue,
                new
                {
                    Id = Guid.NewGuid(),
                    command.TriggerCode,
                    command.QueueName,
                    command.PayloadJson,
                    command.RootWorkflowId,
                    command.CorrelationId,
                    command.ParentTriggerId,
                    command.SourceTriggerId,
                    command.AllocationRequestId,
                    command.ExternalId,
                    ExternalType = command.ExternalType.HasValue ? (int?)command.ExternalType.Value : null,
                    command.SourceComponent,
                    command.TargetComponent,
                    command.IdempotencyKey,
                    command.NextAttemptUtc
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return trigger;
    }

    public async Task<ClaimedTrigger?> TryClaimAsync(
        string queueName,
        string workerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                TriggerProcedureNames.Claim,
                new
                {
                    QueueName = queueName,
                    WorkerInstanceId = workerInstanceId,
                    LeaseSeconds = Math.Max(5, (int)leaseDuration.TotalSeconds)
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        var trigger = await multi.ReadSingleOrDefaultAsync<SystemEventTrigger>();
        if (trigger is null)
        {
            return null;
        }

        var working = await multi.ReadSingleAsync<SystemEventWorking>();
        return new ClaimedTrigger { Trigger = trigger, Working = working };
    }

    public async Task CompleteAsync(Guid triggerId, string? resultJson, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                TriggerProcedureNames.Complete,
                new { TriggerId = triggerId, ResultJson = resultJson },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task RetryAsync(
        Guid triggerId,
        string error,
        DateTimeOffset nextAttemptUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                TriggerProcedureNames.Retry,
                new { TriggerId = triggerId, Error = error, NextAttemptUtc = nextAttemptUtc },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task FailAsync(Guid triggerId, string error, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                TriggerProcedureNames.Fail,
                new { TriggerId = triggerId, Error = error },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<SystemEventTrigger> RequeueAsync(
        Guid triggerId,
        DateTimeOffset? nextAttemptUtc = null,
        bool resetAttemptCount = true,
        string? changedBy = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<SystemEventTrigger>(
            new CommandDefinition(
                TriggerProcedureNames.Requeue,
                new
                {
                    TriggerId = triggerId,
                    NextAttemptUtc = nextAttemptUtc,
                    ResetAttemptCount = resetAttemptCount,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task MarkCompensationAsync(Guid triggerId, string error, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                TriggerProcedureNames.MarkCompensation,
                new { TriggerId = triggerId, Error = error },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task HeartbeatAsync(
        Guid triggerId,
        string workerInstanceId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                TriggerProcedureNames.Heartbeat,
                new
                {
                    TriggerId = triggerId,
                    WorkerInstanceId = workerInstanceId,
                    LeaseSeconds = Math.Max(5, (int)leaseDuration.TotalSeconds)
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<RecoveredTrigger>> RecoverExpiredLeasesAsync(
        int batchSize,
        DateTimeOffset? nextAttemptUtc = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<SystemEventTrigger>(
            new CommandDefinition(
                TriggerProcedureNames.RecoverExpired,
                new
                {
                    BatchSize = Math.Clamp(batchSize, 1, 500),
                    NextAttemptUtc = nextAttemptUtc
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken))).ToArray();

        return rows
            .Select(t => new RecoveredTrigger
            {
                Trigger = t,
                PreviousWorkerInstanceId = "unknown",
                PreviousLeaseExpiresUtc = DateTimeOffset.MinValue
            })
            .ToArray();
    }

    public async Task<SystemEventTrigger?> GetByIdAsync(Guid triggerId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<SystemEventTrigger>(
            new CommandDefinition(
                TriggerProcedureNames.GetById,
                new { Id = triggerId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<SystemEventTrigger?> FindByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<SystemEventTrigger>(
            new CommandDefinition(
                TriggerProcedureNames.GetByIdempotencyKey,
                new { IdempotencyKey = idempotencyKey },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public IReadOnlyList<SystemEventTrigger> GetAll() =>
        throw new NotSupportedException("GetAll is only supported by the in-memory trigger store.");

    public IReadOnlyList<SystemEventWorking> GetWorking() =>
        throw new NotSupportedException("GetWorking is only supported by the in-memory trigger store.");
}
