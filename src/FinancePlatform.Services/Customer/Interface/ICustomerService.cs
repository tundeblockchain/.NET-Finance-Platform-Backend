using FinancePlatform.Models.Components;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;

namespace FinancePlatform.Services.Customer;

public interface ICustomerService
{
    CustomerProvisioningResult CreateCustomer(CreateCustomerRequest request);

    CustomerProvisioningResult? GetCustomer(int customerId, string? currency = null);

    ComponentOperationResult DepositMoney(TriggerContext context, CustomerDepositRequest request);

    ComponentOperationResult ReceiveMoney(TriggerContext context, CustomerReceiveMoneyRequest request);

    ComponentOperationResult DistributeMoney(
        TriggerContext context,
        DistributeMoneyRequest request,
        string rawPayloadJson);
}
