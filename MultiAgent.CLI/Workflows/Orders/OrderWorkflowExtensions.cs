namespace Microsoft.Extensions.Hosting;

public static class OrderWorkflowExtensions
{
    public static IHostApplicationBuilder AddOrderWorkflow(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IOrderGenerator, StaticOrderGenerator>();
        builder.Services.AddSingleton<OrderWorkflowDefinition>();
        builder.Services.AddHostedService<OrderSimulatingWorker>();
        return builder;
    }
}