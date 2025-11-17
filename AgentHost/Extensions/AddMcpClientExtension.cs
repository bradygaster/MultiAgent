using ModelContextProtocol.Client;

namespace Microsoft.Extensions.Hosting;

public static class AddMcpClientExtension
{
    public static IHostApplicationBuilder AddMcpClient(this IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<McpClient>(sp =>
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
