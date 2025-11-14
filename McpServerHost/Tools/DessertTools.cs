using ModelContextProtocol.Server;
using System.ComponentModel;

[McpServerToolType]
public class DessertTools(ILogger<DessertTools> logger)
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
}


