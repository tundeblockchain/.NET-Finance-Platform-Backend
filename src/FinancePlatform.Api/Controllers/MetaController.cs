using Microsoft.AspNetCore.Mvc;

namespace FinancePlatform.Api.Controllers;

[ApiController]
[Route("api")]
[Tags("Meta")]
public sealed class MetaController : ControllerBase
{
    [HttpGet("/")]
    [EndpointName("GetServiceInfo")]
    [EndpointSummary("Service info")]
    [EndpointDescription("Returns basic service metadata including the current build phase.")]
    public IActionResult GetServiceInfo() =>
        Ok(new
        {
            service = "FinancePlatform.Api",
            status = "ready",
            phase = 6,
            docs = "/scalar"
        });
}
