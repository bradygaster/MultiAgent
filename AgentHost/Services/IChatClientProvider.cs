using Microsoft.Extensions.AI;

public interface IChatClientProvider
{
    Task<IChatClient> CreateChatClient();
}