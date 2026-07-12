using FinancePlatform.Data.Triggers;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Models.ValueObjects;
using Microsoft.Extensions.Logging;

namespace FinancePlatform.Services.Triggers;

public sealed class TriggerExecutionService(
    ITriggerStore triggerStore,
    TriggerHandlerRegistry handlerRegistry,
    TriggerRetryService retryService,
    ILogger<TriggerExecutionService> logger)
{
    public async Task ExecuteAsync(ClaimedTrigger claimed, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claimed);

        var trigger = claimed.Trigger;
        var context = ToContext(trigger);
        context.EnsureValid();

        if (!handlerRegistry.TryGetHandler(trigger.TriggerCode, out var handler) || handler is null)
        {
            await triggerStore.FailAsync(
                trigger.Id,
                $"No handler registered for trigger code {trigger.TriggerCode}.",
                cancellationToken);
            return;
        }

        TriggerHandlerResult result;
        try
        {
            result = await handler.ExecuteAsync(context, trigger.PayloadJson, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Handler failed for trigger {TriggerId} code={TriggerCode}", trigger.Id, trigger.TriggerCode);
            await retryService.ScheduleRetryAsync(trigger, ex.Message, cancellationToken);
            return;
        }

        switch (result.ResultCode)
        {
            case TriggerResultCode.Success:
                await CompleteWithChildrenAsync(trigger, result, cancellationToken);
                break;

            case TriggerResultCode.Retry:
                await retryService.ScheduleRetryAsync(
                    trigger,
                    result.Message ?? "Handler requested retry.",
                    cancellationToken);
                break;

            case TriggerResultCode.Failure:
                await FailWithCompensationAsync(trigger, result, cancellationToken);
                break;

            case TriggerResultCode.Compensation:
                await triggerStore.MarkCompensationAsync(
                    trigger.Id,
                    result.Message ?? "Handler requested compensation.",
                    cancellationToken);
                await EnqueueChildrenAsync(trigger, result.NextTriggers, cancellationToken);
                break;

            default:
                await triggerStore.FailAsync(
                    trigger.Id,
                    $"Unknown result code {result.ResultCode}.",
                    cancellationToken);
                break;
        }
    }

    private async Task CompleteWithChildrenAsync(
        SystemEventTrigger trigger,
        TriggerHandlerResult result,
        CancellationToken cancellationToken)
    {
        await triggerStore.CompleteAsync(trigger.Id, result.ResultJson, cancellationToken);
        await EnqueueChildrenAsync(trigger, result.NextTriggers, cancellationToken);

        logger.LogInformation(
            "Completed trigger {TriggerId} code={TriggerCode}; enqueued {ChildCount} child trigger(s)",
            trigger.Id,
            trigger.TriggerCode,
            result.NextTriggers.Count);
    }

    private async Task FailWithCompensationAsync(
        SystemEventTrigger trigger,
        TriggerHandlerResult result,
        CancellationToken cancellationToken)
    {
        await triggerStore.FailAsync(trigger.Id, result.Message ?? "Handler failed.", cancellationToken);

        var compensationTriggers = result.NextTriggers.Count > 0
            ? result.NextTriggers
            : CreateDefaultCompensation(trigger);

        await EnqueueChildrenAsync(trigger, compensationTriggers, cancellationToken);

        logger.LogWarning(
            "Failed trigger {TriggerId} code={TriggerCode}; enqueued {ChildCount} compensation trigger(s)",
            trigger.Id,
            trigger.TriggerCode,
            compensationTriggers.Count);
    }

    private async Task EnqueueChildrenAsync(
        SystemEventTrigger parent,
        IReadOnlyList<NextTriggerRequest> children,
        CancellationToken cancellationToken)
    {
        foreach (var child in children)
        {
            await triggerStore.EnqueueAsync(new EnqueueTriggerCommand
            {
                TriggerCode = child.TriggerCode,
                QueueName = child.QueueName,
                PayloadJson = child.PayloadJson,
                RootWorkflowId = parent.RootWorkflowId,
                CorrelationId = parent.CorrelationId,
                ParentTriggerId = parent.Id,
                SourceTriggerId = parent.Id,
                AllocationRequestId = parent.AllocationRequestId,
                ExternalId = parent.ExternalId,
                ExternalType = parent.ExternalType,
                SourceComponent = parent.TargetComponent,
                TargetComponent = child.TargetComponent,
                IdempotencyKey = child.IdempotencyKey
            }, cancellationToken);
        }
    }

    private static IReadOnlyList<NextTriggerRequest> CreateDefaultCompensation(SystemEventTrigger trigger)
    {
        if (TriggerCodes.IsCompensation(trigger.TriggerCode))
        {
            return [];
        }

        return
        [
            new NextTriggerRequest
            {
                TriggerCode = TriggerCodes.Compensate(trigger.TriggerCode),
                QueueName = "Reversal",
                TargetComponent = trigger.TargetComponent,
                PayloadJson = trigger.PayloadJson,
                IdempotencyKey = $"{trigger.IdempotencyKey}:compensate"
            }
        ];
    }

    private static TriggerContext ToContext(SystemEventTrigger trigger) => new()
    {
        TriggerId = trigger.Id,
        RootWorkflowId = trigger.RootWorkflowId,
        CorrelationId = trigger.CorrelationId,
        ParentTriggerId = trigger.ParentTriggerId,
        SourceTriggerId = trigger.SourceTriggerId,
        AllocationRequestId = trigger.AllocationRequestId,
        ExternalId = trigger.ExternalId,
        ExternalType = trigger.ExternalType,
        SourceComponent = trigger.SourceComponent,
        TargetComponent = trigger.TargetComponent,
        IdempotencyKey = new IdempotencyKey(trigger.IdempotencyKey)
    };
}
