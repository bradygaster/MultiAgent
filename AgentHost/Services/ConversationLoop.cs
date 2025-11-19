using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;

public class ConversationLoop(ILogger<ConversationLoop> logger, AgentPool agentPool, BaseEventPublisher eventPublisher)
{
    private async Task PublishWorkflowEventAsync<TEvent>(TEvent evt, IWorkflowDefinition workflowDefinition, string instanceId, WorkflowEventType eventType) where TEvent : WorkflowStatusEvent
    {
        workflowDefinition.EnrichEvent(evt, instanceId, eventType);
        await eventPublisher.PublishEventAsync(evt);
    }

    public async Task<List<ChatMessage>> ExecuteWorkflowAsync<TEvent>(
        IWorkflowDefinition workflowDefinition,
        string userInput) where TEvent : WorkflowStatusEvent, new()
    {
        // Build the workflow using the definition
        var workflowObject = workflowDefinition.BuildWorkflow(agentPool);

        // Cast to the workflow type expected by StreamAsync
        if (workflowObject is not Workflow workflow)
        {
            throw new InvalidOperationException($"BuildWorkflow must return a Workflow instance, but returned {workflowObject?.GetType().Name ?? "null"}");
        }

        string? lastExecutorId = null;
        
        var instanceId = workflowDefinition.GenerateWorkflowInstanceId();

        // Publish workflow started event
        var startEvent = new TEvent
        {
            AgentId = "system",
            AgentName = "System",
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = userInput,
            Timestamp = DateTime.UtcNow
        };
        await PublishWorkflowEventAsync(startEvent, workflowDefinition, instanceId, WorkflowEventType.WorkflowStarted);

        // Build the initial message using the workflow definition
        var initialMessage = workflowDefinition.BuildInitialMessage(userInput);

        // Start a fresh streaming run for this order so previous conversation state does not leak.
        using (var session = TelemetryConfig.OrderWorkflowActivitySource.StartActivity("ConversationLoop.SubmitOrder", System.Diagnostics.ActivityKind.Server))
        {
            session?.Start();

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
                        var agentEvent = new TEvent
                        {
                            AgentId = e.ExecutorId,
                            AgentName = e.Update.AuthorName ?? "Unknown",
                            WorkflowEventType = WorkflowEventType.AgentStarted,
                            Message = $"🕵️ {e.Update.AuthorName} starting",
                            Timestamp = DateTime.UtcNow
                        };
                        await PublishWorkflowEventAsync(agentEvent, workflowDefinition, instanceId, WorkflowEventType.AgentStarted);
                    }

                    sb.Append(e.Update.Text);

                    if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                    {
                        logger.LogInformation($"📡 Calling MCP Tool '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                        
                        // Publish tool call event
                        var toolEvent = new TEvent
                        {
                            AgentId = e.ExecutorId,
                            AgentName = e.Update.AuthorName ?? "Unknown",
                            WorkflowEventType = WorkflowEventType.ToolCalled,
                            Message = $"Calling {call.Name}",
                            Timestamp = DateTime.UtcNow,
                            ToolCall = new Dictionary<string, object>
                            {
                                ["name"] = call.Name,
                                ["arguments"] = call.Arguments ?? new Dictionary<string, object?>()
                            }
                        };
                        await PublishWorkflowEventAsync(toolEvent, workflowDefinition, instanceId, WorkflowEventType.ToolCalled);
                    }
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    logger.LogInformation($"📒 Output of order fulfillment process: \n {sb.ToString()}");
                    sb.Clear();

                    // Publish workflow completed event
                    var completeEvent = new TEvent
                    {
                        AgentId = "system",
                        AgentName = "System",
                        WorkflowEventType = WorkflowEventType.WorkflowEnded,
                        Message = $"{workflowDefinition.Name} completed successfully",
                        Timestamp = DateTime.UtcNow
                    };
                    await PublishWorkflowEventAsync(completeEvent, workflowDefinition, instanceId, WorkflowEventType.WorkflowEnded);

                    // Return the list of chat messages produced by the workflow
                    return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
                }
            }

            session?.Stop();
        }
        

        return new List<ChatMessage>();
    }
}