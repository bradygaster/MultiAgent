using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public class ShakeTool(ILogger<ShakeTool> logger)
{
    private string LogAndReturn(string input)
    {
        logger.LogInformation(input);
        return input;
    }

    [McpServerTool(Name = "make_shake"), Description("Make a milkshake.")]
    public string MakeShake(MakeShakeRequest request) => LogAndReturn($"🥤 Making {request.Size} {request.Flavor} shake with {request.Toppings}... Creamy shake ready!");

    [McpServerTool(Name = "make_sundae"), Description("Make a sundae.")]
    public string MakeShake(MakeSundaeRequest request) => LogAndReturn($"🍨 Making {request.Size} sundae with {request.Flavor} ice cream and {request.Toppings}... Delicious sundae ready!");

    [McpServerTool(Name = "add_whipped_cream"), Description("Add whipped cream to a dessert.")]
    public string MakeWhippedCream(AddWhippedCreamRequest request) => LogAndReturn($"🍦 Adding {request.Amount} whipped cream... Perfect fluffy topping added!");

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
            : LogAndReturn($"");
    }

    [McpServerTool(Name = "bag_fries_for_order"), Description("Bag an order of fries to prep them for deliverty.")]
    public string BagFriesForOrder() => LogAndReturn($"🍟 Bagging up order of fries ... Fries ready!");

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

    [McpServerTool(Name = "plate_meal"), Description("Plate a meal with proper presentation.")]
    public string PlateMeal(PlateMealRequest request) => LogAndReturn($"🍽️ Plating meal for {request.Items} with {request.Accessories}... Meal beautifully presented!");

    [McpServerTool(Name = "package_for_takeout"), Description("Package food items for takeout.")]
    public string PackageTakeout(PackageTakeoutRequest request) => LogAndReturn($"📦 Packaging {request.Items} for takeout with {request.Accessories}... Order ready for pickup!");
}


