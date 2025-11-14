var builder = DistributedApplication.CreateBuilder(args);

var mcpserverhost = builder.AddProject<Projects.McpServerHost>("mcpserverhost");

var orderSimulator = builder.AddProject<Projects.MultiAgent_OrderSimulator>("orderSimulator")
       .WaitFor(mcpserverhost)
       .WithReference(mcpserverhost);

// Add React Flow workflow visualizer
var workflow_visualizer = builder.AddViteApp("workflow-visualizer", "../workflow-visualizer")
    .WithReference(orderSimulator)
    .WithEnvironment("VITE_SIGNALR_HUB_URL", orderSimulator.GetEndpoint("http"))
    .PublishAsDockerFile();

builder.Build().Run();
