var builder = DistributedApplication.CreateBuilder(args);

var mcpserverhost = builder.AddProject<Projects.McpServerHost>("mcpserverhost");

builder.AddProject<Projects.MultiAgent_CLI>("multiagent-cli")
       .WaitFor(mcpserverhost)
       .WithReference(mcpserverhost);

builder.Build().Run();
