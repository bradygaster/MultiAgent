public interface IOrderHistoryStore
{
    Task AddEventAsync(OrderStatusEvent evt);
    Task<List<OrderStatusEvent>> GetOrderHistoryAsync(string orderId);
    Task<Dictionary<string, int>> GetOrderStatsAsync();
}
