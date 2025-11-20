using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;

public class OrderWorkflowDefinition : WorkflowDefinitionBase
{
    public override string Name => "OrderFulfillment";
    
    public override string Description => "Processes restaurant orders through grill, fryer, dessert, and plating stations";

    public override Workflow BuildWorkflow(AgentPool agentPool)
    {
        return AgentWorkflowBuilder.BuildSequential(
            agentPool.GetAgent("grill")!,
            agentPool.GetAgent("fryer")!,
            agentPool.GetAgent("desserts")!,
            agentPool.GetAgent("expo")!
        );
    }

    public override ChatMessage BuildInitialMessage(string userInput)
    {
        var preamble = new StringBuilder();
        preamble.AppendLine("ORDER SUMMARY:");
        preamble.AppendLine(userInput.Trim());
        preamble.AppendLine();

        return new ChatMessage(ChatRole.User, preamble.ToString());
    }
}
