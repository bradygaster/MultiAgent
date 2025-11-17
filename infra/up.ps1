#!/usr/bin/env pwsh
# up.ps1
# This script deploys Azure resources for the Custom Agent project

Write-Host "Deploying the Azure resources..."

# Determine the script's directory to use proper paths regardless of where the script is called from
$scriptDir = $PSScriptRoot
Write-Host "Script directory: $scriptDir"

# Define resource group parameters
$RG_LOCATION = "eastus"
$MODEL_NAME = "gpt-4o"
$MODEL_VERSION = "2024-11-20"
$AI_PROJECT_FRIENDLY_NAME = "Multi Agent"
$MODEL_CAPACITY = 10
$DISABLE_LOCAL_AUTH = $true

# Deploy the Azure resources and save output to JSON
az deployment sub create `
  --name "custom-agent-deployment-$(Get-Date -Format 'yyyyMMddhhss')" `
  --location "$RG_LOCATION" `  --template-file "$scriptDir\main.bicep" `
  --parameters `
      aiProjectFriendlyName="$AI_PROJECT_FRIENDLY_NAME" `
      modelName="$MODEL_NAME" `
      modelCapacity="$MODEL_CAPACITY" `
      modelVersion="$MODEL_VERSION" `
      disableLocalAuth="$DISABLE_LOCAL_AUTH" `
      location="$RG_LOCATION" | Out-File -FilePath "$scriptDir\output.json" -Encoding utf8

# Parse the JSON file using native PowerShell cmdlets
$outputJsonPath = Join-Path -Path $scriptDir -ChildPath "output.json"
if (-not (Test-Path -Path $outputJsonPath)) {
    Write-Host "Error: output.json not found."
    exit -1
}

$jsonData = Get-Content $outputJsonPath -Raw | ConvertFrom-Json
$outputs = $jsonData.properties.outputs

# Extract values from the JSON object
$projectsEndpoint = $outputs.projectsEndpoint.value
$openAIEndpoint = $outputs.openAIEndpoint.value
$resourceGroupName = $outputs.resourceGroupName.value

if ([string]::IsNullOrEmpty($projectsEndpoint)) {
    Write-Host "Error: projectsEndpoint not found. Possible deployment failure."
    exit -1
}
if ([string]::IsNullOrEmpty($openAIEndpoint)) {
    Write-Host "Error: openAIEndpoint not found. Possible deployment failure."
    exit -1
}

# Set the C# project path relative to the script directory
$CSHARP_PROJECT_PATH = Join-Path -Path $scriptDir -ChildPath "..\AgentHost\AgentHost.csproj"

# Set the user secrets for the C# project
dotnet user-secrets set "Azure:Endpoint" "$openAIEndpoint" --project "$CSHARP_PROJECT_PATH"
dotnet user-secrets set "Azure:ModelName" "$MODEL_NAME" --project "$CSHARP_PROJECT_PATH"

# Delete the output.json file
$outputJsonPath = Join-Path -Path $scriptDir -ChildPath "output.json"
if (Test-Path -Path $outputJsonPath) {
    Remove-Item -Path $outputJsonPath -Force
}

Write-Host "Adding Azure AI Developer user role"

# Set Variables
$subId = az account show --query id --output tsv
$objectId = az ad signed-in-user show --query id -o tsv

$roleResult = az role assignment create --role "f6c7c914-8db3-469d-8ca1-694a8f32e121" `
                        --assignee-object-id "$objectId" `
                        --scope "subscriptions/$subId/resourceGroups/$resourceGroupName" `
                        --assignee-principal-type 'User'

# Check if the command failed
if ($LASTEXITCODE -ne 0) {
    Write-Host "User role assignment failed."
    exit 1
}

Write-Host "User role assignment succeeded."

# Verify static content files exist
Write-Host "Verifying prompt and instruction files exist..."

$promptDirPath = Join-Path -Path $scriptDir -ChildPath "..\prompts"
$instructionsDirPath = Join-Path -Path $scriptDir -ChildPath "..\instructions"
$promptTemplatePath = Join-Path -Path $promptDirPath -ChildPath "prompt_template.md"

# Check if directories and files exist
if (-not (Test-Path -Path $promptDirPath)) {
    Write-Host "Warning: prompts directory not found at $promptDirPath"
    Write-Host "Please ensure the prompts directory exists with appropriate template files."
}

if (-not (Test-Path -Path $instructionsDirPath)) {
    Write-Host "Warning: instructions directory not found at $instructionsDirPath"
    Write-Host "Please ensure the instructions directory exists with appropriate markdown files."
}

if (-not (Test-Path -Path $promptTemplatePath)) {
    Write-Host "Warning: prompt_template.md not found at $promptTemplatePath"
    Write-Host "Please ensure the prompt template file exists."
}

Write-Host "Agent configuration completed successfully."
