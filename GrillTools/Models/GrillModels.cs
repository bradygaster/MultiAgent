// Grill tool models
public record CookPattyRequest(string PattyType, string Doneness);
public record MeltCheeseRequest(string CheeseType);
public record ToastBunRequest(string BunType, string ToastLevel);
public record AssembleBurgerRequest(string Components);
public record AddBaconRequest(int BaconStrips);
