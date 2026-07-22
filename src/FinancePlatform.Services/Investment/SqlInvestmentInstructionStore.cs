using FinancePlatform.Data.DataLayer;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Investment;

/// <summary>
/// SQL-backed investment instruction store using stored procedures.
/// </summary>
public sealed class SqlInvestmentInstructionStore(IInvestmentInstructionRepository repository)
    : IInvestmentInstructionStore
{
    public InvestmentInstructionCreateResult TryCreate(InvestmentInstruction instruction)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction.IdempotencyKey);

        try
        {
            if (instruction.Id == Guid.Empty)
            {
                instruction.Id = Guid.NewGuid();
            }

            if (string.IsNullOrWhiteSpace(instruction.ChangedBy))
            {
                instruction.ChangedBy = ChangeActors.Broker;
            }

            var (created, alreadyApplied) = repository
                .CreateAsync(instruction)
                .GetAwaiter()
                .GetResult();

            return InvestmentInstructionCreateResult.Success(created, alreadyApplied);
        }
        catch (Exception ex)
        {
            return InvestmentInstructionCreateResult.Failure(ex.Message);
        }
    }

    public InvestmentInstruction? GetById(Guid instructionId) =>
        repository.GetAsync(instructionId).GetAwaiter().GetResult();

    public InvestmentInstruction? GetByIdempotencyKey(string idempotencyKey) =>
        repository.GetByIdempotencyKeyAsync(idempotencyKey).GetAwaiter().GetResult();

    public bool TrySetOrderId(Guid instructionId, Guid orderId) =>
        repository.SetOrderIdAsync(instructionId, orderId, ChangeActors.Broker).GetAwaiter().GetResult();

    public bool TryUpdateStatus(Guid instructionId, InvestmentInstructionStatus status) =>
        repository.UpdateStatusAsync(instructionId, status, ChangeActors.Broker).GetAwaiter().GetResult();

    public decimal GetPendingCashAmount(Guid tradingAccountId) =>
        repository.GetPendingCashAmountAsync(tradingAccountId).GetAwaiter().GetResult();
}
