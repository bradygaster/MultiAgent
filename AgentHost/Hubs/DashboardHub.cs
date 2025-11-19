using Microsoft.AspNetCore.SignalR;

public class DashboardHub : Hub
{
    // NOTE: Order history storage is now abstracted for flexibility and testability.
    private readonly IOrderHistoryStore _orderHistoryStore;
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger, IOrderHistoryStore orderHistoryStore)
    {
        _logger = logger;
        _orderHistoryStore = orderHistoryStore;
    }

    public async Task SubscribeToOrders()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "OrderUpdates");
        _logger.LogInformation("📻 Client {ConnectionId} subscribed to order updates", Context.ConnectionId);
    }

    public async Task PublishOrderStatusEvent(OrderStatusEvent evt)
    {
        await _orderHistoryStore.AddEventAsync(evt);
        _logger.LogDebug("📻 Publishing order event: WorkflowEventType={WorkflowEventType} OrderEventType={OrderEventType} for Order {OrderId} Agent {AgentName}", 
            evt.WorkflowEventType, evt.OrderEventType, evt.OrderId, evt.AgentName);
        await Clients.Group("OrderUpdates").SendAsync("OrderStatusUpdate", evt);
    }

    public async Task<List<OrderStatusEvent>> GetOrderHistory(string orderId)
    {
        return await _orderHistoryStore.GetOrderHistoryAsync(orderId);
    }

    public async Task<Dictionary<string, int>> GetOrderStats()
    {
        return await _orderHistoryStore.GetOrderStatsAsync();
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("🛜 Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("⛓️‍💥 Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
