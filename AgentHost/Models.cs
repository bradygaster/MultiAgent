public class AgentMetadata
{
    public string Id { get; set; } = "agendit";
    public string Name { get; set; } = "Custom AI Agent";
    public string Domain { get; set; } = "the specified domain";
    public string[] Tools { get; set; } = Array.Empty<string>();
    public string Emoji { get; set; } = "🤖";
    public string Color { get; set; } = "#94a3b8";
}