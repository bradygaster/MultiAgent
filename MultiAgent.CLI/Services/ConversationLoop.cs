using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

public class ConversationLoop(ILogger<ConversationLoop> logger,
    AgentPool agentPool,
    BaseEventPublisher eventPublisher)
{
    public async Task<List<ChatMessage>> SendPrompt(string prompt)
    {
        // Build the linear workflow through the agents in the intended order.
        var workflow = AgentWorkflowBuilder.BuildSequential(
            agentPool.GetAgent(AgentIdentifiers.GrillAgent)!,
            agentPool.GetAgent(AgentIdentifiers.FryerAgent)!,
            agentPool.GetAgent(AgentIdentifiers.DessertAgent)!,
            agentPool.GetAgent(AgentIdentifiers.PlatingAgent)!
            );

        string? lastExecutorId = null; 
        
        var orderId = Guid.NewGuid().ToString("N")[..8];

        // Publish order received event
        await eventPublisher.PublishEventAsync(new OrderStatusEvent
        {
            OrderId = orderId,
            AgentId = "system",
            AgentName = "System",
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            OrderEventType = OrderEventType.OrderReceived,
            Message = prompt,
            Timestamp = DateTime.UtcNow
        });

        // Build a strict preamble that instructs all agents not to invent items and to use defaults.
        var preamble = new System.Text.StringBuilder();
        preamble.AppendLine("ORDER SUMMARY:");
        preamble.AppendLine(prompt.Trim());
        preamble.AppendLine();

        var initialMessage = new ChatMessage(ChatRole.User, preamble.ToString());

        // Start a fresh streaming run for this order so previous conversation state does not leak.
        using (var session = TelemetryConfig.OrderWorkflowActivitySource.StartActivity("ConversationLoop.SubmitOrder", System.Diagnostics.ActivityKind.Server))
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
                        logger.LogInformation($"🕵️ AgentRunUpdateEvent: {e.Update.AuthorName} starting");
                        
                        // Publish agent started event
                        await eventPublisher.PublishEventAsync(new OrderStatusEvent
                        {
                            OrderId = orderId,
                            AgentId = e.ExecutorId,
                            AgentName = e.Update.AuthorName,
                            WorkflowEventType = WorkflowEventType.AgentStarted,
                            Message = $"🕵️ {e.Update.AuthorName} starting",
                            Timestamp = DateTime.UtcNow
                        });
                    }

                    sb.Append(e.Update.Text);

                    if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                    {
                        logger.LogInformation($"📡 Calling MCP Tool '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                        
                        // Publish tool call event
                        await eventPublisher.PublishEventAsync(new OrderStatusEvent
                        {
                            OrderId = orderId,
                            AgentId = e.ExecutorId,
                            AgentName = e.Update.AuthorName,
                            WorkflowEventType = WorkflowEventType.ToolCalled,
                            Message = $"Calling {call.Name}",
                            Timestamp = DateTime.UtcNow,
                            ToolCall = new Dictionary<string, object>
                            {
                                ["name"] = call.Name,
                                ["arguments"] = call.Arguments ?? new Dictionary<string, object>()
                            }
                        });
                    }
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    logger.LogInformation($"📒 Output of order fulfillment process: \n {sb.ToString()}");
                    sb.Clear();

                    // Publish order completed event
                    await eventPublisher.PublishEventAsync(new OrderStatusEvent
                    {
                        OrderId = orderId,
                        AgentId = "system",
                        AgentName = "System",
                        OrderEventType = OrderEventType.OrderCompleted,
                        Message = "Order completed successfully",
                        Timestamp = DateTime.UtcNow
                    });

                    // Return the list of chat messages produced by the workflow
                    return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
                }
            }

            session.Stop();
        }
        

        return new List<ChatMessage>();
    }
}