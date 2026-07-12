using FinancePlatform.Api.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace FinancePlatform.UnitTests.Api;

public class MetaControllerTests
{
    [Fact]
    public void GetServiceInfo_returns_ok_with_docs_path()
    {
        var controller = new MetaController();

        var result = controller.GetServiceInfo();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().NotBeNull();
        ok.Value!.ToString().Should().Contain("FinancePlatform.Api");
    }
}
