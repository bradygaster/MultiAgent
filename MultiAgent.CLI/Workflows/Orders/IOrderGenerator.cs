public interface IOrderGenerator
{
    Task<string> GenerateRandomOrder();
}