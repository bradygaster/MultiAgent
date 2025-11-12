using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Server;
using System.Text.Json;

public class ConversationLoop(AgentPool agentPool, 
    ConsoleClient consoleClient,
    IEnumerable<McpServerTool> mcpTools)
{
    private AIAgent? _currentAgent;
    private AgentThread? _currentThread;
    private string? _currentAgentKey;

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
                case "tools":
                    ShowAvailableTools();
                    break;
                case string cmd when cmd.StartsWith("order "):
                    await SubmitOrder(cmd.Substring(6));
                    break;
                default:
                    if (_currentAgent == null)
                    {
                        consoleClient.Print("Please select an agent first. Type 'agents' to see available agents.", ConsoleColor.Yellow);
                        continue;
                    }
                    await ProcessAgentResponse(input);
                    break;
            }
        }
    }

    private void ShowAvailableTools()
    {
        consoleClient.Print("\nAvailable Tools:", ConsoleColor.Cyan);
        foreach (var tool in mcpTools)
        {
            consoleClient.Print($"{tool.ProtocolTool.Name}: {tool.ProtocolTool.Description}", ConsoleColor.White);
        }
    }

    private void ShowAvailableAgents()
    {
        consoleClient.Print("\nAvailable Agents:", ConsoleColor.Cyan);
        foreach (var (key, metadata) in agentPool.GetAgentSummaries())
        {
            consoleClient.Print($"{metadata.Id}: {metadata.Name} ({metadata.Domain})", ConsoleColor.White);
        }
        consoleClient.Print("\nCommands:", ConsoleColor.Gray);
        consoleClient.Print("  switch <agent-id> - Switch to an agent", ConsoleColor.Gray);
        consoleClient.Print("  agents - Show this list", ConsoleColor.Gray);
        consoleClient.Print("  tools - show the list of tools", ConsoleColor.Gray);
        consoleClient.Print("  order <your-order> - Place an order", ConsoleColor.Gray);
        consoleClient.Print("  exit - Quit", ConsoleColor.Gray);
    }

    public async Task<List<ChatMessage>> SubmitOrder(string order)
    {
        // Build a strict preamble that instructs all agents not to invent items and to use defaults.
        var preamble = new System.Text.StringBuilder();
        preamble.AppendLine("ORDER SUMMARY (canonical):");
        preamble.AppendLine(order.Trim());
        preamble.AppendLine();

        var initialMessage = new ChatMessage(ChatRole.User, preamble.ToString());

        // Get the agents from the pool
        var grillAgent = agentPool.GetAgent(AgentIdentifiers.GrillAgent);
        var fryerAgent = agentPool.GetAgent(AgentIdentifiers.FryerAgent);
        var dessertAgent = agentPool.GetAgent(AgentIdentifiers.DessertAgent);
        var platingAgent = agentPool.GetAgent(AgentIdentifiers.PlatingAgent);

        // Build the linear workflow through the agents in the intended order.
        var workflow = new WorkflowBuilder(grillAgent!)
                            .AddEdge(grillAgent!, fryerAgent!)
                            .AddEdge(fryerAgent!, dessertAgent!)
                            .AddEdge(dessertAgent!, platingAgent!)
                            .Build();

        string? lastExecutorId = null;

        // Start a fresh streaming run for this order so previous conversation state does not leak.
        await using StreamingRun run = await InProcessExecution.StreamAsync(workflow: workflow, initialMessage);
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentRunUpdateEvent e)
            {
                if (e.ExecutorId != lastExecutorId)
                {
                    lastExecutorId = e.ExecutorId;
                    Console.WriteLine();
                }

                Console.Write(e.Update.Text);
                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                {
                    Console.WriteLine();
                    Console.WriteLine($"  [Calling function '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                }
            }
            else if (evt is WorkflowOutputEvent output)
            {
                Console.WriteLine();
                // Return the list of chat messages produced by the workflow
                return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
            }
        }

        return new List<ChatMessage>();
    }

    private async Task ProcessAgentResponse(string input)
    {
        try
        {
            consoleClient.Print($"\n[{agentPool.GetMetadata(_currentAgentKey!)?.Name}]:", ConsoleColor.Cyan);
            
            await foreach (var update in _currentAgent!.RunStreamingAsync(input, _currentThread!))
            {
                consoleClient.Fragment(update.Text, ConsoleColor.White);
            }

            consoleClient.Print("\n", ConsoleColor.White); // End the response with a newline
        }
        catch (Exception ex)
        {
            consoleClient.Print($"\nError during agent run: {ex.Message}", ConsoleColor.Red);
        }
    }
}