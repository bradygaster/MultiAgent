namespace MultiAgent.StatusHub.Models;

public class OrderStatusEvent
{
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public WorkflowEventType WorkflowEventType { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object>? ToolCall { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public OrderEventType OrderEventType { get; set; }
}

public enum WorkflowEventType
{
    WorkflowStarted,
    WorkflowEnded,
    AgentStarted,
    AgentCompleted,
    ToolCalled,
    Error
}

public enum OrderEventType
{
    OrderReceived,
    OrderCompleted,
    Error
}
