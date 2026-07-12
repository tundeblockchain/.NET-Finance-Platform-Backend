using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Entities;

namespace FinancePlatform.Data.DataLayer;

public interface ICustomerProvisionRepository
{
    Task<ProvisionedCustomerBundle> ProvisionAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ProvisionedCustomerBundle
{
    public required Customer Customer { get; init; }

    public CustomerAddress? Address { get; init; }

    public required CustomerAccount CustomerAccount { get; init; }

    public required TradingAccount TradingAccount { get; init; }

    public required DistributionAgreement DistributionAgreement { get; init; }
}
