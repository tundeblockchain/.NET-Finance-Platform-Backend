namespace FinancePlatform.Data.Sql;

/// <summary>
/// Naming conventions for model tables, archive tables, and stored procedures.
/// Archive pattern applies to all models except SystemEventTrigger / SystemEventWorking.
/// </summary>
public static class SqlObjectNames
{
    public const string BrokerChangedBy = "broker";

    public static readonly IReadOnlyList<string> ArchivedModels =
    [
        "Account",
        "AllocationRequest",
        "CashBalance",
        "CashReservation",
        "Position",
        "Order",
        "AssetPrice",
        "LedgerEntry",
        "Customer",
        "CustomerAddress",
        "CustomerAccount",
        "TradingAccount",
        "InvestmentAccount",
        "InvestmentInstruction",
        "DistributionAgreement",
        "DistributionElement"
    ];

    public static readonly IReadOnlyList<string> NonArchivedModels =
    [
        "SystemEventTrigger",
        "SystemEventWorking"
    ];

    public static string Table(string modelName) => modelName;

    public static string ArchiveTable(string modelName) => $"{modelName}_a";

    public static string GetProc(string modelName) => $"get_{modelName}_f";

    public static string UpsertProc(string modelName) => $"{modelName}_u";

    public static bool HasArchive(string modelName) =>
        ArchivedModels.Contains(modelName, StringComparer.Ordinal);
}
