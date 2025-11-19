public class InMemoryOrderHistoryStore : IOrderHistoryStore
{
    private readonly Dictionary<string, List<OrderStatusEvent>> _orderHistory = new();
    private readonly object _lock = new();

    public Task AddEventAsync(OrderStatusEvent evt)
    {
        lock (_lock)
        {
            if (!_orderHistory.ContainsKey(evt.OrderId))
            {
                _orderHistory[evt.OrderId] = new List<OrderStatusEvent>();
            }
            _orderHistory[evt.OrderId].Add(evt);
        }
        return Task.CompletedTask;
    }

    public Task<List<OrderStatusEvent>> GetOrderHistoryAsync(string orderId)
    {
        lock (_lock)
        {
            return Task.FromResult(_orderHistory.GetValueOrDefault(orderId) ?? new List<OrderStatusEvent>());
        }
    }

    public Task<Dictionary<string, int>> GetOrderStatsAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(new Dictionary<string, int>
            {
                ["TotalOrders"] = _orderHistory.Count,
                ["TotalEvents"] = _orderHistory.Values.Sum(events => events.Count)
            });
        }
    }
}
