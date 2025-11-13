using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using ModelContextProtocol.Server;
using OpenAI;

namespace Microsoft.Extensions.Hosting;

internal static class Extensions
{
    public static IHostApplicationBuilder AddSettings(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddUserSecrets<Program>();
        builder.Services.Configure<AzureSettings>(builder.Configuration.GetSection("Azure"));
        return builder;
    }

    public static IHostApplicationBuilder AddServices(this IHostApplicationBuilder builder)
    {
        builder.AddMcpClient();
        builder.Services.AddSingleton<ConsoleClient>();
        builder.Services.AddSingleton<InstructionLoader>();

        _ = builder.Services.AddSingleton<AgentPool>(services =>
        {
            var azureOptions = services.GetRequiredService<IOptions<AzureSettings>>().Value;
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
                .AsIChatClient();

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

    public static IHostApplicationBuilder AddMcpClient(this IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<McpClient>( sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            McpClientOptions mcpClientOptions = new()
            {
                ClientInfo = new()
                {
                    Name = "AspNetCoreSseClient",
                    Version = "1.0.0"
                }
            };

            using var mcpClient = McpClient.CreateAsync(
                    new HttpClientTransport(new()
                    {
                        Endpoint = new Uri("https://localhost:7148"),
                    }), mcpClientOptions, loggerFactory);

            var result = mcpClient.ConfigureAwait(false).GetAwaiter().GetResult();

            return result;
        });

        return builder;
    }
}
