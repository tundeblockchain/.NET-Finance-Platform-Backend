using System.Data;
using Dapper;
using FinancePlatform.Data.Sql;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Data.DataLayer;

public sealed class OrderRepository(IDbConnectionFactory connectionFactory) : IOrderRepository
{
    public async Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(
                SqlObjectNames.GetProc("Order"),
                new { Id = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(
                "get_Order_ByIdempotencyKey_f",
                new { IdempotencyKey = idempotencyKey },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Order>> GetByAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<Order>(
            new CommandDefinition(
                "get_Order_ByAccountId_f",
                new { AccountId = accountId },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
        return rows.ToArray();
    }

    public async Task<(Order Order, bool AlreadyApplied)> CreateAsync(
        string idempotencyKey,
        Guid accountId,
        Guid triggerId,
        Guid? allocationRequestId,
        string assetSymbol,
        OrderSide side,
        decimal quantity,
        decimal? limitPrice,
        OrderStatus status,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        var procedure = status == OrderStatus.Filled ? "SubmitOrder" : "CreateOrder";

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var row = await connection.QuerySingleAsync<OrderWithFlag>(
            new CommandDefinition(
                procedure,
                new
                {
                    IdempotencyKey = idempotencyKey,
                    AccountId = accountId,
                    TriggerId = triggerId,
                    AllocationRequestId = allocationRequestId,
                    AssetSymbol = assetSymbol,
                    Side = (int)side,
                    Quantity = quantity,
                    LimitPrice = limitPrice,
                    ChangedBy = changedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));

        return (row.ToOrder(), row.AlreadyApplied);
    }

    public async Task<bool> MarkFilledAsync(
        Guid orderId,
        string changedBy,
        CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            var order = await connection.QuerySingleOrDefaultAsync<Order>(
                new CommandDefinition(
                    "MarkOrderFilled",
                    new { Id = orderId, ChangedBy = changedBy },
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken));
            return order is not null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Order> UpsertAsync(Order entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<Order>(
            new CommandDefinition(
                SqlObjectNames.UpsertProc("Order"),
                new
                {
                    entity.Id,
                    entity.AccountId,
                    entity.AllocationRequestId,
                    entity.TriggerId,
                    entity.AssetSymbol,
                    Side = (int)entity.Side,
                    entity.Quantity,
                    entity.LimitPrice,
                    Status = (int)entity.Status,
                    entity.IdempotencyKey,
                    entity.CreatedUtc,
                    entity.SubmittedUtc,
                    entity.ChangedBy
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken));
    }

    private sealed class OrderWithFlag
    {
        public Guid Id { get; init; }
        public Guid AccountId { get; init; }
        public Guid? AllocationRequestId { get; init; }
        public Guid TriggerId { get; init; }
        public string AssetSymbol { get; init; } = "";
        public int Side { get; init; }
        public decimal Quantity { get; init; }
        public decimal? LimitPrice { get; init; }
        public int Status { get; init; }
        public string IdempotencyKey { get; init; } = "";
        public DateTimeOffset CreatedUtc { get; init; }
        public DateTimeOffset? SubmittedUtc { get; init; }
        public DateTimeOffset DateModified { get; init; }
        public string ChangedBy { get; init; } = "";
        public bool AlreadyApplied { get; init; }

        public Order ToOrder() => new()
        {
            Id = Id,
            AccountId = AccountId,
            AllocationRequestId = AllocationRequestId,
            TriggerId = TriggerId,
            AssetSymbol = AssetSymbol,
            Side = (OrderSide)Side,
            Quantity = Quantity,
            LimitPrice = LimitPrice,
            Status = (OrderStatus)Status,
            IdempotencyKey = IdempotencyKey,
            CreatedUtc = CreatedUtc,
            SubmittedUtc = SubmittedUtc,
            DateModified = DateModified,
            ChangedBy = ChangedBy
        };
    }
}
