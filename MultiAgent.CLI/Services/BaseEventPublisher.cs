using Microsoft.AspNetCore.SignalR;

public class BaseEventPublisher(ILogger<BaseEventPublisher> logger,
        IHubContext<OrderStatusHub> orderStatusHubContext)
{
    protected internal async Task PublishEventAsync<TWorkflowStatusEvent>(TWorkflowStatusEvent e) where TWorkflowStatusEvent : WorkflowStatusEvent
    {
        try
        {
            await orderStatusHubContext.Clients.All.SendAsync($"{e.GetType().Name}", e);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish order event {e}", e);
        }
    }
}