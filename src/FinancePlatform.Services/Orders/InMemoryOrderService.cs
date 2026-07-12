using System.Collections.Concurrent;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Orders;

public sealed class InMemoryOrderService : IOrderService
{
    private readonly ConcurrentDictionary<string, Order> _ordersByKey = new(StringComparer.Ordinal);

    public OrderSubmitResult TrySubmit(
        string idempotencyKey,
        Guid accountId,
        Guid triggerId,
        Guid? allocationRequestId,
        string assetSymbol,
        OrderSide side,
        decimal quantity,
        decimal? limitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetSymbol);
        if (quantity <= 0)
        {
            return OrderSubmitResult.Failure("Order quantity must be positive.");
        }

        if (_ordersByKey.TryGetValue(idempotencyKey, out var existing))
        {
            return OrderSubmitResult.Success(existing, alreadyApplied: true);
        }

        var now = DateTimeOffset.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            AllocationRequestId = allocationRequestId,
            TriggerId = triggerId,
            AssetSymbol = assetSymbol,
            Side = side,
            Quantity = quantity,
            LimitPrice = limitPrice,
            Status = OrderStatus.Filled,
            IdempotencyKey = idempotencyKey,
            CreatedUtc = now,
            SubmittedUtc = now,
            DateModified = now,
            ChangedBy = ChangeActors.Broker
        };

        if (!_ordersByKey.TryAdd(idempotencyKey, order))
        {
            return OrderSubmitResult.Success(_ordersByKey[idempotencyKey], alreadyApplied: true);
        }

        return OrderSubmitResult.Success(order);
    }

    public int OrderCount => _ordersByKey.Count;

    public Order? FindByIdempotencyKey(string idempotencyKey) =>
        _ordersByKey.TryGetValue(idempotencyKey, out var order) ? order : null;
}
