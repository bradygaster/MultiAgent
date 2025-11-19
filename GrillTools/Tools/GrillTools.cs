using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public class GrillTools(ILogger<GrillTools> logger)
{
    private string LogAndReturn(string message)
    {
        Task.Delay(CentralStatics.DefaultTimeout).Wait(); // Simulate a delay
        logger.LogInformation(message);
        return message;
    }

    [McpServerTool(Name = "cook_patty"), Description("Grill a beef patty.")]
    public string CookPatty(CookPattyRequest request) => LogAndReturn($"🥩 Cooking {request.PattyType} patty to {request.Doneness} doneness... Done! Perfectly cooked patty ready.");

    [McpServerTool(Name = "melt_cheese"), Description("Melt cheese on a burger patty.")]
    public string MeltCheese(MeltCheeseRequest request) => LogAndReturn($"🧀 Melting {request.CheeseType} cheese on the patty... Perfect melt achieved!");

    [McpServerTool(Name = "add_bacon"), Description("Add crispy bacon strips to a burger.")]
    public string AddBacon(AddBaconRequest request) => LogAndReturn($"🥓 Adding {request.BaconStrips} strips of crispy bacon... Bacon perfectly placed!");

    [McpServerTool(Name = "toast_bun"), Description("Toast burger buns to specified level.")]
    public string ToastBun(ToastBunRequest request) => LogAndReturn($"🍞 Toasting {request.BunType} bun to {request.ToastLevel}... Golden brown perfection!");

    [McpServerTool(Name = "assemble_burger"), Description("Assemble a burger with specified components.")]
    public string AssembleBurger(AssembleBurgerRequest request) => LogAndReturn($"🍔 Assembling burger with {request.Components}... Perfectly assembled burger ready!");
}


