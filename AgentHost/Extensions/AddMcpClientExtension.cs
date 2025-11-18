using ModelContextProtocol.Client;

namespace Microsoft.Extensions.Hosting;

public static class AddMcpClientExtension
{
    public static IHostApplicationBuilder AddMcpClients(this IHostApplicationBuilder builder)
    {
        var multiAgentSettings = builder.Configuration.GetSection("MultiAgent").Get<MultiAgentSettings>();

        foreach (var mcpServer in multiAgentSettings.McpServers)
        {
            builder = builder.AddMcpClient(mcpServer);
        }


        return builder;
    }

    public static IHostApplicationBuilder AddMcpClient(this IHostApplicationBuilder builder, string referenceName)
    {
        builder.Services.AddKeyedTransient<McpClient>(referenceName, (sp, key) =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var config = sp.GetRequiredService<IConfiguration>();
            var multiAgentSettings = config.GetSection("MultiAgent").Get<MultiAgentSettings>();

            McpClientOptions mcpClientOptions = new()
            {
                ClientInfo = new()
                {
                    Name = "AspNetCoreSseClient",
                    Version = "1.0.0"
                }
            };

            var referenceTemplate = "services:{0}:https:0";
            var endpoint = config[string.Format(referenceTemplate, key)];

            using var mcpClient = McpClient.CreateAsync(
                    new HttpClientTransport(new()
                    {
                        Endpoint = new Uri(endpoint),
                    }), mcpClientOptions, loggerFactory);

            var result = mcpClient.ConfigureAwait(false).GetAwaiter().GetResult();

            return result;
        });

        return builder;
    }
}
