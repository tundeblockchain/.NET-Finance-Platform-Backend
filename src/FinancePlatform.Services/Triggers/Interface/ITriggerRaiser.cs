namespace FinancePlatform.Services.Triggers;

/// <summary>
/// Used by event processors to enqueue follow-on triggers after the current one succeeds.
/// </summary>
public interface ITriggerRaiser
{
    void RaiseTrigger(
        int triggerCode,
        string queueName,
        string targetComponent,
        string payloadJson,
        string idempotencyKey);
}
