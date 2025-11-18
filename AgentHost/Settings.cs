public class MultiAgentSettings
{
    public string InstructionsPath { get; set; } = "instructions";
    public List<string> McpServers { get; set; } = new List<string>();
}

public class AzureSettings
{
    public string ModelName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}
