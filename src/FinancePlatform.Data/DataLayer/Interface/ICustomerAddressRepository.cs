using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ICustomerAddressRepository
{
    Task<CustomerAddress?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    Task<CustomerAddress> UpsertAsync(CustomerAddress address, CancellationToken cancellationToken = default);
}
