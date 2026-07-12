namespace FinancePlatform.Services.Cash;

public interface ICashService
{
    /// <summary>
    /// Credits settled cash. Returns false when the idempotency key was already applied.
    /// </summary>
    bool TryDeposit(string idempotencyKey, Guid accountId, decimal amount, string currency);
}
