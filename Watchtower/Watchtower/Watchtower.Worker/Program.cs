using Watchtower.Application;
using Watchtower.Infrastructure;
using Watchtower.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpClient("watcher")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5,
    });

builder.Services.AddSingleton<EndpointHttpChecker>();
builder.Services.AddHostedService<CheckSchedulerService>();

var host = builder.Build();
host.Run();
