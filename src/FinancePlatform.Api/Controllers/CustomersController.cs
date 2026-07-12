using FinancePlatform.Api.Contracts;
using FinancePlatform.Models.Customer;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Workflows;
using Microsoft.AspNetCore.Mvc;

namespace FinancePlatform.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Tags("Customers")]
public sealed class CustomersController(
    ICustomerService customerService,
    IWorkflowEnqueueService workflowsService) : ControllerBase
{
    [HttpPost]
    [EndpointName("CreateCustomer")]
    [EndpointSummary("Create customer with accounts")]
    [EndpointDescription("Creates a customer, CustomerAccount, TradingAccount, and park distribution agreement (→ TradingAccount 702).")]
    public ActionResult<CustomerResponse> Create([FromBody] CreateCustomerHttpRequest body)
    {
        var created = customerService.CreateCustomer(new CreateCustomerRequest
        {
            Email = body.Email,
            FirstName = body.FirstName,
            LastName = body.LastName,
            Currency = body.Currency ?? "GBP",
            Address = body.Address is null
                ? null
                : new CustomerAddressRequest
                {
                    Line1 = body.Address.Line1,
                    Line2 = body.Address.Line2,
                    City = body.Address.City,
                    Region = body.Address.Region,
                    PostalCode = body.Address.PostalCode,
                    Country = body.Address.Country
                }
        });

        return Created($"/api/customers/{created.Customer.Id}", CustomerMapper.ToResponse(created));
    }

    [HttpGet("{customerId:int}")]
    [EndpointName("GetCustomer")]
    [EndpointSummary("Get customer and accounts")]
    public ActionResult<CustomerResponse> Get(int customerId)
    {
        var result = customerService.GetCustomer(customerId);
        return result is null ? NotFound() : Ok(CustomerMapper.ToResponse(result));
    }

    [HttpGet("{customerId:int}/customer-account")]
    [EndpointName("GetCustomerAccount")]
    [EndpointSummary("Get customer account balance")]
    public ActionResult<AccountBalanceResponse> GetCustomerAccount(int customerId)
    {
        var result = customerService.GetCustomer(customerId);
        return result is null ? NotFound() : Ok(CustomerMapper.ToCustomerAccountBalance(result.CustomerAccount));
    }

    [HttpGet("{customerId:int}/trading-account")]
    [EndpointName("GetTradingAccount")]
    [EndpointSummary("Get trading account balance (parked funds)")]
    public ActionResult<AccountBalanceResponse> GetTradingAccount(int customerId)
    {
        var result = customerService.GetCustomer(customerId);
        return result is null ? NotFound() : Ok(CustomerMapper.ToTradingAccountBalance(result.TradingAccount));
    }

    [HttpPost("{customerId:int}/deposits")]
    [EndpointName("DepositFunds")]
    [EndpointSummary("Deposit funds")]
    [EndpointDescription("Credits the CustomerAccount (trigger 6001). Transfer to trading separately.")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> DepositFunds(
        int customerId,
        [FromBody] CustomerDepositHttpRequest body,
        CancellationToken ct)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var accountId = body.CustomerAccountId is { } id && id != Guid.Empty
            ? id
            : customer.CustomerAccount.Id;

        var trigger = await workflowsService.EnqueueCustomerDepositAsync(new CustomerDepositWorkflowCommand
        {
            CustomerId = customerId,
            CustomerAccountId = accountId,
            Amount = body.Amount,
            Currency = body.Currency ?? customer.CustomerAccount.Currency,
            IdempotencyKey = body.IdempotencyKey,
            RootWorkflowId = body.RootWorkflowId
        }, ct);

        return Accepted(
            $"/api/workflows/triggers/{trigger.Id}",
            new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
    }

    [HttpPost("{customerId:int}/distribute-to-trading")]
    [EndpointName("TransferFundsToTrading")]
    [EndpointSummary("Transfer funds to trading")]
    [EndpointDescription("Moves funds from CustomerAccount to TradingAccount (6002 → 7001 park).")]
    public async Task<ActionResult<WorkflowAcceptedResponse>> TransferFundsToTrading(
        int customerId,
        [FromBody] CustomerDistributeHttpRequest body,
        CancellationToken ct)
    {
        var customer = customerService.GetCustomer(customerId);
        if (customer is null)
        {
            return NotFound();
        }

        var customerAccountId = body.CustomerAccountId is { } ca && ca != Guid.Empty
            ? ca
            : customer.CustomerAccount.Id;
        var tradingAccountId = body.TradingAccountId is { } ta && ta != Guid.Empty
            ? ta
            : customer.TradingAccount.Id;

        var trigger = await workflowsService.EnqueueCustomerDistributeAsync(new CustomerDistributeWorkflowCommand
        {
            CustomerId = customerId,
            CustomerAccountId = customerAccountId,
            TradingAccountId = tradingAccountId,
            Amount = body.Amount,
            Currency = body.Currency ?? customer.CustomerAccount.Currency,
            IdempotencyKey = body.IdempotencyKey,
            RootWorkflowId = body.RootWorkflowId
        }, ct);

        return Accepted(
            $"/api/workflows/triggers/{trigger.Id}",
            new WorkflowAcceptedResponse(trigger.Id, trigger.RootWorkflowId, trigger.TriggerCode, trigger.QueueName));
    }
}
