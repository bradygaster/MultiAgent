using MultiAgent.StatusHub.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseCors();

app.MapHub<OrderStatusHub>("/orderstatus");

app.MapGet("/", () => "MultiAgent Status Hub - SignalR endpoint at /orderstatus");

app.Run();
