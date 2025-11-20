using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

public class ConversationLoop(ILogger<ConversationLoop> logger, AgentPool agentPool, WorkflowEventPublisher eventPublisher)
{
    private async Task PublishWorkflowStartedEventAsync<TEvent>(IWorkflowDefinition workflowDefinition, string userInput, string workflowId) where TEvent : WorkflowStatusEvent, new()
    {
        var evt = new TEvent
        {
            AgentId = Static.SystemAgentId,
            AgentName = Static.SystemAgentName,
            WorkflowEventType = WorkflowEventType.WorkflowStarted,
            Message = userInput,
            Timestamp = DateTime.UtcNow,
            WorkflowId = workflowId
        };
        await PublishWorkflowEventAsync(evt, workflowDefinition, WorkflowEventType.WorkflowStarted);
    }

    private async Task PublishAgentStartedEventAsync<TEvent>(IWorkflowDefinition workflowDefinition, AgentRunUpdateEvent e, string workflowId) where TEvent : WorkflowStatusEvent, new()
    {
        var evt = new TEvent
        {
            AgentId = e.ExecutorId,
            AgentName = e.Update.AuthorName ?? "Unknown",
            WorkflowEventType = WorkflowEventType.AgentStarted,
            Message = $"🕵️ {e.Update.AuthorName} starting",
            Timestamp = DateTime.UtcNow,
            WorkflowId = workflowId
        };
        await PublishWorkflowEventAsync(evt, workflowDefinition, WorkflowEventType.AgentStarted);
    }

    private async Task PublishToolCalledEventAsync<TEvent>(IWorkflowDefinition workflowDefinition, AgentRunUpdateEvent e, FunctionCallContent call, string workflowId) where TEvent : WorkflowStatusEvent, new()
    {
        var evt = new TEvent
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
            },
            WorkflowId = workflowId
        };
        await PublishWorkflowEventAsync(evt, workflowDefinition, WorkflowEventType.ToolCalled);
    }

    private async Task PublishWorkflowEndedEventAsync<TEvent>(IWorkflowDefinition workflowDefinition, string workflowId, string workflowName, Activity? activity) where TEvent : WorkflowStatusEvent, new()
    {
        var evt = new TEvent
        {
            AgentId = Static.SystemAgentId,
            AgentName = Static.SystemAgentName,
            WorkflowEventType = WorkflowEventType.WorkflowEnded,
            Message = $"{workflowName} completed successfully",
            Timestamp = DateTime.UtcNow,
            WorkflowId = workflowId
        };

        await PublishWorkflowEventAsync(evt, workflowDefinition, WorkflowEventType.WorkflowEnded);
    }

    private async Task<Workflow> BuildAndValidateWorkflow(IWorkflowDefinition workflowDefinition)
    {
        var workflowObject = workflowDefinition.BuildWorkflow(agentPool);
        if (workflowObject is not Workflow workflow)
        {
            throw new InvalidOperationException($"BuildWorkflow must return a Workflow instance, but returned {workflowObject?.GetType().Name ?? "null"}");
        }
        return workflow;
    }

    private async Task<StreamingRun> StartWorkflowSession(Workflow workflow, ChatMessage initialMessage)
    {
        return await InProcessExecution.StreamAsync(workflow: workflow, initialMessage);
    }

    private async Task<List<ChatMessage>> ProcessWorkflowEvents<TEvent>(StreamingRun run, IWorkflowDefinition workflowDefinition, string workflowId, string workflowName, Activity? activity) where TEvent : WorkflowStatusEvent, new()
    {
        string? lastExecutorId = null;
        var sb = new StringBuilder();
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));
        await foreach (WorkflowEvent evt in run.WatchStreamAsync())
        {
            if (evt is AgentRunUpdateEvent e)
            {
                if (e.ExecutorId != lastExecutorId)
                {
                    lastExecutorId = e.ExecutorId;
                    logger.LogInformation("🕵️ AgentRunUpdateEvent: {Author} starting", e.Update.AuthorName);
                    await PublishAgentStartedEventAsync<TEvent>(workflowDefinition, e, workflowId);
                }
                sb.Append(e.Update.Text);
                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                {
                    logger.LogInformation("📡 Calling MCP Tool '{ToolName}' with arguments: {Args}", call.Name, JsonSerializer.Serialize(call.Arguments));
                    await PublishToolCalledEventAsync<TEvent>(workflowDefinition, e, call, workflowId);
                }
            }
            else if (evt is WorkflowOutputEvent output)
            {
                logger.LogInformation("📒 Output of order fulfillment process: \n {Output}", sb.ToString());
                sb.Clear();
                await PublishWorkflowEndedEventAsync<TEvent>(workflowDefinition, workflowId, workflowName, activity);
                return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
            }
        }
        return new List<ChatMessage>();
    }

    private async Task PublishWorkflowEventAsync<TEvent>(TEvent evt, IWorkflowDefinition workflowDefinition, WorkflowEventType eventType) where TEvent : WorkflowStatusEvent
    {
        workflowDefinition.EnrichEvent(evt, eventType);
        await eventPublisher.PublishEventAsync(evt);
    }

    public async Task<List<ChatMessage>> ExecuteWorkflowAsync<TEvent>(
        IWorkflowDefinition workflowDefinition,
        string userInput) where TEvent : WorkflowStatusEvent, new()
    {
        var workflowId = workflowDefinition.GenerateWorkflowInstanceId();
        await PublishWorkflowStartedEventAsync<TEvent>(workflowDefinition, userInput, workflowId);
        var workflow = await BuildAndValidateWorkflow(workflowDefinition);
        var initialMessage = workflowDefinition.BuildInitialMessage(userInput);
        using (var session = TelemetryConfig.OrderWorkflowActivitySource.StartActivity("ConversationLoop.SubmitOrder", ActivityKind.Server))
        {
            session?.Start();
            await using StreamingRun run = await StartWorkflowSession(workflow, initialMessage);
            var result = await ProcessWorkflowEvents<TEvent>(run, workflowDefinition, workflowId, workflowDefinition.Name, session);
            session?.Stop();
            return result;
        }
    }
}