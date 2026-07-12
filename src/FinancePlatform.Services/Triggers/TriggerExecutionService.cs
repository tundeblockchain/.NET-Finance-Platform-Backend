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
    TriggerEventProcessorRegistry processorRegistry,
    TriggerRetryService retryService,
    ILogger<TriggerExecutionService> logger)
{
    public async Task ExecuteAsync(ClaimedTrigger claimed, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(claimed);

        var trigger = claimed.Trigger;
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = trigger.CorrelationId,
            ["RootWorkflowId"] = trigger.RootWorkflowId,
            ["TriggerId"] = trigger.Id,
            ["TriggerCode"] = trigger.TriggerCode
        }))
        {
            await ExecuteCoreAsync(claimed, cancellationToken);
        }
    }

    private async Task ExecuteCoreAsync(ClaimedTrigger claimed, CancellationToken cancellationToken)
    {
        var trigger = claimed.Trigger;
        var context = ToContext(trigger);
        context.EnsureValid();

        if (!processorRegistry.TryGetProcessor(trigger.TriggerCode, out var processor) || processor is null)
        {
            await triggerStore.FailAsync(
                trigger.Id,
                $"No event processor registered for trigger code {trigger.TriggerCode}.",
                cancellationToken);
            return;
        }

        logger.LogInformation(
            "EP {EventProcessor} picked up trigger {TriggerId} code={TriggerCode} correlation={CorrelationId}",
            processor.Name,
            trigger.Id,
            trigger.TriggerCode,
            trigger.CorrelationId);

        var raiser = new TriggerRaiseBuffer();
        TriggerHandlerResult result;
        try
        {
            result = await processor.ProcessAsync(
                context,
                trigger.TriggerCode,
                trigger.PayloadJson,
                raiser,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "EP {EventProcessor} cancelled for trigger {TriggerId}; leaving lease for recovery",
                processor.Name,
                trigger.Id);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "EP {EventProcessor} failed for trigger {TriggerId} code={TriggerCode}",
                processor.Name,
                trigger.Id,
                trigger.TriggerCode);
            await retryService.ScheduleRetryAsync(trigger, ex.Message, cancellationToken);
            return;
        }

        switch (result.ResultCode)
        {
            case TriggerResultCode.Success:
                await CompleteWithChildrenAsync(trigger, processor.Name, result, raiser.Raised, cancellationToken);
                break;

            case TriggerResultCode.Retry:
                await retryService.ScheduleRetryAsync(
                    trigger,
                    result.Message ?? "Event processor requested retry.",
                    cancellationToken);
                break;

            case TriggerResultCode.Failure:
                await FailWithCompensationAsync(trigger, result, raiser.Raised, cancellationToken);
                break;

            case TriggerResultCode.Compensation:
                await triggerStore.MarkCompensationAsync(
                    trigger.Id,
                    result.Message ?? "Event processor requested compensation.",
                    cancellationToken);
                await EnqueueChildrenAsync(trigger, MergeRaised(result, raiser.Raised), cancellationToken);
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
        string eventProcessorName,
        TriggerHandlerResult result,
        IReadOnlyList<NextTriggerRequest> raised,
        CancellationToken cancellationToken)
    {
        await triggerStore.CompleteAsync(trigger.Id, result.ResultJson, cancellationToken);

        var children = MergeRaised(result, raised);
        await EnqueueChildrenAsync(trigger, children, cancellationToken);

        logger.LogInformation(
            "Trigger completed: EP={EventProcessor} TriggerId={TriggerId} Code={TriggerCode} CorrelationId={CorrelationId} Children={ChildCount}",
            eventProcessorName,
            trigger.Id,
            trigger.TriggerCode,
            trigger.CorrelationId,
            children.Count);
    }

    private async Task FailWithCompensationAsync(
        SystemEventTrigger trigger,
        TriggerHandlerResult result,
        IReadOnlyList<NextTriggerRequest> raised,
        CancellationToken cancellationToken)
    {
        await triggerStore.FailAsync(trigger.Id, result.Message ?? "Event processor failed.", cancellationToken);

        var compensationTriggers = raised.Count > 0 || result.NextTriggers.Count > 0
            ? MergeRaised(result, raised)
            : CreateDefaultCompensation(trigger);

        await EnqueueChildrenAsync(trigger, compensationTriggers, cancellationToken);

        logger.LogWarning(
            "Trigger failed: TriggerId={TriggerId} Code={TriggerCode}; enqueued {ChildCount} compensation trigger(s)",
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

    private static IReadOnlyList<NextTriggerRequest> MergeRaised(
        TriggerHandlerResult result,
        IReadOnlyList<NextTriggerRequest> raised)
    {
        if (raised.Count == 0)
        {
            return result.NextTriggers;
        }

        if (result.NextTriggers.Count == 0)
        {
            return raised;
        }

        return raised.Concat(result.NextTriggers).ToArray();
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
                QueueName = QueueNames.Reversal,
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
