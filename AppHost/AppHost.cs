using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var dessertTools = builder.AddProject<Projects.DessertTools>("dessertTools");
var expoTools = builder.AddProject<Projects.ExpoTools>("expoTools");
var fryerTools = builder.AddProject<Projects.FryerTools>("fryerTools");
var grillTools = builder.AddProject<Projects.GrillTools>("grillTools");

// Wire up the Agent Host
var agentHost = builder.AddProject<Projects.AgentHost>("agentHost")
       .WaitFor(dessertTools)
       .WithReference(dessertTools)
       .WaitFor(expoTools)
       .WithReference(expoTools)
       .WaitFor(fryerTools)
       .WithReference(fryerTools)
       .WaitFor(grillTools)
       .WithReference(grillTools);

// Add React Flow workflow visualizer
var frontend = builder.AddViteApp("workflow-visualizer", "../workflow-visualizer")
    .WithReference(agentHost)
    .WithEnvironment("VITE_SIGNALR_HUB_URL", agentHost.GetEndpoint("http"))
    .PublishAsDockerFile();

builder.Build().Run();
