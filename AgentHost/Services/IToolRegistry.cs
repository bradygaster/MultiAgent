using Microsoft.Extensions.AI;

public interface IToolRegistry
{
    Task<IDictionary<string, AITool>> GetAllToolsAsync();
}