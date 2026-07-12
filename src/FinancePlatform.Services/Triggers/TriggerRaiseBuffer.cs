namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Collects <see cref="ITriggerRaiser.RaiseTrigger"/> calls during a single EP invocation.
/// </summary>
public sealed class TriggerRaiseBuffer : ITriggerRaiser
{
    private readonly List<NextTriggerRequest> _raised = [];

    public IReadOnlyList<NextTriggerRequest> Raised => _raised;

    public void RaiseTrigger(
        int triggerCode,
        string queueName,
        string targetComponent,
        string payloadJson,
        string idempotencyKey)
    {
        _raised.Add(new NextTriggerRequest
        {
            TriggerCode = triggerCode,
            QueueName = queueName,
            TargetComponent = targetComponent,
            PayloadJson = payloadJson,
            IdempotencyKey = idempotencyKey
        });
    }
}
