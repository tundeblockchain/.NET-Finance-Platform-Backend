using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Investment;

public interface IInvestmentInstructionStore
{
    InvestmentInstructionCreateResult TryCreate(InvestmentInstruction instruction);

    InvestmentInstruction? GetById(Guid instructionId);

    InvestmentInstruction? GetByIdempotencyKey(string idempotencyKey);

    bool TrySetOrderId(Guid instructionId, Guid orderId);

    bool TryUpdateStatus(Guid instructionId, InvestmentInstructionStatus status);

    decimal GetPendingCashAmount(Guid tradingAccountId);
}
