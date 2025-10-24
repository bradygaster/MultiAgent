public class InstructionMetadata
{
    public string Id { get; set; } = "agendit";
    public string Name { get; set; } = "Custom AI Agent";
    public string Domain { get; set; } = "the specified domain";
}

public class InstructionData
{
    public InstructionMetadata Metadata { get; set; } = new();
    public string Content { get; set; } = string.Empty;
}

public class AzureSettings
{
    public string ModelName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
}
