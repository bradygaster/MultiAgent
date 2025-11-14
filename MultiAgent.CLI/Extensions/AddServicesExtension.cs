using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

internal static class AddServicesExtension
{
    public static IHostApplicationBuilder AddServices(this IHostApplicationBuilder builder)
    {
        builder.AddMcpClient();
        builder.Services.AddSingleton<InstructionLoader>();
        builder.Services.AddSingleton<BaseEventPublisher>();

        // order workflow related services
        builder.Services.AddSingleton<IOrderGenerator, StaticOrderGenerator>();
        builder.Services.AddSingleton<OrderEventPublisher>();
        builder.Services.AddHostedService<OrderSimulatingWorker>();

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

            // Get the tools available for each agent
            var mcpClient = services.GetService<McpClient>();
            if (mcpClient == null)
                throw new InvalidOperationException("McpClient service not available.");

            var tools = mcpClient.ListToolsAsync().GetAwaiter().GetResult();

            foreach (var (key, instructionData) in allInstructions)
            {
                var filteredTools = tools
                    .Where(t => instructionData.Metadata.Tools.Contains(t.Name))
                    .ToList();

                var agent = chatClient.CreateAIAgent(
                    name: instructionData.Metadata.Name,
                    instructions: instructionData.Content,
                    tools: filteredTools.Cast<AITool>().ToList()
                );

                agentPool.AddAgent(instructionData.Metadata.Id, agent, instructionData.Metadata);
            }

            return agentPool;
        });

        builder.Services.AddSingleton<ConversationLoop>();

        return builder;
    }
}
