using FinancePlatform.Data.DataLayer;
using FinancePlatform.Models;
using FinancePlatform.Models.Entities;
using FinancePlatform.Models.Enums;

namespace FinancePlatform.Services.Ledger;

public sealed class SqlLedgerService(ILedgerEntryRepository ledgerRepository) : ILedgerService
{
    public int EntryCount =>
        ledgerRepository.CountAsync().GetAwaiter().GetResult();

    public LedgerEntry? FindByIdempotencyKey(string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        return ledgerRepository.GetByIdempotencyKeyAsync(idempotencyKey).GetAwaiter().GetResult();
    }

    public LedgerPostResult TryPost(
        string idempotencyKey,
        Guid accountId,
        string currency,
        decimal amount,
        LedgerEntryType entryType,
        string description,
        Guid? triggerId,
        Guid? allocationRequestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (amount <= 0)
        {
            return LedgerPostResult.Fail("Ledger amount must be positive.");
        }

        try
        {
            var (entry, alreadyApplied) = ledgerRepository
                .CreateAsync(
                    Guid.NewGuid(),
                    accountId,
                    triggerId,
                    allocationRequestId,
                    entryType,
                    amount,
                    currency.ToUpperInvariant(),
                    idempotencyKey,
                    description,
                    ChangeActors.Broker)
                .GetAwaiter()
                .GetResult();

            return alreadyApplied
                ? LedgerPostResult.Duplicate(entry)
                : LedgerPostResult.Success(entry);
        }
        catch (Exception ex)
        {
            return LedgerPostResult.Fail(RootMessage(ex));
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
