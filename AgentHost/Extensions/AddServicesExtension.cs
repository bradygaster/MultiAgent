using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

internal static class AddServicesExtension
{
    public static IHostApplicationBuilder AddServices(this IHostApplicationBuilder builder)
    {
        builder.AddMcpClients();
        builder.Services.AddSingleton<InstructionLoader>();
        builder.Services.AddSingleton<BaseEventPublisher>();
        builder.Services.AddSingleton<IOrderHistoryStore, InMemoryOrderHistoryStore>();

        // add workflows
        builder.AddOrderWorkflow();

        // set up the agent pool
        _ = builder.Services.AddSingleton<AgentPool>(services =>
        {
            var azureOptions = services.GetRequiredService<IOptions<AzureSettings>>().Value;
            var appSettings = services.GetRequiredService<IOptions<MultiAgentSettings>>().Value;
            var instructionLoader = services.GetRequiredService<InstructionLoader>();

            // Validate required Azure settings
            if (string.IsNullOrWhiteSpace(azureOptions.ModelName))
                throw new InvalidOperationException("Azure:ModelName not configured.");
            if (string.IsNullOrWhiteSpace(azureOptions.Endpoint))
                throw new InvalidOperationException("Azure:Endpoint not configured.");

            // Load ALL instruction files
            var allInstructions = instructionLoader.LoadAllInstructions();

            // Create chat client once
            var cred = new ChainedTokenCredential(
                new AzureCliCredential(),
                new EnvironmentCredential(),
                new ManagedIdentityCredential()
            );

            var chatClient = new AzureOpenAIClient(new Uri(azureOptions.Endpoint), cred)
                .GetChatClient(azureOptions.ModelName)
                .AsIChatClient()
                .AsBuilder()
                .UseOpenTelemetry(sourceName: "agent-telemetry-source")
                .Build();

            // Create agent pool and populate it
            var agentPool = new AgentPool();

            // All the MCP Client tools available across all the servers
            var allMcpClientTools = new Dictionary<string, AITool>();

            foreach (var mcpServer in appSettings.McpServers)
            {
                var mcpClient = services.GetKeyedService<McpClient>(mcpServer);
                if (mcpClient == null)
                    throw new InvalidOperationException("McpClient service not available.");
                var tools = mcpClient.ListToolsAsync().GetAwaiter().GetResult();
                foreach (var tool in tools)
                {
                    if (!allMcpClientTools.ContainsKey(tool.Name))
                    {
                        allMcpClientTools[tool.Name] = tool;
                    }
                }
            }

            // Get the tools available for each agent
            foreach (var (key, instructionData) in allInstructions)
            {
                var filteredTools = allMcpClientTools
                    .Where(t => instructionData.Metadata.Tools.Contains(t.Key))
                    .Select(t => t.Value)
                    .ToList();

                var convertedTools = filteredTools.Cast<AITool>().ToList();

                var agent = chatClient.CreateAIAgent(
                    name: instructionData.Metadata.Name,
                    instructions: instructionData.Content,
                    tools: convertedTools
                );

                agentPool.AddAgent(instructionData.Metadata.Id, agent, instructionData.Metadata);
            }

            return agentPool;
        });

        builder.Services.AddSingleton<ConversationLoop>();

        return builder;
    }
}
