using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Ledger;

public sealed class LedgerPostResult
{
    public required bool Succeeded { get; init; }

    public bool AlreadyApplied { get; init; }

    public LedgerEntry? Entry { get; init; }

    public string? Error { get; init; }

    public static LedgerPostResult Success(LedgerEntry entry) =>
        new() { Succeeded = true, Entry = entry };

    public static LedgerPostResult Duplicate(LedgerEntry entry) =>
        new() { Succeeded = true, AlreadyApplied = true, Entry = entry };

    public static LedgerPostResult Fail(string error) =>
        new() { Succeeded = false, Error = error };
}
