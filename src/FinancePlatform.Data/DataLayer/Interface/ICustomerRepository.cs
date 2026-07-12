using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ICustomerRepository
{
    Task<Customer?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Customer> UpsertAsync(Customer customer, CancellationToken cancellationToken = default);
}
