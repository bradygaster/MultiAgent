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