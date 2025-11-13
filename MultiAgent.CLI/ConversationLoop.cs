using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class ConversationLoop(AgentPool agentPool, ILogger<ConversationLoop> logger)
{
    private AIAgent? _currentAgent;
    private AgentThread? _currentThread;
    private string? _currentAgentKey;

    public async Task<List<ChatMessage>> SubmitRandomOrder()
    {
        var randomOrders = new List<string>
        {
            "1 cheeseburger with fries and a chocolate milkshake",
            "3 cheeseburgers, 2 orders of fries, and 2 chocolate milkshakes",
            "2 cheeseburgers, one with bacon, 1 order of regular fries without salt, 1 order of sweet potato fries, and 2 strawberry milkshakes",
        };

        return await SubmitOrder(randomOrders[new Random().Next(randomOrders.Count)]);
    }

    public async Task<List<ChatMessage>> SubmitOrder(string order)
    {
        // Build a strict preamble that instructs all agents not to invent items and to use defaults.
        var preamble = new System.Text.StringBuilder();
        preamble.AppendLine("ORDER SUMMARY:");
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
                            .AddEdge(grillAgent!, fryerAgent!).WithOutputFrom(grillAgent!)
                            .AddEdge(fryerAgent!, dessertAgent!).WithOutputFrom(fryerAgent!)
                            .AddEdge(dessertAgent!, platingAgent!).WithOutputFrom(dessertAgent!)
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
                    logger.LogInformation($"\n--- AgentRunUpdateEvent {JsonSerializer.Serialize(evt.Data)} ---\n");
                }

                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                {
                    logger.LogInformation($"  [Calling function '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                }
                
                // If this update contains function calls, send a turn token to continue workflow execution
                if (e.Update.Contents.OfType<FunctionCallContent>().Any())
                {
                    await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
                }
            }
            else if (evt is WorkflowOutputEvent output)
            {
                // Return the list of chat messages produced by the workflow
                return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
            }
        }

        return new List<ChatMessage>();
    }
}