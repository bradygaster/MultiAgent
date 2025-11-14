public class OrderStatusEvent : WorkflowStatusEvent
{
    public string OrderId { get; set; }
    public OrderEventType OrderEventType { get; set; }

}

public enum OrderEventType
{
    OrderReceived,
    OrderCompleted,
    Error
}