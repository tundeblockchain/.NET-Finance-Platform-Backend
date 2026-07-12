using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IDistributionAgreementRepository
{
    Task<DistributionAgreement?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DistributionAgreement?> GetByOwnerAccountAsync(
        Guid ownerAccountId,
        CancellationToken cancellationToken = default);

    Task<DistributionAgreement> UpsertAsync(
        DistributionAgreement agreement,
        CancellationToken cancellationToken = default);
}
