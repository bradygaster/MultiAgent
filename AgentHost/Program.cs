var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.AddSettings();
builder.AddServices();

var host = builder.Build();
host.UseCors();
host.MapDefaultEndpoints();
host.MapHub<DashboardHub>("/orderstatus");
host.MapGet("/", () => "MultiAgent Status Hub - SignalR endpoint at /orderstatus");

host.Run();