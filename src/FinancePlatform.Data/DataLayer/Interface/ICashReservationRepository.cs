using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ICashReservationRepository
{
    Task<CashReservation?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CashReservation> UpsertAsync(CashReservation entity, CancellationToken cancellationToken = default);
}
