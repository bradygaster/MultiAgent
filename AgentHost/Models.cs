public class AgentMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string[] Tools { get; set; } = Array.Empty<string>();
    public string Emoji { get; set; } = Static.DefaultAgentEmoji;
    public string Color { get; set; } = Static.DefaultAgentColor;
}