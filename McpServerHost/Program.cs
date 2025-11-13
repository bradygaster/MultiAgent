var builder = WebApplication.CreateBuilder(args);

// Configure MCP server
builder.Services
       .AddMcpServer()
       .WithHttpTransport()
       .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp();

app.Run();
