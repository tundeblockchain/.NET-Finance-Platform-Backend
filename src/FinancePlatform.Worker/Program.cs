using FinancePlatform.Services;
using FinancePlatform.Services.Triggers;
using FinancePlatform.Worker;
using FinancePlatform.Worker.EventProcessors;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BrokerOptions>(builder.Configuration.GetSection(BrokerOptions.SectionName));
builder.Services.Configure<TriggerRetryOptions>(builder.Configuration.GetSection(TriggerRetryOptions.SectionName));
builder.Services.AddTriggerEngine(builder.Configuration);

builder.Services.AddSingleton<ITriggerEventProcessor, CashEP>();
builder.Services.AddSingleton<ITriggerEventProcessor, CustomerEP>();
builder.Services.AddSingleton<ITriggerEventProcessor, TradeEP>();
builder.Services.AddSingleton<ITriggerEventProcessor, InvestmentEP>();
builder.Services.AddSingleton<ITriggerEventProcessor, AssetEP>();

var brokerOptions = builder.Configuration.GetSection(BrokerOptions.SectionName).Get<BrokerOptions>()
    ?? new BrokerOptions();

foreach (var queue in brokerOptions.Queues)
{
    var captured = queue;
    builder.Services.AddSingleton<IHostedService>(sp =>
        new QueueWorkerHostedService(
            sp.GetRequiredService<ILogger<QueueWorkerHostedService>>(),
            sp.GetRequiredService<TriggerClaimService>(),
            sp.GetRequiredService<TriggerExecutionService>(),
            sp.GetRequiredService<TriggerHeartbeatService>(),
            sp.GetRequiredService<IOptions<BrokerOptions>>(),
            captured));
}

builder.Services.AddHostedService<SampleWorkflowHostedService>();

var host = builder.Build();
host.Run();
