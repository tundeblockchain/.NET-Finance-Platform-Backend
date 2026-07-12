namespace FinancePlatform.Models.Customer;

public sealed class CustomerDepositRequest
{
    public int CustomerId { get; set; }

    public Guid CustomerAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "GBP";
}
