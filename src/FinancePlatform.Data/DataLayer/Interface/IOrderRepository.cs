using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IOrderRepository
{
    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order> UpsertAsync(Order entity, CancellationToken cancellationToken = default);
}
