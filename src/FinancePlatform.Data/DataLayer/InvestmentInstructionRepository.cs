using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Data.DataLayer;

public sealed class InvestmentInstructionRepository(IDbConnectionFactory connectionFactory)
    : IInvestmentInstructionRepository
{
    public async Task<InvestmentInstruction?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<InvestmentInstruction>(
            new CommandDefinition(
                SqlObjectNames.GetProc("InvestmentInstruction"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<InvestmentInstruction?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<InvestmentInstruction>(
            new CommandDefinition(
                "get_InvestmentInstruction_ByIdempotencyKey_f",
                new { IdempotencyKey = idempotencyKey },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<(InvestmentInstruction Instruction, bool AlreadyApplied)> CreateAsync(
        InvestmentInstruction instruction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleAsync<InvestmentInstructionMutationRow>(
            new CommandDefinition(
                "CreateInvestmentInstruction",
                new
                {
                    instruction.Id,
                    instruction.CustomerId,
                    instruction.TradingAccountId,
                    instruction.InvestmentAccountId,
                    instruction.AssetSymbol,
                    instruction.Quantity,
                    instruction.CashAmount,
                    instruction.Currency,
                    Side = (int)instruction.Side,
                    Status = (int)instruction.Status,
                    instruction.IdempotencyKey,
                    instruction.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return (row.ToEntity(), row.AlreadyApplied);
    }

    public async Task<InvestmentInstruction> UpsertAsync(
        InvestmentInstruction instruction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(instruction);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<InvestmentInstruction>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("InvestmentInstruction"),
                new
                {
                    instruction.Id,
                    instruction.CustomerId,
                    instruction.TradingAccountId,
                    instruction.InvestmentAccountId,
                    instruction.AssetSymbol,
                    instruction.Quantity,
                    instruction.CashAmount,
                    instruction.Currency,
                    Side = (int)instruction.Side,
                    Status = (int)instruction.Status,
                    instruction.OrderId,
                    instruction.IdempotencyKey,
                    instruction.CreatedUtc,
                    instruction.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<bool> SetOrderIdAsync(
        Guid instructionId,
        Guid orderId,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            var row = await connection.QuerySingleOrDefaultAsync<InvestmentInstruction>(
                new CommandDefinition(
                    "SetInvestmentInstructionOrderId",
                    new { Id = instructionId, OrderId = orderId, ChangedBy = changedBy },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
            return row is not null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateStatusAsync(
        Guid instructionId,
        InvestmentInstructionStatus status,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            var row = await connection.QuerySingleOrDefaultAsync<InvestmentInstruction>(
                new CommandDefinition(
                    "UpdateInvestmentInstructionStatus",
                    new { Id = instructionId, Status = (int)status, ChangedBy = changedBy },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
            return row is not null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<decimal> GetPendingCashAmountAsync(
        Guid tradingAccountId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        return await connection.QuerySingleAsync<decimal>(
            new CommandDefinition(
                "get_InvestmentInstruction_PendingCashByTradingAccount_f",
                new { TradingAccountId = tradingAccountId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    private sealed class InvestmentInstructionMutationRow
    {
        public Guid Id { get; init; }
        public int CustomerId { get; init; }
        public Guid TradingAccountId { get; init; }
        public Guid InvestmentAccountId { get; init; }
        public string AssetSymbol { get; init; } = "";
        public decimal Quantity { get; init; }
        public decimal CashAmount { get; init; }
        public string Currency { get; init; } = "";
        public int Side { get; init; }
        public int Status { get; init; }
        public Guid? OrderId { get; init; }
        public string IdempotencyKey { get; init; } = "";
        public DateTimeOffset CreatedUtc { get; init; }
        public DateTimeOffset DateModified { get; init; }
        public string ChangedBy { get; init; } = "";
        public bool AlreadyApplied { get; init; }

        public InvestmentInstruction ToEntity() => new()
        {
            Id = Id,
            CustomerId = CustomerId,
            TradingAccountId = TradingAccountId,
            InvestmentAccountId = InvestmentAccountId,
            AssetSymbol = AssetSymbol,
            Quantity = Quantity,
            CashAmount = CashAmount,
            Currency = Currency,
            Side = (OrderSide)Side,
            Status = (InvestmentInstructionStatus)Status,
            OrderId = OrderId,
            IdempotencyKey = IdempotencyKey,
            CreatedUtc = CreatedUtc,
            DateModified = DateModified,
            ChangedBy = ChangedBy
        };
    }
}
