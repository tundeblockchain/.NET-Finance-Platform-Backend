namespace FinancePlatform.Services.Cash;

public enum CashLockStatus
{
    Acquired = 1,
    AlreadyOwned = 2,
    Contended = 3
}
