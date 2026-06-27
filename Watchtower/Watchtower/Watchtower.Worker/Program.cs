using Watchtower.Application;
using Watchtower.Infrastructure;
using Watchtower.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<CheckSchedulerService>();

var host = builder.Build();
host.Run();
