using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Data.DataLayer;

public interface IInvestmentInstructionRepository
{
    Task<InvestmentInstruction?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<InvestmentInstruction?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(InvestmentInstruction Instruction, bool AlreadyApplied)> CreateAsync(
        InvestmentInstruction instruction,
        CancellationToken cancellationToken = default);

    Task<InvestmentInstruction> UpsertAsync(
        InvestmentInstruction instruction,
        CancellationToken cancellationToken = default);

    Task<bool> SetOrderIdAsync(
        Guid instructionId,
        Guid orderId,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateStatusAsync(
        Guid instructionId,
        InvestmentInstructionStatus status,
        string changedBy,
        CancellationToken cancellationToken = default);

    Task<decimal> GetPendingCashAmountAsync(
        Guid tradingAccountId,
        CancellationToken cancellationToken = default);
}
