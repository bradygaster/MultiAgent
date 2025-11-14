namespace Microsoft.Extensions.Hosting;

public static class AddSettingsExtension
{
    public static IHostApplicationBuilder AddSettings(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
        builder.Configuration.AddUserSecrets<Program>();
        builder.Services.Configure<AzureSettings>(builder.Configuration.GetSection("Azure"));
        builder.Services.Configure<MultiAgentSettings>(builder.Configuration.GetSection("MultiAgent"));
        return builder;
    }
}