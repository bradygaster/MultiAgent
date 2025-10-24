#!/usr/bin/env pwsh
# down.ps1
# This script will clean up only the resources created by the up.ps1 script,
# including purging any soft-deleted Cognitive Services accounts created by up.ps1

# Determine the script's directory to use proper paths regardless of where the script is called from
$scriptDir = $PSScriptRoot
Write-Host "Script directory: $scriptDir"

Write-Host "Starting cleanup of Azure resources..." -ForegroundColor Yellow

# Extract the unique suffix from the resource group name
# This should match how the uniqueSuffix is calculated in main.bicep
$resourcePrefix = "custom-agent"

# Get resource group created by up.ps1
Write-Host "Looking for resource group created by up.ps1..."
$deploymentName = "custom-agent-deployment-$(Get-Date -Format 'yyyyMMdd')"
$deployment = az deployment sub show --name $deploymentName 2>$null | ConvertFrom-Json

# If today's deployment isn't found, try with yesterday's date (in case it was deployed yesterday)
if (-not $deployment) {
    $yesterdayDate = (Get-Date).AddDays(-1).ToString('yyyyMMdd')
    $deploymentName = "custom-agent-deployment-$yesterdayDate"
    $deployment = az deployment sub show --name $deploymentName 2>$null | ConvertFrom-Json
    if ($deployment) {
        Write-Host "Found deployment from yesterday: $deploymentName" -ForegroundColor Green
    }
}

if ($deployment) {
    # Get the resource group from the deployment outputs
    $resourceGroupName = $deployment.properties.outputs.resourceGroupName.value
    Write-Host "Found resource group from deployment: $resourceGroupName" -ForegroundColor Green
} else {    # Fallback to looking for resource groups matching the pattern
    Write-Host "No active deployment found. Looking for resource groups matching pattern 'rg-$resourcePrefix-*'..."
    $resourceGroups = az group list --query "[?starts_with(name, 'rg-$resourcePrefix-')].name" -o json | ConvertFrom-Json
    
    if ($resourceGroups.Count -eq 0) {
        Write-Host "No matching resource groups found. Nothing to clean up!" -ForegroundColor Green
        exit 0
    }
    
    # We'll only process the most recently created resource group
    $resourceGroupName = $resourceGroups[0]
    Write-Host "Using most recent matching resource group: $resourceGroupName" -ForegroundColor Yellow
}

# Extract the unique suffix from the resource group name
if ($resourceGroupName -match "rg-$resourcePrefix-(.*)")
{
    $uniqueSuffix = $Matches[1]
    Write-Host "Found resource group: $resourceGroupName with suffix: $uniqueSuffix"
      # 1. Get the Cognitive Services account name
    $foundryResourceName = "ca-foundry-$uniqueSuffix"
    Write-Host "Cognitive Services account name: $foundryResourceName"
    
    # 2. Delete the resource group
    Write-Host "Deleting resource group: $resourceGroupName..." -ForegroundColor Yellow
    $deleteResult = az group delete --name $resourceGroupName --yes --no-wait
    Write-Host "Resource group deletion initiated. Waiting for completion..."
    
    # 3. Wait for resource group deletion to complete
    $checkInterval = 30  # seconds
    $timeout = 600  # 10 minutes
    $elapsed = 0
    
    do {
        Start-Sleep -Seconds $checkInterval
        $elapsed += $checkInterval
        $groupExists = az group exists --name $resourceGroupName
        Write-Host "Checking if resource group still exists... ($elapsed seconds elapsed)"
        
        if ($elapsed -ge $timeout) {
            Write-Host "Timeout waiting for resource group deletion. Continuing with cleanup..." -ForegroundColor Yellow
            break
        }
    } while ($groupExists -contains "true")
    
    # 4. Check for and purge the specific soft-deleted Cognitive Services account
    Write-Host "Checking for soft-deleted Cognitive Services account: $foundryResourceName..." -ForegroundColor Yellow
    
    # Get location from up.ps1 (typically eastus)
    $location = "eastus" # Default location used in up.ps1
    
    # Try to purge the specific account
    Write-Host "Attempting to purge soft-deleted cognitive service: $foundryResourceName..."    $purgeResult = az cognitiveservices account purge --name $foundryResourceName --resource-group $resourceGroupName --location $location 2>$null
    if ($?) {
        Write-Host "Purged soft-deleted cognitive service: $foundryResourceName" -ForegroundColor Green
    } else {
        Write-Host "No soft-deleted cognitive service found with name: $foundryResourceName" -ForegroundColor Yellow
    }
    
    # 5. Clean up the role assignment created by up.ps1
    Write-Host "Cleaning up role assignment..." -ForegroundColor Yellow
    $subId = az account show --query id --output tsv
    $objectId = az ad signed-in-user show --query id -o tsv
    
    # Delete the specific Azure AI Developer role assignment created by up.ps1
    $aiDeveloperRoleId = "f6c7c914-8db3-469d-8ca1-694a8f32e121"  # Azure AI Developer role ID
    $roleResult = az role assignment delete --role $aiDeveloperRoleId --assignee-object-id $objectId --scope "/subscriptions/$subId/resourceGroups/$resourceGroupName" --assignee-principal-type 'User' --yes 2>$null
    if ($?) {
        Write-Host "Role assignment deleted successfully" -ForegroundColor Green
    } else {
        Write-Host "No matching role assignment found" -ForegroundColor Yellow
    }
      # Clean up the user secrets set by up.ps1
    Write-Host "Removing user secrets..."
    $CSHARP_PROJECT_PATH = Join-Path -Path $scriptDir -ChildPath "..\CustomAgent.csproj"
    dotnet user-secrets remove "Azure:Endpoint" --project $CSHARP_PROJECT_PATH 2>$null
    dotnet user-secrets remove "Azure:ModelName" --project $CSHARP_PROJECT_PATH 2>$null
}

# Clean up the deployment itself
Write-Host "Cleaning up deployment..." -ForegroundColor Yellow
$deploymentName = "custom-agent-deployment"
az deployment sub delete --name $deploymentName 2>$null

# Remove the output.json file if it exists
$outputJsonPath = Join-Path -Path $scriptDir -ChildPath "output.json"
if (Test-Path -Path $outputJsonPath) {
    Remove-Item -Path $outputJsonPath -Force
    Write-Host "Removed output.json file" -ForegroundColor Green
}

Write-Host "Cleanup complete! All resources created by up.ps1 have been deleted and purged." -ForegroundColor Green
