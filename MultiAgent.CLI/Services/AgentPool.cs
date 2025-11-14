using Microsoft.Agents.AI;

public class AgentPool
{
    private readonly Dictionary<string, AIAgent> _agents = new();
    private readonly Dictionary<string, AgentMetadata> _agentMetadata = new();
    
    public void AddAgent(string agentId, AIAgent agent, AgentMetadata metadata)
    {
        _agents[agentId] = agent;
        _agentMetadata[agentId] = metadata;
    }
    
    public AIAgent? GetAgent(string id) => _agents.GetValueOrDefault(id);
    
    public AgentMetadata? GetMetadata(string id) => _agentMetadata.GetValueOrDefault(id);
    
    public IEnumerable<string> GetAvailableAgents() => _agents.Select(x => x.Value.Id);
    
    public IEnumerable<(string AgentId, AgentMetadata Metadata)> GetAgentSummaries() 
        => _agentMetadata.Select(kvp => (kvp.Value.Id, kvp.Value));
}