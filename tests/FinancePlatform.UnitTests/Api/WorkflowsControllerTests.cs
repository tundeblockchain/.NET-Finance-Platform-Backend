using FinancePlatform.Api.Contracts;
using FinancePlatform.Api.Controllers;
using FinancePlatform.Models.Triggers;
using FinancePlatform.Services.Workflows;
using FinancePlatform.UnitTests.Api.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace FinancePlatform.UnitTests.Api;

public class WorkflowsControllerTests
{
    [Fact]
    public async Task EnqueueBuy_returns_accepted_with_buy_trigger()
    {
        var trigger = ApiTestFixtures.CreateTrigger(TriggerCodes.BuyAsset, QueueNames.Trading, "legacy-buy");
        var workflows = Substitute.For<IWorkflowEnqueueService>();
        workflows.EnqueueBuyAsync(Arg.Any<BuyWorkflowCommand>(), Arg.Any<CancellationToken>()).Returns(trigger);
        var controller = new WorkflowsController(workflows);
        var accountId = Guid.NewGuid();

        var result = await controller.EnqueueBuy(
            new BuyRequest(accountId, "VWRL", 1m, 50m, "legacy-buy"),
            CancellationToken.None);

        var accepted = result.Result.Should().BeOfType<AcceptedResult>().Subject;
        var body = accepted.Value.Should().BeOfType<WorkflowAcceptedResponse>().Subject;
        body.TriggerCode.Should().Be(TriggerCodes.BuyAsset);
        body.QueueName.Should().Be(QueueNames.Trading);
    }
}
