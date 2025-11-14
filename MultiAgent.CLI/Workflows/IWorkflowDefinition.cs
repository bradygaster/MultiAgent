using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

public interface IWorkflowDefinition
{
    string Name { get; }
    
    string Description { get; }
    
    object BuildWorkflow(AgentPool agentPool);
    
    ChatMessage BuildInitialMessage(string userInput);
    
    string GenerateWorkflowInstanceId();
    
    void EnrichEvent(WorkflowStatusEvent evt, string instanceId, WorkflowEventType eventType);
}
