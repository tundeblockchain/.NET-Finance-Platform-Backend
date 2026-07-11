using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Finance Platform API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapGet("/", () => Results.Ok(new
{
    service = "FinancePlatform.Api",
    status = "ready",
    phase = 1,
    docs = "/scalar"
}))
.WithName("GetServiceInfo")
.WithTags("Meta")
.WithSummary("Service info")
.WithDescription("Returns basic service metadata including the current build phase.");

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
