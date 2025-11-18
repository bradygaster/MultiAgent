public class OrderStatusEvent : WorkflowStatusEvent
{
    public required string OrderId { get; set; }
    public OrderEventType OrderEventType { get; set; }
}

public enum OrderEventType
{
    OrderReceived,
    OrderCompleted,
    Error
}