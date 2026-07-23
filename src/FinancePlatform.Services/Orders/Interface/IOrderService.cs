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

    OrderSubmitResult TryCreate(
        string idempotencyKey,
        Guid accountId,
        Guid triggerId,
        Guid? allocationRequestId,
        string assetSymbol,
        OrderSide side,
        decimal quantity,
        decimal? limitPrice);

    Order? GetById(Guid orderId);

    /// <summary>
    /// Marks a previously created (Submitted) order as Filled with broker execution details.
    /// Idempotent when the order is already filled.
    /// </summary>
    OrderSubmitResult TryFill(
        Guid orderId,
        decimal fillPrice,
        string? externalOrderId = null,
        string? provider = null,
        DateTimeOffset? filledUtc = null);

    IReadOnlyList<Order> GetByAccount(Guid accountId);
}
