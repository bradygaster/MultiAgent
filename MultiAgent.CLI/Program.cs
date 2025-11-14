using MultiAgent.CLI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSettings();
builder.AddServices();
builder.Services.AddHostedService<OrderSimulatingWorker>();

var host = builder.Build();

host.MapDefaultEndpoints();

// Start the OrderEventPublisher connection
var eventPublisher = host.Services.GetRequiredService<OrderEventPublisher>();
await eventPublisher.StartAsync();

host.Run();