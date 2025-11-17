var builder = DistributedApplication.CreateBuilder(args);

var mcpHost = builder.AddProject<Projects.McpHost>("mcpHost");

var orderSimulator = builder.AddProject<Projects.AgentHost>("agentHost")
       .WaitFor(mcpHost)
       .WithReference(mcpHost);

// Add React Flow workflow visualizer
var frontend = builder.AddViteApp("workflow-visualizer", "../workflow-visualizer")
    .WithReference(orderSimulator)
    .WithEnvironment("VITE_SIGNALR_HUB_URL", orderSimulator.GetEndpoint("http"))
    .PublishAsDockerFile();

builder.Build().Run();
