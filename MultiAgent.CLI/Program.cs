using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSettings();
builder.AddServices();
builder.Services.AddHostedService<OrderSimulator>();

var host = builder.Build();
host.Run();

public class OrderSimulator(ILogger<OrderSimulator> logger,
    ConversationLoop conversation) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await conversation.SubmitRandomOrder();

            logger.LogInformation("Waiting to simulate next order ...");

            // Simulate order processing logic here
            await Task.Delay(5000, stoppingToken);
        }
    }
}