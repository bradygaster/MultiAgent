using Microsoft.Extensions.Options;
using YamlDotNet.Serialization;

public class InstructionData
{
    public AgentMetadata Metadata { get; set; } = new();
    public string Content { get; set; } = string.Empty;
}

public class InstructionLoader(IOptions<MultiAgentSettings> settings, ILogger<MultiAgentSettings> logger)
{
    public Dictionary<string, InstructionData> LoadAllInstructions()
    {
        var basePath = AppContext.BaseDirectory;

        var instructionsPath = Path.Combine(basePath, settings.Value.InstructionsPath);
        logger.LogInformation($"Base path for instructions: {instructionsPath}");

        var instructionFiles = Directory.GetFiles(instructionsPath, "*.md");

        var instructions = new Dictionary<string, InstructionData>();
        
        foreach (var file in instructionFiles)
        {
            logger.LogInformation($"Loading instruction file: {file}");
            var fileName = Path.GetFileName(file);
            var key = Path.GetFileNameWithoutExtension(file); // Use filename without extension as key
            
            try
            {
                instructions[key] = LoadInstruction(fileName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load instruction '{fileName}': {ex.Message}", ex);
            }
        }
        
        return instructions;
    }
    
    public InstructionData LoadInstruction(string fileName)
    {
        var basePath = AppContext.BaseDirectory;
        var instructionPath = Path.Combine(basePath, settings.Value.InstructionsPath, fileName);
        
        if (!File.Exists(instructionPath))
            throw new FileNotFoundException($"Instruction file '{fileName}' not found in the instructions directory.");

        // Read the instruction file
        var instructionContent = File.ReadAllText(instructionPath);
        
        // Parse front matter and content
        var (metadata, content) = ParseFrontMatter(instructionContent);
        
        return new InstructionData
        {
            Metadata = metadata,
            Content = instructionContent
        };
    }
    
    private (AgentMetadata metadata, string content) ParseFrontMatter(string fileContent)
    {
        var metadata = new AgentMetadata();
        var content = fileContent;

        if (fileContent.StartsWith("---"))
        {
            var endOfFrontMatter = fileContent.IndexOf("---", 3);
            if (endOfFrontMatter > 0)
            {
                var frontMatterYaml = fileContent.Substring(4, endOfFrontMatter - 4).Trim();
                content = fileContent.Substring(endOfFrontMatter + 3).Trim();

                try
                {
                    var deserializer = new DeserializerBuilder()
                        .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                        .Build();
                    metadata = deserializer.Deserialize<AgentMetadata>(frontMatterYaml) ?? new AgentMetadata();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to parse front matter: {ex.Message}", ex);
                }
            }
        }

        return (metadata, content);
    }
}