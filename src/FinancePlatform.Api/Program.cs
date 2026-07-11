var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new
{
    service = "FinancePlatform.Api",
    status = "ready",
    phase = 0
}));

app.MapHealthChecks("/health");

app.Run();

public partial class Program;
