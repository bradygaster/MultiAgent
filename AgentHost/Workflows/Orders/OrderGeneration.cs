public interface IOrderGenerator
{
    Task<string> GenerateRandomOrder();
}

public class StaticOrderGenerator : IOrderGenerator
{
    public async Task<string> GenerateRandomOrder()
    {
        var randomOrders = new List<string>
        {
            "1 cheeseburger with fries and a chocolate milkshake",
            "2 bacon cheeseburgers, 2 orders of fries, and 2 chocolate milkshakes",
            "2 bacon cheeseburgers, 1 order of regular fries without salt, 1 order of sweet potato fries, and 2 strawberry milkshakes",
            "2 vanilla milkshakes and 1 order of onion rings",
            "1 sundae with whipped cream and a cherry on top",
            "1 bacon cheeseburger with extra cheese, 1 order of fries, and 1 vanilla milkshake with sprinkles",
            "2 cheeseburgers, 1 order of sweet potato fries, and 2 chocolate milkshakes with whipped cream",
            "1 cheeseburger with bacon, 1 order of fries without salt, and 1 strawberry milkshake with a cherry on top",
            "2 bacon cheeseburgers and 5 chocolate milkshakes",
            "2 cheeseburgers with extra cheese, 2 with bacon, 2 orders of sweet potato fries, 1 with no salt",
            "3 hot fudge sundaes",
            "3 hot fudge sundaes with peanuts and whipped cream"
        };

        return randomOrders[new Random().Next(randomOrders.Count)];
    }
}

public class OrderSimulatingWorker(ILogger<OrderSimulatingWorker> logger,
    IOrderGenerator orderGenerator,
    OrderWorkflowDefinition orderWorkflowDefinition,
    WorkflowFactory conversation) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var randomOrder = await orderGenerator.GenerateRandomOrder();
            _ = conversation.ExecuteWorkflowAsync<WorkflowStatusEvent>(orderWorkflowDefinition, randomOrder);

            // pause between orders
            logger.LogInformation("🕛 Waiting to simulate next order ...");
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}