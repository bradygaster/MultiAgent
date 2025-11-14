var builder = DistributedApplication.CreateBuilder(args);

var mcpserverhost = builder.AddProject<Projects.McpServerHost>("mcpserverhost");

builder.AddProject<Projects.MultiAgent_OrderSimulator>("orderSimulator")
       .WaitFor(mcpserverhost)
       .WithReference(mcpserverhost);

// Add React Flow workflow visualizer
var workflow_visualizer = builder.AddViteApp("workflow-visualizer", "../workflow-visualizer")
    .PublishAsDockerFile();

builder.Build().Run();
