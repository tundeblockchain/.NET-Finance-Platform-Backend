using FinancePlatform.Models.Entities;

namespace FinancePlatform.Services.Cash;

public sealed class CashMutationResult
{
    public required bool Succeeded { get; init; }

    public bool AlreadyApplied { get; init; }

    public string? Error { get; init; }

    public CashBalance? Balance { get; init; }

    public CashReservation? Reservation { get; init; }

    public static CashMutationResult Success(CashBalance balance, CashReservation? reservation = null) =>
        new() { Succeeded = true, Balance = balance, Reservation = reservation };

    public static CashMutationResult Duplicate(CashBalance balance, CashReservation? reservation = null) =>
        new() { Succeeded = true, AlreadyApplied = true, Balance = balance, Reservation = reservation };

    public static CashMutationResult Fail(string error) =>
        new() { Succeeded = false, Error = error };
}
