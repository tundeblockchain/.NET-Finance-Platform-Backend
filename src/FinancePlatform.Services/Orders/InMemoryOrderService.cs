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
        decimal? limitPrice,
        decimal? fillPrice = null,
        string? externalOrderId = null,
        string? provider = null,
        DateTimeOffset? filledUtc = null)
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
            FillPrice = fillPrice,
            ExternalOrderId = externalOrderId,
            Provider = provider,
            Status = OrderStatus.Filled,
            IdempotencyKey = idempotencyKey,
            CreatedUtc = now,
            SubmittedUtc = now,
            FilledUtc = filledUtc ?? now,
            DateModified = now,
            ChangedBy = ChangeActors.Broker
        };

        if (!_ordersByKey.TryAdd(idempotencyKey, order))
        {
            return OrderSubmitResult.Success(_ordersByKey[idempotencyKey], alreadyApplied: true);
        }

        return OrderSubmitResult.Success(order);
    }

    public OrderSubmitResult TryCreate(
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
            Status = OrderStatus.Submitted,
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

    public Order? GetById(Guid orderId) =>
        _ordersByKey.Values.FirstOrDefault(o => o.Id == orderId) is { } order ? Clone(order) : null;

    public bool TryMarkFilled(Guid orderId)
    {
        var order = _ordersByKey.Values.FirstOrDefault(o => o.Id == orderId);
        if (order is null)
        {
            return false;
        }

        order.Status = OrderStatus.Filled;
        order.DateModified = DateTimeOffset.UtcNow;
        return true;
    }

    public int OrderCount => _ordersByKey.Count;

    public Order? FindByIdempotencyKey(string idempotencyKey) =>
        _ordersByKey.TryGetValue(idempotencyKey, out var order) ? order : null;

    public IReadOnlyList<Order> GetByAccount(Guid accountId) =>
        _ordersByKey.Values
            .Where(o => o.AccountId == accountId)
            .OrderByDescending(o => o.CreatedUtc)
            .Select(Clone)
            .ToArray();

    private static Order Clone(Order o) => new()
    {
        Id = o.Id,
        AccountId = o.AccountId,
        AllocationRequestId = o.AllocationRequestId,
        TriggerId = o.TriggerId,
        AssetSymbol = o.AssetSymbol,
        Side = o.Side,
        Quantity = o.Quantity,
        LimitPrice = o.LimitPrice,
        FillPrice = o.FillPrice,
        ExternalOrderId = o.ExternalOrderId,
        Provider = o.Provider,
        Status = o.Status,
        IdempotencyKey = o.IdempotencyKey,
        CreatedUtc = o.CreatedUtc,
        SubmittedUtc = o.SubmittedUtc,
        FilledUtc = o.FilledUtc,
        DateModified = o.DateModified,
        ChangedBy = o.ChangedBy
    };
}
