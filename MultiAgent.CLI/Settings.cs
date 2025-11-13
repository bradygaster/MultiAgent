public class InstructionMetadata
{
    public string Id { get; set; } = "agendit";
    public string Name { get; set; } = "Custom AI Agent";
    public string Domain { get; set; } = "the specified domain";
    public string[] Tools { get; set; } = Array.Empty<string>();
}

public class MultiAgentSettings
{
    public string InstructionsPath { get; set; } = "instructions";
}

public class AzureSettings
{
    public string ModelName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}

public static class AgentIdentifiers
{
    public static string DessertAgent => "desserts";
    public static string FryerAgent => "fryer";
    public static string GrillAgent => "grill";
    public static string PlatingAgent => "expo";
}
