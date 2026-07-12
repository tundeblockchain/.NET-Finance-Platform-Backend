using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Customer;
using FluentAssertions;

namespace FinancePlatform.UnitTests.Trading;

public class CustomerProvisioningTests
{
    [Fact]
    public void Create_customer_provisions_accounts_and_trading_distribution_element()
    {
        var directory = new InMemoryCustomerDirectory();
        var service = new CustomerService(directory);

        var created = service.CreateCustomer(new CreateCustomerRequest
        {
            Email = "ada@example.com",
            FirstName = "Ada",
            LastName = "Lovelace",
            Currency = "GBP",
            Address = new CustomerAddressRequest
            {
                Line1 = "1 Analytical Engine Way",
                City = "London",
                PostalCode = "EC1A 1BB",
                Country = "GB"
            }
        });

        created.Customer.Id.Should().BeGreaterThan(0);
        created.CustomerAccount.Settled.Should().Be(0m);
        created.TradingAccount.Settled.Should().Be(0m);
        created.Address.Should().NotBeNull();

        var elements = directory.GetActiveElements(created.CustomerAccount.Id);
        elements.Should().ContainSingle(e =>
            e.TargetType == DistributionTargetType.TradingAccount
            && e.TargetAccountId == created.TradingAccount.Id);

        var loaded = service.GetCustomer(created.Customer.Id);
        loaded.Should().NotBeNull();
        loaded!.Customer.Email.Should().Be("ada@example.com");
        loaded.DistributionAgreement.Id.Should().Be(created.DistributionAgreement.Id);
    }
}
