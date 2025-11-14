using System.Diagnostics;

public static class TelemetryConfig
{
    public static readonly string OrderWorkflowSourceName = "OrderWorkflow";

    public static ActivitySource OrderWorkflowActivitySource = new ActivitySource(OrderWorkflowSourceName);
}
