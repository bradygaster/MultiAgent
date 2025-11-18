using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public class FryerTools(ILogger<FryerTools> logger)
{
    private string LogAndReturn(string message)
    {
        logger.LogInformation(message);
        return message;
    }

    [McpServerTool(Name = "fry_fries"), Description("Fry standard French fries.")]
    public string FryStandard(FryStandardRequest request) => LogAndReturn($"🍟 Frying {request.Portion} portion of standard fries for {request.Duration} minutes... Crispy golden fries ready!");

    [McpServerTool(Name = "fry_onion_rings"), Description("Fry standard French fries.")]
    public string FryOnionRings(FryOnionRingsRequest request) => LogAndReturn($"🧅 Frying {request.Portion} portion of onion rings for {request.Duration} minutes... Crispy golden fries ready!");

    [McpServerTool(Name = "fry_waffle_fries"), Description("Fry waffle-cut French fries.")]
    public string FryWaffle(FryWaffleRequest request) => LogAndReturn($"🧇 Frying {request.Portion} portion of waffle fries for {request.Duration} minutes... Crispy waffle-cut fries ready!");

    [McpServerTool(Name = "fry_sweet_potato_fries"), Description("Fry sweet potato fries.")]
    public string FrySweetPotato(FrySweetPotatoRequest request) => LogAndReturn($"🍠 Frying {request.Portion} portion of sweet potato fries for {request.Duration} minutes... Delicious sweet potato fries ready!");

    [McpServerTool(Name = "add_salt"), Description("Add salt to fries.")]
    public string AddSaltToFries(AddSaltRequest request)
    {
        return request.addSalt
            ? LogAndReturn("🧂 Adding salt to fries... Perfectly seasoned fries ready!")
            : LogAndReturn("No salt added to fries.");
    }

    [McpServerTool(Name = "bag_fries_for_order"), Description("Bag an order of fries to prep them for delivery.")]
    public string BagFriesForOrder() => LogAndReturn($"🍟 Bagging up order of fries ... Fries ready!");
}


