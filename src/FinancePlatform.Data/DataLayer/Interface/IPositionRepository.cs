using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IPositionRepository
{
    Task<Position?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Position> UpsertAsync(Position entity, CancellationToken cancellationToken = default);
}
