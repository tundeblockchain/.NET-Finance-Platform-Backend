using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Data.DataLayer;

public interface IOrderRepository
{
    Task<Order?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<(Order Order, bool AlreadyApplied)> CreateAsync(
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
        decimal? fillPrice = null,
        string? externalOrderId = null,
        string? provider = null,
        DateTimeOffset? filledUtc = null,
        CancellationToken cancellationToken = default);

    Task<Order?> MarkFilledAsync(
        Guid orderId,
        string changedBy,
        decimal? fillPrice = null,
        string? externalOrderId = null,
        string? provider = null,
        DateTimeOffset? filledUtc = null,
        CancellationToken cancellationToken = default);

    Task<Order> UpsertAsync(Order entity, CancellationToken cancellationToken = default);
}
