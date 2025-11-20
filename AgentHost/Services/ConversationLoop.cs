using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

public class ConversationLoop(ILogger<ConversationLoop> logger, WorkflowEventPublisher eventPublisher)
{
    public async Task<List<ChatMessage>> ExecuteWorkflowAsync<TEvent>(
        IWorkflowDefinition workflowDefinition,
        string userInput) where TEvent : WorkflowStatusEvent, new()
    {
        var workflowId = workflowDefinition.GenerateWorkflowInstanceId();
        using (var session = TelemetryConfig.WorkflowActivitySource.StartActivity($"Workflow #{workflowId}", ActivityKind.Server))
        {
            session?.Start();
            await eventPublisher.PublishWorkflowStartedEventAsync<TEvent>(workflowDefinition, userInput, workflowId);
            var workflow = await BuildAndValidateWorkflow(workflowDefinition);
            var initialMessage = workflowDefinition.BuildInitialMessage(userInput);
            await using StreamingRun run = await StartWorkflowSession(workflow, initialMessage);
            var result = await ProcessWorkflowEvents<TEvent>(run, workflowDefinition, workflowId, workflowDefinition.Name, session);
            session?.Stop();
            return result;
        }
    }

    private async Task<Workflow> BuildAndValidateWorkflow(IWorkflowDefinition workflowDefinition)
    {
        var workflowObject = workflowDefinition.BuildWorkflow();
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
                    await eventPublisher.PublishAgentStartedEventAsync<TEvent>(workflowDefinition, e, workflowId);
                }
                sb.Append(e.Update.Text);
                if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                {
                    logger.LogInformation("📡 Calling MCP Tool '{ToolName}' with arguments: {Args}", call.Name, JsonSerializer.Serialize(call.Arguments));
                    await eventPublisher.PublishToolCalledEventAsync<TEvent>(workflowDefinition, e, call, workflowId);
                }
            }
            else if (evt is WorkflowOutputEvent output)
            {
                logger.LogInformation("📒 Output of order fulfillment process: \n {Output}", sb.ToString());
                sb.Clear();
                await eventPublisher.PublishWorkflowEndedEventAsync<TEvent>(workflowDefinition, workflowId, workflowName, activity);
                return output.As<List<ChatMessage>>() ?? new List<ChatMessage>();
            }
        }
        return new List<ChatMessage>();
    }
}