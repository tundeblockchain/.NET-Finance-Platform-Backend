using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Orders;

public sealed class OrderSubmitResult
{
    public required bool Succeeded { get; init; }

    public bool AlreadyApplied { get; init; }

    public Order? Order { get; init; }

    public string? Error { get; init; }

    public static OrderSubmitResult Success(Order order, bool alreadyApplied = false) =>
        new() { Succeeded = true, AlreadyApplied = alreadyApplied, Order = order };

    public static OrderSubmitResult Failure(string error) =>
        new() { Succeeded = false, Error = error };
}
