using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Cash;

public sealed class CashLockResult
{
    public required CashLockStatus Status { get; init; }

    public CashBalance? Balance { get; init; }

    public bool IsHeld => Status is CashLockStatus.Acquired or CashLockStatus.AlreadyOwned;

    public static CashLockResult Acquired(CashBalance balance) =>
        new() { Status = CashLockStatus.Acquired, Balance = balance };

    public static CashLockResult AlreadyOwned(CashBalance balance) =>
        new() { Status = CashLockStatus.AlreadyOwned, Balance = balance };

    public static CashLockResult Contended() =>
        new() { Status = CashLockStatus.Contended };
}
