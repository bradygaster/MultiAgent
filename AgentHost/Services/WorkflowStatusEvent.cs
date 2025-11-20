public class WorkflowStatusEvent
{
    public string WorkflowId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string AgentName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public WorkflowEventType WorkflowEventType { get; set; }
    public Dictionary<string, object>? ToolCall { get; set; }
}

public enum WorkflowEventType
{
    Custom = 0,
    WorkflowStarted = 1,
    WorkflowEnded = 2,
    AgentStarted = 3,
    AgentCompleted = 4,
    ToolCalled = 5,
    Error = 6
}