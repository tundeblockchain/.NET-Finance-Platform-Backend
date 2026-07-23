using FinancePlatform.Data.DataLayer;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Orders;

public sealed class SqlOrderService(IOrderRepository orderRepository) : IOrderService
{
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
        DateTimeOffset? filledUtc = null) =>
        Create(
            idempotencyKey,
            accountId,
            triggerId,
            allocationRequestId,
            assetSymbol,
            side,
            quantity,
            limitPrice,
            OrderStatus.Filled,
            fillPrice,
            externalOrderId,
            provider,
            filledUtc);

    public OrderSubmitResult TryCreate(
        string idempotencyKey,
        Guid accountId,
        Guid triggerId,
        Guid? allocationRequestId,
        string assetSymbol,
        OrderSide side,
        decimal quantity,
        decimal? limitPrice) =>
        Create(
            idempotencyKey,
            accountId,
            triggerId,
            allocationRequestId,
            assetSymbol,
            side,
            quantity,
            limitPrice,
            OrderStatus.Submitted);

    public Order? FindByIdempotencyKey(string idempotencyKey) =>
        orderRepository.GetByIdempotencyKeyAsync(idempotencyKey).GetAwaiter().GetResult();

    public Order? GetById(Guid orderId) =>
        orderRepository.GetAsync(orderId).GetAwaiter().GetResult();

    public OrderSubmitResult TryFill(
        Guid orderId,
        decimal fillPrice,
        string? externalOrderId = null,
        string? provider = null,
        DateTimeOffset? filledUtc = null)
    {
        try
        {
            var order = orderRepository
                .MarkFilledAsync(
                    orderId,
                    ChangeActors.Broker,
                    fillPrice,
                    externalOrderId,
                    provider,
                    filledUtc)
                .GetAwaiter()
                .GetResult();

            if (order is null)
            {
                return OrderSubmitResult.Failure("Order was not found or could not be filled.");
            }

            return OrderSubmitResult.Success(order, alreadyApplied: order.Status == OrderStatus.Filled);
        }
        catch (Exception ex)
        {
            return OrderSubmitResult.Failure(RootMessage(ex));
        }
    }

    public IReadOnlyList<Order> GetByAccount(Guid accountId) =>
        orderRepository.GetByAccountAsync(accountId).GetAwaiter().GetResult();

    private OrderSubmitResult Create(
        string idempotencyKey,
        Guid accountId,
        Guid triggerId,
        Guid? allocationRequestId,
        string assetSymbol,
        OrderSide side,
        decimal quantity,
        decimal? limitPrice,
        OrderStatus status,
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

        try
        {
            var (order, alreadyApplied) = orderRepository
                .CreateAsync(
                    idempotencyKey,
                    accountId,
                    triggerId,
                    allocationRequestId,
                    assetSymbol,
                    side,
                    quantity,
                    limitPrice,
                    status,
                    ChangeActors.Broker,
                    fillPrice,
                    externalOrderId,
                    provider,
                    filledUtc)
                .GetAwaiter()
                .GetResult();

            return OrderSubmitResult.Success(order, alreadyApplied);
        }
        catch (Exception ex)
        {
            return OrderSubmitResult.Failure(RootMessage(ex));
        }
    }

    private static string RootMessage(Exception ex)
    {
        while (ex.InnerException is not null)
        {
            ex = ex.InnerException;
        }

        return ex.Message;
    }
}
