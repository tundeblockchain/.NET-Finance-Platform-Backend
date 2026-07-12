namespace FinancePlatform.Data.Sql;

public static class CashProcedureNames
{
    public const string AcquireCashLock = "AcquireCashLock";
    public const string ReleaseCashLock = "ReleaseCashLock";
    public const string ReserveCash = "ReserveCash";
    public const string DepositCash = "DepositCash";
    public const string ReleaseCashReservation = "ReleaseCashReservation";
    public const string ConsumeCashReservation = "ConsumeCashReservation";
    public const string GetCashBalanceByAccountCurrency = "get_CashBalance_ByAccountCurrency_f";
    public const string CreateLedgerEntry = "CreateLedgerEntry";
}
