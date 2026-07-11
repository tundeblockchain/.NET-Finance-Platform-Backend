namespace FinancePlatform.Models.Enums;

public enum TriggerStatus
{
    Pending = 0,
    Claimed = 1,
    Running = 2,
    Completed = 3,
    Retry = 4,
    Failed = 5,
    Compensation = 6
}
