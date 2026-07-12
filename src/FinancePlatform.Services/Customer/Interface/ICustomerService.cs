using FinancePlatform.Models.Components;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;

namespace FinancePlatform.Services.Customer;

public interface ICustomerService
{
    ComponentOperationResult DistributeMoney(
        TriggerContext context,
        DistributeMoneyRequest request,
        string rawPayloadJson);
}
