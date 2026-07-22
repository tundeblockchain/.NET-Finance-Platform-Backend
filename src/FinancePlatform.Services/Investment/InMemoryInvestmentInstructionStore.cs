using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Investment;

public sealed class InvestmentInstructionCreateResult
{
    public bool Succeeded { get; init; }

    public InvestmentInstruction? Instruction { get; init; }

    public bool AlreadyApplied { get; init; }

    public string? Error { get; init; }

    public static InvestmentInstructionCreateResult Success(InvestmentInstruction instruction, bool alreadyApplied = false) =>
        new() { Succeeded = true, Instruction = instruction, AlreadyApplied = alreadyApplied };

    public static InvestmentInstructionCreateResult Failure(string error) =>
        new() { Succeeded = false, Error = error };
}

public sealed class InMemoryInvestmentInstructionStore : IInvestmentInstructionStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, InvestmentInstruction> _byId = new();
    private readonly Dictionary<string, Guid> _byKey = new(StringComparer.Ordinal);

    public InvestmentInstructionCreateResult TryCreate(InvestmentInstruction instruction)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction.IdempotencyKey);

        lock (_gate)
        {
            if (_byKey.TryGetValue(instruction.IdempotencyKey, out var existingId)
                && _byId.TryGetValue(existingId, out var existing))
            {
                return InvestmentInstructionCreateResult.Success(Clone(existing), alreadyApplied: true);
            }

            var now = DateTimeOffset.UtcNow;
            instruction.Id = instruction.Id == Guid.Empty ? Guid.NewGuid() : instruction.Id;
            instruction.CreatedUtc = now;
            instruction.DateModified = now;
            instruction.ChangedBy = ChangeActors.Broker;

            _byId[instruction.Id] = Clone(instruction);
            _byKey[instruction.IdempotencyKey] = instruction.Id;
            return InvestmentInstructionCreateResult.Success(Clone(instruction));
        }
    }

    public InvestmentInstruction? GetById(Guid instructionId)
    {
        lock (_gate)
        {
            return _byId.TryGetValue(instructionId, out var instruction) ? Clone(instruction) : null;
        }
    }

    public InvestmentInstruction? GetByIdempotencyKey(string idempotencyKey)
    {
        lock (_gate)
        {
            return _byKey.TryGetValue(idempotencyKey, out var id) && _byId.TryGetValue(id, out var instruction)
                ? Clone(instruction)
                : null;
        }
    }

    public bool TrySetOrderId(Guid instructionId, Guid orderId)
    {
        lock (_gate)
        {
            if (!_byId.TryGetValue(instructionId, out var instruction))
            {
                return false;
            }

            instruction.OrderId = orderId;
            instruction.DateModified = DateTimeOffset.UtcNow;
            return true;
        }
    }

    public bool TryUpdateStatus(Guid instructionId, InvestmentInstructionStatus status)
    {
        lock (_gate)
        {
            if (!_byId.TryGetValue(instructionId, out var instruction))
            {
                return false;
            }

            instruction.Status = status;
            instruction.DateModified = DateTimeOffset.UtcNow;
            return true;
        }
    }

    public decimal GetPendingCashAmount(Guid tradingAccountId)
    {
        lock (_gate)
        {
            return _byId.Values
                .Where(i => i.TradingAccountId == tradingAccountId
                    && i.Status is InvestmentInstructionStatus.Pending or InvestmentInstructionStatus.Processing)
                .Sum(i => i.CashAmount);
        }
    }

    private static InvestmentInstruction Clone(InvestmentInstruction i) => new()
    {
        Id = i.Id,
        CustomerId = i.CustomerId,
        TradingAccountId = i.TradingAccountId,
        InvestmentAccountId = i.InvestmentAccountId,
        AssetSymbol = i.AssetSymbol,
        Quantity = i.Quantity,
        CashAmount = i.CashAmount,
        Currency = i.Currency,
        Side = i.Side,
        Status = i.Status,
        OrderId = i.OrderId,
        IdempotencyKey = i.IdempotencyKey,
        CreatedUtc = i.CreatedUtc,
        DateModified = i.DateModified,
        ChangedBy = i.ChangedBy
    };
}
