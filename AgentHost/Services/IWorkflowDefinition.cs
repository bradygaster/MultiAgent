using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

public interface IWorkflowDefinition
{
    string Name { get; }
    
    string Description { get; }

    string GenerateWorkflowInstanceId();

    void EnrichEvent(WorkflowStatusEvent evt, WorkflowEventType eventType);

    Workflow BuildWorkflow();
    
    ChatMessage BuildInitialMessage(string userInput);
}
