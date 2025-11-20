using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

public class AzureOpenAIChatClientProvider(IOptions<AzureSettings> azureSettings) : IChatClientProvider
{
    public async Task<IChatClient> CreateChatClient()
    {
        // Validate required Azure settings
        if (string.IsNullOrWhiteSpace(azureSettings.Value.ModelName))
            throw new InvalidOperationException("Azure:ModelName not configured.");
        if (string.IsNullOrWhiteSpace(azureSettings.Value.Endpoint))
            throw new InvalidOperationException("Azure:Endpoint not configured.");

        // Create chat client once
        var cred = new ChainedTokenCredential(
            new AzureCliCredential(),
            new EnvironmentCredential(),
            new ManagedIdentityCredential()
        );

        var chatClient = new AzureOpenAIClient(new Uri(azureSettings.Value.Endpoint), cred)
            .GetChatClient(azureSettings.Value.ModelName)
            .AsIChatClient()
            .AsBuilder()
            .UseOpenTelemetry(sourceName: "agent-telemetry-source")
            .Build();

        return chatClient;
    }
}