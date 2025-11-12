using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MultiAgent;

[McpServerToolType]
public static class ShakeTool
{
    [McpServerTool(Name = "make_shake"), Description("Make a milkshake.")]
    public static string MakeShake(MakeShakeRequest request) => $"🥤 Making {request.Size} {request.Flavor} shake with {request.Toppings}... Creamy shake ready!";
}

[McpServerToolType]
public static class SundaeTool
{
    [McpServerTool(Name = "make_sundae"), Description("Make a sundae.")]
    public static string MakeShake(MakeSundaeRequest request) => $"🍨 Making {request.Size} sundae with {request.Flavor} ice cream and {request.Toppings}... Delicious sundae ready!";
}

[McpServerToolType]
public static class WhippedCreamTool
{
    [McpServerTool(Name = "add_whipped_cream"), Description("Add whipped cream to a dessert.")]
    public static string MakeWhippedCream(AddWhippedCreamRequest request) => $"🍦 Adding {request.Amount} whipped cream... Perfect fluffy topping added!";
}

[McpServerToolType]
public static class FryStandardTool
{
    [McpServerTool(Name = "fry_fries"), Description("Fry standard French fries.")]
    public static string FryStandard(FryStandardRequest request) => $"🍟 Frying {request.Portion} portion of standard fries for {request.Duration} minutes... Crispy golden fries ready!";
}

[McpServerToolType]
public static class FryWaffleTool
{
    [McpServerTool(Name = "fry_waffle_fries"), Description("Fry waffle-cut French fries.")]
    public static string FryWaffle(FryWaffleRequest request) => $"🧇 Frying {request.Portion} portion of waffle fries for {request.Duration} minutes... Crispy waffle-cut fries ready!";
}

[McpServerToolType]
public static class FrySweetPotatoTool
{
    [McpServerTool(Name = "fry_sweet_potato_fries"), Description("Fry sweet potato fries.")]
    public static string FrySweetPotato(FrySweetPotatoRequest request) => $"🍠 Frying {request.Portion} portion of sweet potato fries for {request.Duration} minutes... Delicious sweet potato fries ready!";
}

[McpServerToolType]
public static class AddSalt
{
    [McpServerTool(Name = "add_salt"), Description("Add salt to fries.")]
    public static string AddSaltToFries(AddSaltRequest request)
    {
        return request.addSalt
            ? "🧂 Adding salt to fries... Perfectly seasoned fries ready!"
            : $"";
    }
}

[McpServerToolType]
public static class BagFriesTool
{
    [McpServerTool(Name = "bag_fries_for_order"), Description("Bag an order of fries to prep them for deliverty.")]
    public static string BagFriesForOrder() => $"🍟 Bagging up order of fries ... Fries ready!";
}

[McpServerToolType]
public static class GrillPattyTool
{
    [McpServerTool(Name = "grill_burger_patty"), Description("Cook a burger patty to specified doneness.")]
    public static string CookPatty(CookPattyRequest request) => $"🥩 Cooking {request.PattyType} patty to {request.Doneness} doneness... Done! Perfectly cooked patty ready.";
}

[McpServerToolType]
public static class MeltCheeseTool
{
    [McpServerTool(Name = "melt_cheese"), Description("Melt cheese on a burger patty.")]
    public static string MeltCheese(MeltCheeseRequest request) => $"🧀 Melting {request.CheeseType} cheese on the patty... Perfect melt achieved!";
}

[McpServerToolType]
public static class AddBaconTool
{
    [McpServerTool(Name = "add_bacon"), Description("Add crispy bacon strips to a burger.")]
    public static string AddBacon(AddBaconRequest request) => $"🥓 Adding {request.BaconStrips} strips of crispy bacon... Bacon perfectly placed!";
}

[McpServerToolType]
public static class GrillBunTool
{
    [McpServerTool(Name = "grill_bun"), Description("Toast burger buns to specified level.")]
    public static string ToastBun(ToastBunRequest request) => $"🍞 Toasting {request.BunType} bun to {request.ToastLevel}... Golden brown perfection!";
}

[McpServerToolType]
public static class AssembleBurgerTool
{
    [McpServerTool(Name = "assemble_burger"), Description("Assemble a burger with specified components.")]
    public static string AssembleBurger(AssembleBurgerRequest request) => $"🍔 Assembling burger with {request.Components}... Perfectly assembled burger ready!";
}

[McpServerToolType]
public static class PlateMealTool
{
    [McpServerTool(Name = "plate_meal"), Description("Plate a meal with proper presentation.")]
    public static string PlateMeal(PlateMealRequest request) => $"🍽️ Plating meal for {request.Service} with {request.Presentation}... Meal beautifully presented!";
}

[McpServerToolType]
public static class PackageTakeoutTool
{
    [McpServerTool(Name = "package_for_takeout"), Description("Package food items for takeout.")]
    public static string PackageTakeout(PackageTakeoutRequest request) => $"📦 Packaging {request.Items} for takeout with {request.Accessories}... Order ready for pickup!";
}


