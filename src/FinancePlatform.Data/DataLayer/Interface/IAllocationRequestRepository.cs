using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IAllocationRequestRepository
{
    Task<AllocationRequest?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AllocationRequest> UpsertAsync(AllocationRequest entity, CancellationToken cancellationToken = default);
}
