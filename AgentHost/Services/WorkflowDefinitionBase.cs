using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;

public abstract class WorkflowDefinitionBase : IWorkflowDefinition
{
    public abstract string Name { get; }

    public abstract string Description { get; }

    public abstract Workflow BuildWorkflow();

    public virtual void EnrichEvent(WorkflowStatusEvent evt, WorkflowEventType eventType)
    {
        // leave empty for override
    }

    public virtual string GenerateWorkflowInstanceId()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    public virtual ChatMessage BuildInitialMessage(string userInput)
    {
        var preamble = new StringBuilder();
        preamble.AppendLine(userInput.Trim());
        preamble.AppendLine();

        return new ChatMessage(ChatRole.User, preamble.ToString());
    }
}