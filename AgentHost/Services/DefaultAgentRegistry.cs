using Microsoft.Extensions.AI;
using System.Diagnostics;

public class DefaultAgentRegistry(InstructionLoader instructionLoader,
    AgentPool agentPool,
    IToolRegistry aIToolRegistry,
    IChatClientProvider chatClientProvider) : IAgentRegistry
{
    public async Task RegisterAllAgentsAsync()
    {
        var allAiTools = await aIToolRegistry.GetAllToolsAsync();
        var allInstructions = instructionLoader.LoadAllInstructions();
        var chatClient = await chatClientProvider.CreateChatClient();

        // Create agents based on instructions and available tools
        using (var session = TelemetryConfig.ApplicationTelemetrySource.StartActivity("Agent Creation", ActivityKind.Server))
        {
            foreach (var (key, instructionData) in allInstructions)
            {
                var filteredTools = allAiTools
                    .Where(t => instructionData.Metadata.Tools.Contains(t.Key))
                    .Select(t => t.Value)
                    .ToList();

                var convertedTools = filteredTools.Cast<AITool>().ToList();

                var agent = chatClient.CreateAIAgent(
                    name: instructionData.Metadata.Name,
                    instructions: instructionData.Instructions,
                    tools: convertedTools
                );

                agentPool.AddAgent(instructionData.Metadata.Id, agent, instructionData.Metadata);
            }
        }
    }
}