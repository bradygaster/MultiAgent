namespace Microsoft.Extensions.Hosting;

internal static class AddServicesExtension
{
    public static IHostApplicationBuilder AddServices(this IHostApplicationBuilder builder)
    {
        builder.AddMcpClients();
        builder.Services.AddSingleton<IChatClientProvider, AzureOpenAIChatClientProvider>();
        builder.Services.AddSingleton<IToolRegistry, DefaultToolRegistry>();
        builder.Services.AddSingleton<IAgentRegistry, DefaultAgentRegistry>();
        builder.Services.AddSingleton<InstructionLoader>();
        builder.Services.AddSingleton<WorkflowEventPublisher>();
        builder.Services.AddSingleton<WorkflowFactory>();
        builder.Services.AddSingleton<AgentPool>();

        // add workflows
        builder.AddOrderWorkflow();

        return builder;
    }
}
