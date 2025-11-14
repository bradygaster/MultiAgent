public class OrderSimulatingWorker(ILogger<OrderSimulatingWorker> logger,
    IOrderGenerator orderGenerator,
    OrderWorkflowDefinition orderWorkflowDefinition,
    ConversationLoop conversation) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var randomOrder = await orderGenerator.GenerateRandomOrder();
            await conversation.ExecuteWorkflowAsync<OrderStatusEvent>(orderWorkflowDefinition, randomOrder);

            // pause between orders
            logger.LogInformation("🕛 Waiting to simulate next order ...");
            await Task.Delay(1000, stoppingToken);
        }
    }
}