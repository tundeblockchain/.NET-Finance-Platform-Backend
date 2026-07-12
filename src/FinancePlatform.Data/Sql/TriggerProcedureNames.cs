namespace FinancePlatform.Data.Sql;

public static class TriggerProcedureNames
{
    public const string Enqueue = "EnqueueTrigger";
    public const string Claim = "ClaimTrigger";
    public const string Complete = "CompleteTrigger";
    public const string Retry = "RetryTrigger";
    public const string Fail = "FailTrigger";
    public const string MarkCompensation = "MarkCompensationTrigger";
    public const string Heartbeat = "HeartbeatTrigger";
    public const string GetById = "get_SystemEventTrigger_f";
    public const string GetByIdempotencyKey = "get_SystemEventTrigger_ByIdempotencyKey_f";
    public const string Upsert = "SystemEventTrigger_u";
    public const string WorkingUpsert = "SystemEventWorking_u";
    public const string GetWorking = "get_SystemEventWorking_f";
}
