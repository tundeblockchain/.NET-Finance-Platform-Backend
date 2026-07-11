using FinancePlatform.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BrokerOptions>(builder.Configuration.GetSection(BrokerOptions.SectionName));
builder.Services.AddHostedService<BrokerHostedService>();

var host = builder.Build();
host.Run();
