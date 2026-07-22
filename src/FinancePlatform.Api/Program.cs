using FinancePlatform.Services;
using FinancePlatform.Services.Configuration;
using Scalar.AspNetCore;

EnvFileLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddTriggerEngine(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Finance Platform API")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program;
