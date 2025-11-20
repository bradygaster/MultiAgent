using Microsoft.AspNetCore.SignalR;

public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger)
    {
        _logger = logger;
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

    public async Task PublishOrderStatusEvent(WorkflowStatusEvent evt)
    {
        _logger.LogDebug("📻 Publishing order event: WorkflowEventType={WorkflowEventType} for Order {OrderId} Agent {AgentName}", 
            evt.WorkflowEventType, evt.WorkflowId, evt.AgentName);
        await Clients.All.SendAsync(evt.GetType().Name, evt);
    }
}
