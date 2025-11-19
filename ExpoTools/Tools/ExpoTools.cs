using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public class ExpoTools(ILogger<ExpoTools> logger)
{
    private string LogAndReturn(string message)
    {
        Task.Delay(CentralStatics.DefaultTimeout).Wait(); // Simulate a delay
        logger.LogInformation(message);
        return message;
    }

    [McpServerTool(Name = "plate_meal"), Description("Plate a meal with proper presentation.")]
    public string PlateMeal(PlateMealRequest request) => LogAndReturn($"🍽️ Plating meal for {request.Items} with {request.Accessories}... Meal beautifully presented!");

    [McpServerTool(Name = "package_for_takeout"), Description("Package food items for takeout.")]
    public string PackageTakeout(PackageTakeoutRequest request) => LogAndReturn($"📦 Packaging {request.Items} for takeout with {request.Accessories}... Order ready for pickup!");
}


