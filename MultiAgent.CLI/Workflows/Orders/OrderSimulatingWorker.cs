public class OrderSimulatingWorker(ILogger<OrderSimulatingWorker> logger,
    ConversationLoop conversation,
    IOrderGenerator orderGenerator) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var randomOrder = await orderGenerator.GenerateRandomOrder();
            await conversation.SendPrompt(randomOrder);

            logger.LogInformation("🕛 Waiting to simulate next order ...");

            // Simulate order processing logic here
            await Task.Delay(1000, stoppingToken);
        }
    }
}