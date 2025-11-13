using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSettings();
builder.AddServices();

var host = builder.Build();

try
{
    var conversation = host.Services.GetRequiredService<ConversationLoop>();
    await conversation.Chat();
}
catch (Exception ex)
{
    host.Services.GetRequiredService<ConsoleClient>().Print($"\nError: {ex.Message}", ConsoleColor.Red);
}