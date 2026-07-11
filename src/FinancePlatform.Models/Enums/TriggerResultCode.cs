namespace FinancePlatform.Models.Enums;

/// <summary>
/// Outcome returned by a trigger handler after executing one atomic business action.
/// </summary>
public enum TriggerResultCode
{
    Success = 0,
    Retry = 1,
    Failure = 2,
    Compensation = 3
}
