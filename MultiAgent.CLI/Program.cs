using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MultiAgent.CLI;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSettings();
builder.AddServices();
builder.Services.AddHostedService<OrderSimulator>();

var host = builder.Build();
host.Run();