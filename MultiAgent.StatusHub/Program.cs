using MultiAgent.StatusHub.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSignalR();

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

var app = builder.Build();

app.UseCors();

app.MapDefaultEndpoints();

app.MapHub<OrderStatusHub>("/orderstatus");

app.MapGet("/", () => "MultiAgent Status Hub - SignalR endpoint at /orderstatus");

app.Run();
