using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Ledger;

public interface ILedgerService
{
    LedgerPostResult TryPost(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        LedgerEntryType entryType,
        string description,
        Guid? triggerId,
        Guid? allocationRequestId);

    LedgerEntry? FindByIdempotencyKey(string idempotencyKey);

    int EntryCount { get; }
}
