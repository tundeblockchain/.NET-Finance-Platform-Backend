using FinancePlatform.Api.Contracts;
using FinancePlatform.Api.Controllers;
using FinancePlatform.Models.Customer;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Customer;
using FinancePlatform.Services.Workflows;
using FinancePlatform.UnitTests.Api.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace FinancePlatform.UnitTests.Api;

public class CustomersControllerTests
{
    [Fact]
    public void Create_returns_created_customer_response()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer();
        var customers = Substitute.For<ICustomerService>();
        customers.CreateCustomer(Arg.Any<CreateCustomerRequest>()).Returns(provisioned);
        var workflows = Substitute.For<IWorkflowEnqueueService>();
        var controller = new CustomersController(customers, workflows);

        var result = controller.Create(new CreateCustomerHttpRequest(
            "trader@example.com",
            "Test",
            "Trader",
            "GBP"));

        var created = result.Result.Should().BeOfType<CreatedResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Location.Should().Be($"/api/customers/{provisioned.Customer.Id}");
        var body = created.Value.Should().BeOfType<CustomerResponse>().Subject;
        body.Email.Should().Be("trader@example.com");
        body.CustomerAccount.Id.Should().Be(provisioned.CustomerAccount.Id);
        body.TradingAccount.Id.Should().Be(provisioned.TradingAccount.Id);
    }

    [Fact]
    public void Get_returns_not_found_when_customer_missing()
    {
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(99).Returns((CustomerProvisioningResult?)null);
        var controller = new CustomersController(customers, Substitute.For<IWorkflowEnqueueService>());

        var result = controller.Get(99);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public void Get_returns_ok_when_customer_exists()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer(customerSettled: 250m);
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);
        var controller = new CustomersController(customers, Substitute.For<IWorkflowEnqueueService>());

        var result = controller.Get(1);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeOfType<CustomerResponse>().Subject;
        body.CustomerAccount.Settled.Should().Be(250m);
    }

    [Fact]
    public async Task DepositFunds_returns_not_found_when_customer_missing()
    {
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(5).Returns((CustomerProvisioningResult?)null);
        var controller = new CustomersController(customers, Substitute.For<IWorkflowEnqueueService>());

        var result = await controller.DepositFunds(
            5,
            new CustomerDepositHttpRequest(100m, "dep-1"),
            CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DepositFunds_enqueues_6001_and_returns_accepted()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer();
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);

        var trigger = ApiTestFixtures.CreateTrigger(TriggerCodes.CustomerDepositMoney, QueueNames.Customer, "dep-1");
        var workflows = Substitute.For<IWorkflowEnqueueService>();
        workflows.EnqueueCustomerDepositAsync(Arg.Any<CustomerDepositWorkflowCommand>(), Arg.Any<CancellationToken>())
            .Returns(trigger);

        var controller = new CustomersController(customers, workflows);

        var result = await controller.DepositFunds(
            1,
            new CustomerDepositHttpRequest(100m, "dep-1"),
            CancellationToken.None);

        var accepted = result.Result.Should().BeOfType<AcceptedResult>().Subject;
        accepted.StatusCode.Should().Be(202);
        var body = accepted.Value.Should().BeOfType<WorkflowAcceptedResponse>().Subject;
        body.TriggerCode.Should().Be(TriggerCodes.CustomerDepositMoney);
        body.QueueName.Should().Be(QueueNames.Customer);

        await workflows.Received(1).EnqueueCustomerDepositAsync(
            Arg.Is<CustomerDepositWorkflowCommand>(c =>
                c.CustomerId == 1
                && c.CustomerAccountId == provisioned.CustomerAccount.Id
                && c.Amount == 100m
                && c.IdempotencyKey == "dep-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransferFundsToTrading_enqueues_6002_and_returns_accepted()
    {
        var provisioned = ApiTestFixtures.CreateProvisionedCustomer(customerSettled: 500m);
        var customers = Substitute.For<ICustomerService>();
        customers.GetCustomer(1).Returns(provisioned);

        var trigger = ApiTestFixtures.CreateTrigger(TriggerCodes.CustomerDistributeMoney, QueueNames.Customer, "xfer-1");
        var workflows = Substitute.For<IWorkflowEnqueueService>();
        workflows.EnqueueCustomerDistributeAsync(Arg.Any<CustomerDistributeWorkflowCommand>(), Arg.Any<CancellationToken>())
            .Returns(trigger);

        var controller = new CustomersController(customers, workflows);

        var result = await controller.TransferFundsToTrading(
            1,
            new CustomerDistributeHttpRequest(200m, "xfer-1"),
            CancellationToken.None);

        var accepted = result.Result.Should().BeOfType<AcceptedResult>().Subject;
        var body = accepted.Value.Should().BeOfType<WorkflowAcceptedResponse>().Subject;
        body.TriggerCode.Should().Be(TriggerCodes.CustomerDistributeMoney);

        await workflows.Received(1).EnqueueCustomerDistributeAsync(
            Arg.Is<CustomerDistributeWorkflowCommand>(c =>
                c.CustomerId == 1
                && c.CustomerAccountId == provisioned.CustomerAccount.Id
                && c.TradingAccountId == provisioned.TradingAccount.Id
                && c.Amount == 200m),
            Arg.Any<CancellationToken>());
    }
}
