namespace FinancePlatform.Services.Positions;

public sealed class PositionMutationResult
{
    public required bool Succeeded { get; init; }

    public bool AlreadyApplied { get; init; }

    public decimal Quantity { get; init; }

    public string? Error { get; init; }

    public static PositionMutationResult Success(decimal quantity, bool alreadyApplied = false) =>
        new() { Succeeded = true, AlreadyApplied = alreadyApplied, Quantity = quantity };

    public static PositionMutationResult Failure(string error) =>
        new() { Succeeded = false, Error = error };
}
