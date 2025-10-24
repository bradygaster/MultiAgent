# Custom Agents with Microsoft Agent Framework

This repository contains a flexible AI agent built using Microsoft's Agent Framework. The agent can be configured to specialize in any knowledge domain by loading instruction files with embedded metadata, allowing you to create custom AI assistants with specific expertise.

## Project Structure

- `Program.cs` - The main entry point using Host builder pattern
- `ConversationLoop.cs` - Handles the interactive chat loop with the AI agent
- `Extensions.cs` - Dependency injection configuration and service registration
- `Settings.cs` - Configuration classes for app and Azure settings
- `ConsoleClient.cs` - Console output utilities for colorized text display
- `InstructionLoader.cs` - Loads and processes instruction files with YAML front matter
- `appsettings.json` - Application configuration including instruction file selection
- `instructions/` - Directory containing the agent's knowledge base
  - `jabberwocky.md` - Sample knowledge about the Jabberwocky with embedded metadata
  - `quantum.md` - Sample knowledge about quantum computing with embedded metadata
- `prompts/` - Directory containing prompt templates
  - `prompt_template.md` - Configurable template for defining agent behaviors
- `infra/` - Infrastructure as Code for Azure deployment
  - `up.ps1` - PowerShell script to provision Azure resources
  - `down.ps1` - PowerShell script to tear down Azure resources
  - Various Bicep files for Azure resource definitions

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or later
- Azure subscription with appropriate permissions
- Azure CLI installed and configured
- PowerShell 7+ for running deployment scripts

## Key Dependencies

The application uses the following key NuGet packages:
- `Microsoft.Agents.AI.OpenAI` (v1.0.0-preview.251009.1) - Microsoft Agent Framework
- `Azure.AI.OpenAI` (v2.1.0) - Azure OpenAI integration
- `Azure.Identity` (v1.13.2) - Azure authentication
- `Microsoft.Extensions.Hosting` (v9.0.10) - .NET Host builder pattern
- `YamlDotNet` (v16.3.0) - YAML parsing for front matter metadata

## Setup Instructions

### 1. Clone the Repository

```powershell
git clone https://github.com/yourusername/CustomAgent.git
cd CustomAgent
```

### 2. Provision Azure Resources

Run the provisioning script to deploy necessary Azure resources and configure the application:

```powershell
.\infra\up.ps1
```

This script will:
- Create Azure AI Foundry resources including a GPT-4o model deployment
- Set up required IAM permissions (Azure AI Developer role)
- Configure the application's user secrets with proper Azure connection details

The script automatically configures these user secrets:
- `Azure:ModelName` - Set to "gpt-4o"
- `Azure:Endpoint` - The Azure AI Projects endpoint URL

### 3. Build and Run the Application

```powershell
dotnet build
dotnet run
```

## Sample Interaction

Here's what a typical interaction with the quantum computing agent looks like:

```
Quantum Computing Agent

Ask a question about Quantum Computing (type 'exit' to quit, 'save' to save the conversation):
What can you tell me about quantum computing?

Quantum computing is a field of computer science focused on developing computers that leverage the principles of quantum mechanics. Unlike classical computers, which use bits as the smallest unit of data (0 or 1), quantum computers use quantum bits, or qubits, which can exist in multiple states simultaneously due to superposition...

Ask a question about Quantum Computing (type 'exit' to quit, 'save' to save the conversation):
exit
```

The application provides streaming responses from the AI agent, displaying text as it's generated. The agent's name, welcome message, and prompt are all configured in the instruction file's front matter.

## Configuration

The application uses .NET's configuration system with user secrets for sensitive data and a simple JSON configuration file for instruction selection.

### Application Settings

Configure which instruction file to load in `appsettings.json`:

```json
{
  "InstructionFile": "quantum.md",
  "Azure": {
    "ModelName": "",
    "Endpoint": ""
  }
}
```

- `InstructionFile` - Specifies which markdown file from the `instructions/` directory to load
- Azure settings can be configured here or via user secrets (user secrets take precedence)

### Azure Settings (Required)

These are automatically set by the `up.ps1` script:

```powershell
dotnet user-secrets set "Azure:ModelName" "gpt-4o"
dotnet user-secrets set "Azure:Endpoint" "your-azure-endpoint"
```

### Agent Metadata (From Instruction Files)

All agent configuration is now read from the YAML front matter in instruction files. Each instruction file should start with:

```yaml
---
name: "Agent Name"
domain: "specialized domain"
tone: "scholarly but approachable"
welcome: "Welcome Message"
prompt: "Ask a question (type 'exit' to quit):"
---
```

## Instruction File Format

Instruction files combine YAML front matter with markdown content. The front matter contains all agent configuration, while the markdown content provides the knowledge base.

### Example Structure

```yaml
---
name: "Quantum Computing Expert"
domain: "Quantum Computing"
tone: "scholarly but approachable"
welcome: "Quantum Computing Agent"
prompt: "Ask a question about Quantum Computing (type 'exit' to quit):"
---

# Your Knowledge Base Content

This is where you put all the information about your domain...
```

### Front Matter Properties

- `name` - The agent's display name
- `domain` - The subject matter domain (used in prompt template)
- `tone` - The agent's communication style (used in prompt template)
- `welcome` - Welcome message displayed when the agent starts
- `prompt` - The input prompt shown to users

## Files Structure

### Instructions Files

The `instructions/` directory contains markdown files with YAML front matter that provide both agent configuration and knowledge base content. The application loads a single instruction file specified in `appsettings.json`. The repository includes two example instruction files:

- `instructions/jabberwocky.md` - Comprehensive information about the Jabberwocky creature and poem
- `instructions/quantum.md` - Introduction to quantum computing concepts

Each instruction file is self-contained with its own agent configuration in the front matter.

### Prompt Template

The `prompts/prompt_template.md` file defines the agent's behavior and response guidelines. It uses placeholders that are replaced with values from the instruction file's front matter:

- `{{DOMAIN_NAME}}` - Replaced with the `domain` property from front matter
- `{{TONE_STYLE}}` - Replaced with the `tone` property from front matter

This template system allows you to create specialized agents without modifying the code.

## Customizing Content

To create your own specialized agent:

1. **Create a new instruction file**: Add a new `.md` file to the `instructions/` directory with proper front matter
2. **Update configuration**: Change the `InstructionFile` setting in `appsettings.json` to point to your new file
3. **Optionally modify the prompt template**: Update `prompts/prompt_template.md` to define specific behaviors

### Example Custom Instruction File

```yaml
---
name: "Medical Assistant"
domain: "Medical Information"
tone: "professional and empathetic"
welcome: "Medical Information Assistant"
prompt: "How can I help you with medical information today? (type 'exit' to quit):"
---

# Medical Knowledge Base

## Important Disclaimer
This information is for educational purposes only and should not replace professional medical advice...

## Common Conditions
...your medical knowledge content here...
```

The `InstructionLoader` class automatically processes the selected instruction file and combines it with the prompt template to create the agent's complete instructions.

## Architecture

The application follows modern .NET patterns:

- **Host Builder Pattern**: Uses `Microsoft.Extensions.Hosting` for dependency injection and configuration
- **Streaming Responses**: Implements real-time streaming of AI responses using `RunStreamingAsync`
- **Azure Authentication**: Uses `DefaultAzureCredential` for seamless Azure authentication
- **Configuration Management**: Leverages .NET's configuration system with user secrets
- **Modular Design**: Separates concerns across multiple classes for maintainability

The main flow:
1. `Program.cs` sets up the host and starts the conversation
2. `Extensions.cs` configures services and creates the AI agent
3. `InstructionLoader.cs` loads the specified instruction file and parses YAML front matter
4. `ConversationLoop.cs` handles the interactive chat experience using metadata from front matter
5. `ConsoleClient.cs` provides colorized console output

## Error Handling

The application includes comprehensive error handling:

- Validates required Azure configuration at startup
- Checks for the existence of prompt and instruction files
- Validates YAML front matter parsing
- Gracefully handles agent communication errors
- Provides clear error messages with colorized console output

Common error scenarios:
- Missing Azure configuration: "Azure:ModelName not configured"
- Missing files: "Instruction file 'filename.md' not found"
- Invalid front matter: "Failed to parse front matter: ..."
- Agent communication issues: Displayed during chat interaction

## Cleanup

When you're done with the application, you can remove the Azure resources to avoid incurring costs:

```powershell
.\infra\down.ps1
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
