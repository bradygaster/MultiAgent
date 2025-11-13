var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure MCP server
builder.Services
       .AddMcpServer()
       .WithHttpTransport()
       .WithToolsFromAssembly();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapMcp();

app.Run();
