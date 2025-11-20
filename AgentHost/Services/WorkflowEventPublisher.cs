using Microsoft.Agents.AI.Workflows;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.AI;
using System.Diagnostics;

public class WorkflowEventPublisher(ILogger<WorkflowEventPublisher> logger,
        IHubContext<DashboardHub> dashboardHubContext)
{
    protected internal async Task PublishEventAsync<TWorkflowStatusEvent>(TWorkflowStatusEvent e) where TWorkflowStatusEvent : WorkflowStatusEvent
    {
        try
        {
            await dashboardHubContext.Clients.All.SendAsync($"{e.GetType().Name}", e);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish order event {e}", e);
        }
    }

    public async Task PublishWorkflowEventAsync<TEvent>(TEvent evt, IWorkflowDefinition workflowDefinition, WorkflowEventType eventType) where TEvent : WorkflowStatusEvent
    {
        workflowDefinition.EnrichEvent(evt, eventType);
        await PublishEventAsync(evt);
    }

    public async Task PublishWorkflowStartedEventAsync<TEvent>(IWorkflowDefinition workflowDefinition, string userInput, string workflowId) where TEvent : WorkflowStatusEvent, new()
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

    public async Task PublishAgentStartedEventAsync<TEvent>(IWorkflowDefinition workflowDefinition, AgentRunUpdateEvent e, string workflowId) where TEvent : WorkflowStatusEvent, new()
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

    public async Task PublishToolCalledEventAsync<TEvent>(IWorkflowDefinition workflowDefinition, AgentRunUpdateEvent e, FunctionCallContent call, string workflowId) where TEvent : WorkflowStatusEvent, new()
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

    public async Task PublishWorkflowEndedEventAsync<TEvent>(IWorkflowDefinition workflowDefinition, string workflowId, string workflowName, Activity? activity) where TEvent : WorkflowStatusEvent, new()
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
}