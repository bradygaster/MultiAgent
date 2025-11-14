using Microsoft.AspNetCore.SignalR.Client;

namespace MultiAgent.CLI.Services;

public class OrderEventPublisher : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<OrderEventPublisher> _logger;

    public OrderEventPublisher(IConfiguration config, ILogger<OrderEventPublisher> logger)
    {
        _logger = logger;
        
        // Get the StatusHub URL from Aspire service discovery
        var hubUrl = config.GetConnectionString("statushub") ?? config["services:statushub:http:0"] ?? "http://localhost:5274";
        
        _logger.LogInformation("Connecting to OrderStatusHub at {HubUrl}", hubUrl);
        
        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{hubUrl}/orderstatus")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "SignalR connection lost, reconnecting...");
            return Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected with connection ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _logger.LogError(error, "SignalR connection closed");
            return Task.CompletedTask;
        };
    }

    public async Task StartAsync()
    {
        try
        {
            await _hubConnection.StartAsync();
            _logger.LogInformation("Connected to OrderStatusHub successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to OrderStatusHub");
        }
    }

    public async Task PublishEventAsync(OrderStatusEvent evt)
    {
        try
        {
            if (_hubConnection.State == HubConnectionState.Connected)
            {
                await _hubConnection.InvokeAsync("PublishOrderEvent", evt);
            }
            else
            {
                _logger.LogWarning("Cannot publish event - SignalR connection state is {State}", _hubConnection.State);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish order event for Order {OrderId}", evt.OrderId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}

public class OrderStatusEvent
{
    public string OrderId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public OrderEventType EventType { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? ToolCall { get; set; }
}

public enum OrderEventType
{
    OrderReceived,
    AgentStarted,
    ToolCalled,
    AgentCompleted,
    OrderCompleted,
    Error
}
