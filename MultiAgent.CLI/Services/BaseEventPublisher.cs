using Microsoft.AspNetCore.SignalR.Client;

public class BaseEventPublisher : IAsyncDisposable
{
    protected internal readonly HubConnection HubConnection;
    protected internal readonly ILogger<BaseEventPublisher> Logger;

    public BaseEventPublisher(IConfiguration config, ILogger<BaseEventPublisher> logger)
    {
        Logger = logger;
        
        // Get the StatusHub URL from Aspire service discovery
        var hubUrl = config.GetConnectionString("statushub") ?? config["services:statushub:http:0"] ?? "http://localhost:5274";
        
        Logger.LogInformation("Connecting to OrderStatusHub at {HubUrl}", hubUrl);
        
        HubConnection = new HubConnectionBuilder()
            .WithUrl($"{hubUrl}/orderstatus")
            .WithAutomaticReconnect()
            .Build();

        HubConnection.Reconnecting += error =>
        {
            Logger.LogWarning(error, "SignalR connection lost, reconnecting...");
            return Task.CompletedTask;
        };

        HubConnection.Reconnected += connectionId =>
        {
            Logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };

        HubConnection.Closed += error =>
        {
            Logger.LogError(error, "SignalR connection closed");
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        try
        {
            await HubConnection.StartAsync();
            Logger.LogInformation("Connected to OrderStatusHub successfully");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to connect to OrderStatusHub");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (HubConnection != null)
        {
            await HubConnection.DisposeAsync();
        }
    }

    protected internal async Task PublishEventAsync<TWorkflowStatusEvent>(TWorkflowStatusEvent e) where TWorkflowStatusEvent : WorkflowStatusEvent
    {
        try
        {
            if (HubConnection.State == HubConnectionState.Connected)
            {
                await HubConnection.InvokeAsync("PublishOrderEvent", e);
            }
            else
            {
                Logger.LogWarning("Cannot publish event - SignalR connection state is {State}", HubConnection.State);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to publish order event {e}", e);
        }
    }
}