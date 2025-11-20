using Microsoft.AspNetCore.SignalR;

public class WorkflowEventPublisher(ILogger<WorkflowEventPublisher> logger,
        IHubContext<DashboardHub> dashboardHubContext)
{
    protected internal async Task PublishEventAsync<TWorkflowStatusEvent>(TWorkflowStatusEvent e) where TWorkflowStatusEvent : WorkflowStatusEvent
    {
        try
        {
            await dashboardHubContext.Clients.All.SendAsync($"{e.GetType().Name}", e);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish order event {e}", e);
        }
    }
}