using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using System.Diagnostics;

public class DefaultToolRegistry(IOptions<AgentHostSettings> agentSettings, IServiceProvider serviceProvider) : IToolRegistry
{
    public async Task<IDictionary<string, AITool>> GetAllToolsAsync()
    {
        // All the MCP Client tools available across all the servers
        var allAiTools = new Dictionary<string, AITool>();

        // Load in tools from all MCP servers
        using (var session = TelemetryConfig.ApplicationTelemetrySource.StartActivity("MCP Tool Indexing", ActivityKind.Server))
        {
            foreach (var mcpServer in agentSettings.Value.McpServers)
            {
                var mcpClient = serviceProvider.GetKeyedService<McpClient>(mcpServer);
                if (mcpClient == null)
                    throw new InvalidOperationException($"McpClient {mcpServer} not available.");
                var tools = await mcpClient.ListToolsAsync();
                foreach (var tool in tools)
                {
                    if (!allAiTools.ContainsKey(tool.Name))
                    {
                        allAiTools[tool.Name] = tool;
                    }
                }
            }
        }

        return allAiTools;
    }
}