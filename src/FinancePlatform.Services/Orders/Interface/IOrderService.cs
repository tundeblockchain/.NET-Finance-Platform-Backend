using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Orders;

public interface IOrderService
{
    /// <summary>
    /// Creates and submits an order. Idempotent by key. In-memory fills immediately.
    /// </summary>
    OrderSubmitResult TrySubmit(
        string idempotencyKey,
        Guid accountId,
        Guid triggerId,
        Guid? allocationRequestId,
        string assetSymbol,
        OrderSide side,
        decimal quantity,
        decimal? limitPrice);
}
