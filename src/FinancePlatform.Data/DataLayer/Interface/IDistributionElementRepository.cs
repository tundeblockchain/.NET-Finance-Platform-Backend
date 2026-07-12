using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface IDistributionElementRepository
{
    Task<DistributionElement?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DistributionElement>> GetByAgreementIdAsync(
        Guid agreementId,
        CancellationToken cancellationToken = default);

    Task<DistributionElement> UpsertAsync(
        DistributionElement element,
        CancellationToken cancellationToken = default);
}
