using Microsoft.Agents.AI;

public class ConversationLoop
{
    private readonly AgentPool _agentPool;
    private readonly ConsoleClient _consoleClient;
    private AIAgent? _currentAgent;
    private AgentThread? _currentThread;
    private string? _currentAgentKey;

    public ConversationLoop(AgentPool agentPool, ConsoleClient consoleClient)
    {
        _agentPool = agentPool;
        _consoleClient = consoleClient;
    }

    public async Task Chat()
    {
        ShowAvailableAgents();
        
        while (true)
        {
            Console.Write("\n> ");
            var input = Console.ReadLine();

            switch (input?.Trim().ToLowerInvariant())
            {
                case null or "":
                    continue;
                case "exit":
                    return;
                case "agents":
                    ShowAvailableAgents();
                    break;
                case string cmd when cmd.StartsWith("switch "):
                    SwitchAgent(cmd.Substring(7));
                    break;
                default:
                    if (_currentAgent == null)
                    {
                        _consoleClient.Print("Please select an agent first. Type 'agents' to see available agents.", ConsoleColor.Yellow);
                        continue;
                    }
                    await ProcessAgentResponse(input);
                    break;
            }
        }
    }

    private void ShowAvailableAgents()
    {
        _consoleClient.Print("\nAvailable Agents:", ConsoleColor.Cyan);
        foreach (var (key, metadata) in _agentPool.GetAgentSummaries())
        {
            var marker = key == _currentAgentKey ? "* " : "  ";
            _consoleClient.Print($"{marker}{key}: {metadata.Name} ({metadata.Domain})", ConsoleColor.White);
        }
        _consoleClient.Print("\nCommands:", ConsoleColor.Gray);
        _consoleClient.Print("  switch <agent-key> - Switch to an agent", ConsoleColor.Gray);
        _consoleClient.Print("  agents - Show this list", ConsoleColor.Gray);
        _consoleClient.Print("  exit - Quit", ConsoleColor.Gray);
    }

    private void SwitchAgent(string agentKey)
    {
        var agent = _agentPool.GetAgent(agentKey);
        var metadata = _agentPool.GetMetadata(agentKey);
        
        if (agent == null || metadata == null)
        {
            _consoleClient.Print($"Agent '{agentKey}' not found.", ConsoleColor.Red);
            return;
        }
        
        _currentAgent = agent;
        _currentThread = agent.GetNewThread();
        _currentAgentKey = agentKey;
        
        _consoleClient.Print($"\nSwitched to {metadata.Name} ({metadata.Domain})", ConsoleColor.Green);
    }

    private async Task ProcessAgentResponse(string input)
    {
        try
        {
            _consoleClient.Print($"\n[{_agentPool.GetMetadata(_currentAgentKey!)?.Name}]:", ConsoleColor.Cyan);
            
            await foreach (var update in _currentAgent!.RunStreamingAsync(input, _currentThread!))
            {
                _consoleClient.Fragment(update.Text, ConsoleColor.White);
            }
            
            _consoleClient.Print("\n", ConsoleColor.White); // End the response with a newline
        }
        catch (Exception ex)
        {
            _consoleClient.Print($"\nError during agent run: {ex.Message}", ConsoleColor.Red);
        }
    }
}