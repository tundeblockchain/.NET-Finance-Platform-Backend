namespace FinancePlatform.Api.Contracts;

public sealed record CreateCustomerHttpRequest(
    string Email,
    string FirstName,
    string LastName,
    string? Currency = null,
    AddressHttpRequest? Address = null);

public sealed record AddressHttpRequest(
    string Line1,
    string City,
    string PostalCode,
    string Country,
    string? Line2 = null,
    string? Region = null);

public sealed record CustomerDepositHttpRequest(
    decimal Amount,
    string IdempotencyKey,
    Guid? CustomerAccountId = null,
    string? Currency = null,
    Guid? RootWorkflowId = null);

public sealed record CustomerDistributeHttpRequest(
    decimal Amount,
    string IdempotencyKey,
    Guid? CustomerAccountId = null,
    Guid? TradingAccountId = null,
    string? Currency = null,
    Guid? RootWorkflowId = null);

public sealed record CustomerResponse(
    int Id,
    string Email,
    string FirstName,
    string LastName,
    AddressResponse? Address,
    AccountBalanceResponse CustomerAccount,
    AccountBalanceResponse TradingAccount,
    Guid DistributionAgreementId);

public sealed record AddressResponse(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string PostalCode,
    string Country);

public sealed record AccountBalanceResponse(
    Guid Id,
    int CustomerId,
    string Currency,
    decimal Settled,
    decimal Reserved,
    decimal Available);

public sealed record DepositRequest(
    Guid AccountId,
    decimal Amount,
    string IdempotencyKey,
    string? Currency = null,
    string? AssetSymbol = null,
    decimal Quantity = 1m,
    Guid? RootWorkflowId = null);

public sealed record BuyRequest(
    Guid AccountId,
    string AssetSymbol,
    decimal Quantity,
    decimal CashAmount,
    string IdempotencyKey,
    string? Currency = null,
    Guid? RootWorkflowId = null,
    Guid? AllocationRequestId = null);

public sealed record SellRequest(
    Guid AccountId,
    string AssetSymbol,
    decimal Quantity,
    decimal CashAmount,
    string IdempotencyKey,
    string? Currency = null,
    Guid? RootWorkflowId = null,
    Guid? AllocationRequestId = null);

public sealed record TradingOrderRequest(
    string AssetSymbol,
    decimal Quantity,
    decimal CashAmount,
    string IdempotencyKey,
    string? Currency = null,
    Guid? TradingAccountId = null,
    Guid? RootWorkflowId = null);

public sealed record TradingTransferToCustomerHttpRequest(
    decimal Amount,
    string IdempotencyKey,
    Guid? TradingAccountId = null,
    Guid? CustomerAccountId = null,
    string? Currency = null,
    Guid? RootWorkflowId = null);

public sealed record TradingFundsResponse(
    AccountBalanceResponse Cash,
    IReadOnlyList<PositionResponse> Positions);

public sealed record PositionResponse(
    string AssetSymbol,
    decimal Quantity);

public sealed record TradeHistoryItemResponse(
    Guid OrderId,
    Guid TradingAccountId,
    string AssetSymbol,
    string Side,
    decimal Quantity,
    decimal? LimitPrice,
    string Status,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? SubmittedUtc);

public sealed record WorkflowAcceptedResponse(
    Guid TriggerId,
    Guid RootWorkflowId,
    int TriggerCode,
    string QueueName);
