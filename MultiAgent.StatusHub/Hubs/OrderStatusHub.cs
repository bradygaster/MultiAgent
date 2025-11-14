using Microsoft.AspNetCore.SignalR;
using MultiAgent.StatusHub.Models;

namespace MultiAgent.StatusHub.Hubs;

public class OrderStatusHub : Hub
{
    private static readonly Dictionary<string, List<OrderStatusEvent>> _orderHistory = new();
    private readonly ILogger<OrderStatusHub> _logger;

    public OrderStatusHub(ILogger<OrderStatusHub> logger)
    {
        _logger = logger;
    }

    public async Task SubscribeToOrders()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "OrderUpdates");
        _logger.LogInformation("📻 Client {ConnectionId} subscribed to order updates", Context.ConnectionId);
    }

    public async Task PublishOrderEvent(OrderStatusEvent evt)
    {
        if (!_orderHistory.ContainsKey(evt.OrderId))
        {
            _orderHistory[evt.OrderId] = new List<OrderStatusEvent>();
        }
        
        _orderHistory[evt.OrderId].Add(evt);
        
        _logger.LogDebug("📻 Publishing order event: {EventType} for Order {OrderId} Agent {AgentName}", 
            evt.EventType, evt.OrderId, evt.AgentName);
        
        await Clients.Group("OrderUpdates").SendAsync("OrderStatusUpdate", evt);
    }

    public Task<List<OrderStatusEvent>> GetOrderHistory(string orderId)
    {
        return Task.FromResult(_orderHistory.GetValueOrDefault(orderId) ?? new List<OrderStatusEvent>());
    }

    public Task<Dictionary<string, int>> GetOrderStats()
    {
        return Task.FromResult(new Dictionary<string, int>
        {
            ["TotalOrders"] = _orderHistory.Count,
            ["TotalEvents"] = _orderHistory.Values.Sum(events => events.Count)
        });
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
