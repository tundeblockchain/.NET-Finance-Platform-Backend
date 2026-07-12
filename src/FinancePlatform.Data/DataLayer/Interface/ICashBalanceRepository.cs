using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ICashBalanceRepository
{
    Task<CashBalance?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CashBalance> UpsertAsync(CashBalance entity, CancellationToken cancellationToken = default);
}
