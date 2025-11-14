using Microsoft.AspNetCore.SignalR.Client;

public class OrderEventPublisher(BaseEventPublisher baseEventPublisher)
{
    public async Task PublishEventAsync(OrderStatusEvent evt)
    {
        try
        {
            if (baseEventPublisher.HubConnection.State == HubConnectionState.Connected)
            {
                await baseEventPublisher.HubConnection.InvokeAsync("PublishOrderEvent", evt);
            }
            else
            {
                baseEventPublisher.Logger.LogWarning("Cannot publish event - SignalR connection state is {State}", baseEventPublisher.HubConnection.State);
            }
        }
        catch (Exception ex)
        {
            baseEventPublisher.Logger.LogError(ex, "Failed to publish order event for Order {OrderId}", evt.OrderId);
        }
    }
}