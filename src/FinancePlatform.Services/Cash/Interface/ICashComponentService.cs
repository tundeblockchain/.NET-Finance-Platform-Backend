using FinancePlatform.Models.Cash;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Dtos;

namespace FinancePlatform.Services.Cash;

/// <summary>
/// Orchestrates deposit workflows for CashEP (uses primitive cash + ledger services).
/// </summary>
public interface ICashComponentService
{
    ComponentOperationResult Deposit(TriggerContext context, DepositCashRequest request);
}
