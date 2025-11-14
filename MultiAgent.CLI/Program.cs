var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSettings();
builder.AddServices();
var host = builder.Build();

host.MapDefaultEndpoints();

// Start the OrderEventPublisher connection
var eventPublisher = host.Services.GetRequiredService<BaseEventPublisher>();
await eventPublisher.StartAsync();

host.Run();