using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ILedgerEntryRepository
{
    Task<LedgerEntry?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LedgerEntry> UpsertAsync(LedgerEntry entity, CancellationToken cancellationToken = default);
}
