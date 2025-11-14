using MultiAgent.CLI;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddSettings();
builder.AddServices();
builder.Services.AddHostedService<OrderSimulatingWorker>();

// Add OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(b => b.AddSource("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddInstrumentation<OrderSimulatingWorker>()
    )
    .WithMetrics(b => b.AddMeter("*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
    )
    .WithLogging();

var host = builder.Build();

host.MapDefaultEndpoints();

host.Run();