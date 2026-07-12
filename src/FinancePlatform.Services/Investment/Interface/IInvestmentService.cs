using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Investment;

namespace FinancePlatform.Services.Investment;

public interface IInvestmentService
{
    ComponentOperationResult ReceiveMoney(TriggerContext context, InvestMoneyRequest request, string rawPayloadJson);

    ComponentOperationResult InvestMoney(TriggerContext context, InvestMoneyRequest request, string rawPayloadJson);
}
