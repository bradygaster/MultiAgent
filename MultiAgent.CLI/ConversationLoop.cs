using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

public class ConversationLoop(AgentPool agentPool, ILogger<ConversationLoop> logger)
{
    public async Task<List<ChatMessage>> SubmitRandomOrder()
    {
        var randomOrders = new List<string>
        {
            "1 cheeseburger with fries and a chocolate milkshake",
            "3 cheeseburgers, 2 orders of fries, and 2 chocolate milkshakes",
            "2 cheeseburgers, 1 order of regular fries without salt, 1 order of sweet potato fries, and 2 strawberry milkshakes",
            "2 vanilla milkshakes and 1 order of onion rings",
            "1 sundae with whipped cream and a cherry on top",
            "1 cheeseburger with extra cheese, 1 order of fries, and 1 vanilla milkshake with sprinkles",
            "2 cheeseburgers, 1 order of sweet potato fries, and 2 chocolate milkshakes with whipped cream",
            "1 cheeseburger with bacon, 1 order of fries without salt, and 1 strawberry milkshake with a cherry on top",
            "5 cheeseburgers, 3 orders of fries, and 5 chocolate milkshakes",
            "5 cheeseburgers with extra cheese, 2 with bacon, 2 orders of sweet potato fries, 1 with no salt and 5 vanilla milkshakes with sprinkles"
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
        var workflow = AgentWorkflowBuilder.BuildSequential(grillAgent!, fryerAgent!, dessertAgent!, platingAgent!);

        string? lastExecutorId = null;

        // Start a fresh streaming run for this order so previous conversation state does not leak.
        using(var session = TelemetryConfig.OrderWorkflowActivitySource.StartActivity("ConversationLoop.SubmitOrder", System.Diagnostics.ActivityKind.Server))
        {
            session.Start();

            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow: workflow, initialMessage);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            var sb = new StringBuilder();

            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                if (evt is AgentRunUpdateEvent e)
                {
                    if (e.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = e.ExecutorId;
                        logger.LogInformation($"🕵️ AgentRunUpdateEvent: {JsonSerializer.Serialize(evt.Data)}");
                    }

                    sb.Append(e.Update.Text);

                    if (e.Update.Contents.OfType<FunctionCallContent>().Any())
                    {
                        logger.LogInformation($"📡 Calling MCP Tools");
                    }

                    if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                    {
                        logger.LogInformation($"📡 Calling MCP Tool '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                    }
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    logger.LogInformation($"📒 Output of order fulfillment process: \n {sb.ToString()}");
                    sb.Clear();

                    // Return the list of chat messages produced by the workflow
                    return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
                }
            }

            session.Stop();
        }
        

        return new List<ChatMessage>();
    }
}