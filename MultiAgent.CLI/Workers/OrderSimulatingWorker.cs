public class OrderSimulatingWorker(ILogger<OrderSimulatingWorker> logger,
    ConversationLoop conversation) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await conversation.SubmitRandomOrder();

            logger.LogInformation("🕛 Waiting to simulate next order ...");

            // Simulate order processing logic here
            await Task.Delay(1000, stoppingToken);
        }
    }
}