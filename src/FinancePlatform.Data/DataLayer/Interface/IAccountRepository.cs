using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IAccountRepository
{
    Task<Account?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Account> UpsertAsync(Account account, CancellationToken cancellationToken = default);
}
