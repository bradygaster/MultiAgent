public abstract class BaseTool(ILogger<BaseTool> logger)
{
    public string LogAndReturn(string input)
    {
        logger.LogInformation(input);
        return input;
    }
}
