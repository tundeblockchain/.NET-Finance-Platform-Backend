using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;
using FinancePlatform.Services.Customer;
using CustomerEntity = FinancePlatform.Models.Entities.Customer;

namespace FinancePlatform.UnitTests.Api.Support;

internal static class ApiTestFixtures
{
    public static CustomerProvisioningResult CreateProvisionedCustomer(
        int customerId = 1,
        decimal customerSettled = 0m,
        decimal tradingSettled = 0m,
        string currency = "GBP")
    {
        var now = DateTimeOffset.UtcNow;
        var customerAccountId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tradingAccountId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var agreementId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        return new CustomerProvisioningResult
        {
            Customer = new CustomerEntity
            {
                Id = customerId,
                Email = "trader@example.com",
                FirstName = "Test",
                LastName = "Trader",
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.System
            },
            CustomerAccount = new CustomerAccount
            {
                Id = customerAccountId,
                CustomerId = customerId,
                Currency = currency,
                Settled = customerSettled,
                Reserved = 0m,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.System
            },
            TradingAccount = new TradingAccount
            {
                Id = tradingAccountId,
                CustomerId = customerId,
                Currency = currency,
                Settled = tradingSettled,
                Reserved = 0m,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.System
            },
            DistributionAgreement = new DistributionAgreement
            {
                Id = agreementId,
                CustomerId = customerId,
                OwnerComponent = ComponentType.Customer,
                OwnerAccountId = customerAccountId,
                Name = "Customer → Trading (park)",
                IsActive = true,
                CreatedUtc = now,
                DateModified = now,
                ChangedBy = ChangeActors.System
            }
        };
    }

    public static SystemEventTrigger CreateTrigger(int triggerCode, string queueName, string idempotencyKey = "idem-1")
    {
        var id = Guid.NewGuid();
        return new SystemEventTrigger
        {
            Id = id,
            TriggerCode = triggerCode,
            QueueName = queueName,
            PayloadJson = "{}",
            RootWorkflowId = id,
            CorrelationId = id,
            SourceComponent = "Api",
            TargetComponent = "Customer",
            IdempotencyKey = idempotencyKey,
            CreatedUtc = DateTimeOffset.UtcNow,
            DateModified = DateTimeOffset.UtcNow,
            ChangedBy = ChangeActors.Broker
        };
    }
}
