namespace MultiAgent.StatusHub.Models;

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
