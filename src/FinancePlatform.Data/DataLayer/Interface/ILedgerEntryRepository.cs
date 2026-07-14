using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Data.DataLayer;

public interface ILedgerEntryRepository
{
    Task<LedgerEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LedgerEntry?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(LedgerEntry Entry, bool AlreadyApplied)> CreateAsync(
        Guid id,
        Guid accountId,
        Guid? triggerId,
        Guid? allocationRequestId,
        LedgerEntryType entryType,
        decimal amount,
        string currency,
        string idempotencyKey,
        string description,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task<LedgerEntry> UpsertAsync(LedgerEntry entity, CancellationToken cancellationToken = default);
}
