using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;

public class OrderWorkflowDefinition : IWorkflowDefinition
{
    public string Name => "OrderFulfillment";
    
    public string Description => "Processes restaurant orders through grill, fryer, dessert, and plating stations";

    public object BuildWorkflow(AgentPool agentPool)
    {
        return AgentWorkflowBuilder.BuildSequential(
            agentPool.GetAgent(AgentIdentifiers.GrillAgent)!,
            agentPool.GetAgent(AgentIdentifiers.FryerAgent)!,
            agentPool.GetAgent(AgentIdentifiers.DessertAgent)!,
            agentPool.GetAgent(AgentIdentifiers.PlatingAgent)!
        );
    }

    public ChatMessage BuildInitialMessage(string userInput)
    {
        var preamble = new StringBuilder();
        preamble.AppendLine("ORDER SUMMARY:");
        preamble.AppendLine(userInput.Trim());
        preamble.AppendLine();

        return new ChatMessage(ChatRole.User, preamble.ToString());
    }

    public string GenerateWorkflowInstanceId()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }

    public void EnrichEvent(WorkflowStatusEvent evt, string instanceId, WorkflowEventType eventType)
    {
        if (evt is OrderStatusEvent orderEvent)
        {
            orderEvent.OrderId = instanceId;
            
            // Map workflow event types to order-specific event types
            orderEvent.OrderEventType = eventType switch
            {
                WorkflowEventType.WorkflowStarted => OrderEventType.OrderReceived,
                WorkflowEventType.WorkflowEnded => OrderEventType.OrderCompleted,
                _ => default
            };
        }
    }
}
