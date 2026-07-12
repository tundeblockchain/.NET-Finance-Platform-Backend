using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Entities;
using CustomerEntity = FinancePlatform.Models.Entities.Customer;

namespace FinancePlatform.Services.Customer;

public sealed class CustomerProvisioningResult
{
    public required CustomerEntity Customer { get; init; }

    public CustomerAddress? Address { get; init; }

    public required CustomerAccount CustomerAccount { get; init; }

    public required TradingAccount TradingAccount { get; init; }

    public required DistributionAgreement DistributionAgreement { get; init; }
}
