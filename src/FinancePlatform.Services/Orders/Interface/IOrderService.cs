using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Orders;

public interface IOrderService
{
    /// <summary>
    /// Creates and records an order. Idempotent by key.
    /// </summary>
    OrderSubmitResult TrySubmit(
        string idempotencyKey,
        Guid accountId,
        Guid triggerId,
        Guid? allocationRequestId,
        string assetSymbol,
        OrderSide side,
        decimal quantity,
        decimal? limitPrice,
        decimal? fillPrice = null,
        string? externalOrderId = null,
        string? provider = null,
        DateTimeOffset? filledUtc = null);

    Order? FindByIdempotencyKey(string idempotencyKey);

    IReadOnlyList<Order> GetByAccount(Guid accountId);
}
