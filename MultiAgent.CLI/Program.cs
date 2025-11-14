using MultiAgent.CLI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSettings();
builder.AddServices();
builder.Services.AddHostedService<OrderSimulatingWorker>();

var host = builder.Build();

host.MapDefaultEndpoints();

host.Run();