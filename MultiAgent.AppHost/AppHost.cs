var builder = DistributedApplication.CreateBuilder(args);

var mcpserverhost = builder.AddProject<Projects.McpServerHost>("mcpserverhost");

var statushub = builder.AddProject<Projects.MultiAgent_StatusHub>("statushub")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.MultiAgent_OrderSimulator>("orderSimulator")
       .WaitFor(mcpserverhost)
       .WaitFor(statushub)
       .WithReference(mcpserverhost)
       .WithReference(statushub);

// Add React Flow workflow visualizer
var workflow_visualizer = builder.AddViteApp("workflow-visualizer", "../workflow-visualizer")
    .WithReference(statushub)
    .WithEnvironment("VITE_SIGNALR_HUB_URL", statushub.GetEndpoint("http"))
    .PublishAsDockerFile();

builder.Build().Run();
