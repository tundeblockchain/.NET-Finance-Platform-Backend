using System.Text.Json;
using FinancePlatform.Models.Components;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Dtos;
using FinancePlatform.Models.Enums;
using FinancePlatform.Models.Trade;
using FinancePlatform.Models.Triggers;

namespace FinancePlatform.Services.Customer;

/// <summary>
/// Main customer component service: provision customers, deposit to customer account,
/// distribute (park) to trading account per distribution agreement.
/// </summary>
public sealed class CustomerService(ICustomerDirectory directory) : ICustomerService
{
    public CustomerProvisioningResult CreateCustomer(CreateCustomerRequest request) =>
        directory.CreateCustomer(request);

    public CustomerProvisioningResult? GetCustomer(int customerId, string? currency = null)
    {
        var customer = directory.FindCustomer(customerId);
        if (customer is null)
        {
            return null;
        }

        var ccy = string.IsNullOrWhiteSpace(currency) ? "GBP" : currency.ToUpperInvariant();
        var customerAccount = directory.FindCustomerAccountByCustomer(customerId, ccy)
            ?? throw new InvalidOperationException($"Customer {customerId} has no customer account for {ccy}.");
        var tradingAccount = directory.FindTradingAccountByCustomer(customerId, ccy)
            ?? throw new InvalidOperationException($"Customer {customerId} has no trading account for {ccy}.");
        var agreement = directory.FindAgreementByOwnerAccount(customerAccount.Id)
            ?? throw new InvalidOperationException($"Customer {customerId} has no distribution agreement.");

        return new CustomerProvisioningResult
        {
            Customer = customer,
            Address = directory.FindAddress(customerId),
            CustomerAccount = customerAccount,
            TradingAccount = tradingAccount,
            DistributionAgreement = agreement
        };
    }

    public ComponentOperationResult DepositMoney(TriggerContext context, CustomerDepositRequest request)
    {
        if (request.Amount <= 0)
        {
            return ComponentOperationResult.Failure("Deposit amount must be positive.");
        }

        var account = directory.FindCustomerAccount(request.CustomerAccountId);
        if (account is null || account.CustomerId != request.CustomerId)
        {
            return ComponentOperationResult.Failure("Customer account was not found.");
        }

        if (!string.Equals(account.Currency, request.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return ComponentOperationResult.Failure("Currency mismatch for customer account.");
        }

        var credited = directory.TryCreditCustomerAccount(
            account.Id,
            request.Amount,
            context.TriggerId,
            $"{context.IdempotencyKey}:customer-deposit");

        if (!credited)
        {
            return ComponentOperationResult.Failure("Unable to credit customer account.");
        }

        return ComponentOperationResult.Success(
            resultJson: $$"""{"status":"customer-deposited","customerAccountId":"{{account.Id}}","amount":{{request.Amount}}}""");
    }

    public ComponentOperationResult DistributeMoney(
        TriggerContext context,
        DistributeMoneyRequest request,
        string rawPayloadJson)
    {
        _ = rawPayloadJson;
        var amount = request.EffectiveAmount;
        if (amount <= 0)
        {
            return ComponentOperationResult.Failure("Distribute amount must be positive.");
        }

        var customerAccount = directory.FindCustomerAccount(request.CustomerAccountId);
        if (customerAccount is null || customerAccount.CustomerId != request.CustomerId)
        {
            return ComponentOperationResult.Failure("Customer account was not found.");
        }

        var elements = directory.GetActiveElements(customerAccount.Id);
        var tradingElement = elements.FirstOrDefault(e =>
            e.TargetType == DistributionTargetType.TradingAccount);
        if (tradingElement is null)
        {
            return ComponentOperationResult.Failure(
                "No active distribution element to TradingAccount (702) for this customer account.");
        }

        var tradingAccountId = request.TradingAccountId == Guid.Empty
            ? tradingElement.TargetAccountId
            : request.TradingAccountId;

        if (tradingAccountId != tradingElement.TargetAccountId)
        {
            return ComponentOperationResult.Failure("Trading account does not match distribution agreement.");
        }

        var tradingAccount = directory.FindTradingAccount(tradingAccountId);
        if (tradingAccount is null || tradingAccount.CustomerId != request.CustomerId)
        {
            return ComponentOperationResult.Failure("Trading account was not found.");
        }

        var debited = directory.TryDebitCustomerAccount(
            customerAccount.Id,
            amount,
            context.TriggerId,
            $"{context.IdempotencyKey}:customer-debit");

        if (!debited)
        {
            return ComponentOperationResult.Failure("Insufficient funds in customer account.");
        }

        var receivePayload = JsonSerializer.Serialize(new TradingReceiveMoneyRequest
        {
            CustomerId = request.CustomerId,
            TradingAccountId = tradingAccountId,
            SourceCustomerAccountId = customerAccount.Id,
            Amount = amount,
            Currency = request.Currency,
            ParkOnly = true
        });

        return ComponentOperationResult.Success(
            resultJson: """{"status":"customer-distributed","parkOnly":true}""",
            nextTriggers:
            [
                new NextTriggerSpec
                {
                    TriggerCode = TriggerCodes.TradingReceiveMoney,
                    QueueName = QueueNames.Trading,
                    TargetComponent = "Trading",
                    PayloadJson = receivePayload,
                    IdempotencyKey = $"{context.IdempotencyKey}:7001"
                }
            ]);
    }
}
