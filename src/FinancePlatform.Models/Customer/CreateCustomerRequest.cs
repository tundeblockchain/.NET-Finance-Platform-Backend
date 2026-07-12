namespace FinancePlatform.Models.Customer;

public sealed class CreateCustomerRequest
{
    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public string Currency { get; init; } = "GBP";

    public CustomerAddressRequest? Address { get; init; }
}

public sealed class CustomerAddressRequest
{
    public required string Line1 { get; init; }

    public string? Line2 { get; init; }

    public required string City { get; init; }

    public string? Region { get; init; }

    public required string PostalCode { get; init; }

    public required string Country { get; init; }
}
