using System.Diagnostics;

public static class TelemetryConfig
{
    public static readonly string WorkflowSourceName = "Workflows";

    public static ActivitySource WorkflowActivitySource = new ActivitySource(WorkflowSourceName);
}
