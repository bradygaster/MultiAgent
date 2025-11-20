using System.Diagnostics;

public static class TelemetryConfig
{
    public static readonly string ApplicationTelemetrySourceName = "MutiAgent";

    public static ActivitySource ApplicationTelemetrySource = new ActivitySource(ApplicationTelemetrySourceName);
}
