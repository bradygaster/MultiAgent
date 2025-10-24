using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
        builder.Services.AddSingleton<ConsoleClient>();
        builder.Services.AddSingleton<InstructionLoader>();
        
        builder.Services.AddSingleton<AgentPool>(services =>
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
                .GetChatClient(azureOptions.ModelName);
                
            // Create agent pool and populate it
            var agentPool = new AgentPool();
            
            foreach (var (key, instructionData) in allInstructions)
            {
                var agent = chatClient.CreateAIAgent(
                    name: instructionData.Metadata.Name,
                    instructions: instructionData.Content
                );
                
                agentPool.AddAgent(instructionData.Metadata.Id, agent, instructionData.Metadata);
            }
            
            return agentPool;
        });

        builder.Services.AddSingleton<ConversationLoop>();
        
        return builder;
    }
}
