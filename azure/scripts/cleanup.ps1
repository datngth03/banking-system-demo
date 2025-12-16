# ============================================================================
# Banking System - Unified Cleanup Script
# Xóa toàn b? Azure resources cho m?t environment
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-banking-$Environment",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipConfirmation,
    
    [Parameter(Mandatory=$false)]
    [switch]$Nuclear  # Force delete t?ng resource khi b? stuck
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Red
Write-Host "  Banking System - Azure Cleanup" -ForegroundColor Red
Write-Host "  Environment: $Environment" -ForegroundColor Red
if ($Nuclear) {
    Write-Host "  Mode: NUCLEAR (Delete individual resources)" -ForegroundColor Magenta
} else {
    Write-Host "  Mode: FAST (Delete resource group)" -ForegroundColor Yellow
}
Write-Host "============================================" -ForegroundColor Red
Write-Host ""

# ============================================================================
# FUNCTIONS
# ============================================================================

function Show-Confirmation {
    Write-Host "??  WARNING: This will DELETE:" -ForegroundColor Yellow
    Write-Host "  - Resource Group: $ResourceGroupName" -ForegroundColor Yellow
    Write-Host "  - ALL resources inside (databases, containers, secrets, etc.)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "?? THIS ACTION CANNOT BE UNDONE!" -ForegroundColor Red
    Write-Host ""
    
    $confirmation = Read-Host "Type 'DELETE' to confirm"
    
    if ($confirmation -ne 'DELETE') {
        Write-Host "? Cleanup cancelled" -ForegroundColor Green
        exit 0
    }
}

function Test-ResourceGroupExists {
    Write-Host "Checking resource group: $ResourceGroupName..." -ForegroundColor Cyan
    
    $rgExists = az group exists --name $ResourceGroupName 2>$null | ConvertFrom-Json
    
    if (-not $rgExists) {
        Write-Host "? Resource group does not exist. Nothing to clean up." -ForegroundColor Green
        exit 0
    }
    
    Write-Host "?? Resource group found" -ForegroundColor Yellow
}

function Show-Resources {
    Write-Host ""
    Write-Host "?? Listing resources to be deleted..." -ForegroundColor Cyan
    
    try {
        $resources = az resource list --resource-group $ResourceGroupName 2>$null | ConvertFrom-Json
        
        if ($resources.Count -eq 0) {
            Write-Host "? No resources found in resource group" -ForegroundColor Green
            return $false
        }
        
        Write-Host "Found $($resources.Count) resources:" -ForegroundColor Yellow
        $resources | ForEach-Object {
            Write-Host "  - $($_.name) ($($_.type))" -ForegroundColor White
        }
        
        return $true
    }
    catch {
        Write-Host "??  Could not list resources (may be empty)" -ForegroundColor Yellow
        return $false
    }
}

function Remove-ResourceLocks {
    Write-Host ""
    Write-Host "?? Step 1: Removing locks..." -ForegroundColor Cyan
    
    try {
        $locks = az lock list --resource-group $ResourceGroupName 2>$null | ConvertFrom-Json
        
        if ($locks.Count -gt 0) {
            foreach ($lock in $locks) {
                Write-Host "  Removing lock: $($lock.name)" -ForegroundColor Yellow
                az lock delete --ids $lock.id 2>$null
            }
            Write-Host "? Locks removed" -ForegroundColor Green
        } else {
            Write-Host "? No locks found" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "??  Could not check/remove locks" -ForegroundColor Yellow
    }
}

function Remove-ResourcesIndividually {
    Write-Host ""
    Write-Host "?? Step 2: Deleting resources individually (Nuclear mode)..." -ForegroundColor Magenta
    
    $resources = az resource list --resource-group $ResourceGroupName 2>$null | ConvertFrom-Json
    
    if ($resources.Count -eq 0) {
        Write-Host "? No resources to delete" -ForegroundColor Green
        return
    }
    
    # Delete in specific order to avoid dependency issues
    $deletionOrder = @(
        @{ Type = "Microsoft.App/containerApps"; Name = "Container Apps" }
        @{ Type = "Microsoft.App/managedEnvironments"; Name = "Container Apps Environment" }
        @{ Type = "Microsoft.DBforPostgreSQL/flexibleServers"; Name = "PostgreSQL Servers" }
        @{ Type = "Microsoft.Cache/redis"; Name = "Redis Cache" }
        @{ Type = "Microsoft.ContainerRegistry/registries"; Name = "Container Registry" }
        @{ Type = "Microsoft.KeyVault/vaults"; Name = "Key Vault" }
        @{ Type = "Microsoft.Insights/components"; Name = "Application Insights" }
        @{ Type = "Microsoft.OperationalInsights/workspaces"; Name = "Log Analytics" }
    )
    
    foreach ($item in $deletionOrder) {
        Write-Host ""
        Write-Host "  Deleting $($item.Name)..." -ForegroundColor Yellow
        
        $resourcesOfType = $resources | Where-Object { $_.type -eq $item.Type }
        
        if ($resourcesOfType.Count -eq 0) {
            Write-Host "    No $($item.Name) found" -ForegroundColor Gray
            continue
        }
        
        foreach ($resource in $resourcesOfType) {
            Write-Host "    Deleting: $($resource.name)" -ForegroundColor White
            
            try {
                # Special handling for different resource types
                switch ($item.Type) {
                    "Microsoft.App/containerApps" {
                        az containerapp delete --name $resource.name --resource-group $ResourceGroupName --yes 2>$null
                    }
                    "Microsoft.App/managedEnvironments" {
                        az containerapp env delete --name $resource.name --resource-group $ResourceGroupName --yes 2>$null
                    }
                    "Microsoft.DBforPostgreSQL/flexibleServers" {
                        az postgres flexible-server delete --name $resource.name --resource-group $ResourceGroupName --yes 2>$null
                    }
                    "Microsoft.Cache/redis" {
                        az redis delete --name $resource.name --resource-group $ResourceGroupName --yes 2>$null
                    }
                    "Microsoft.ContainerRegistry/registries" {
                        az acr delete --name $resource.name --resource-group $ResourceGroupName --yes 2>$null
                    }
                    "Microsoft.KeyVault/vaults" {
                        az keyvault delete --name $resource.name --resource-group $ResourceGroupName 2>$null
                        # Purge to completely remove
                        Start-Sleep -Seconds 5
                        az keyvault purge --name $resource.name 2>$null
                    }
                    "Microsoft.Insights/components" {
                        az monitor app-insights component delete --app $resource.name --resource-group $ResourceGroupName 2>$null
                    }
                    "Microsoft.OperationalInsights/workspaces" {
                        az monitor log-analytics workspace delete --workspace-name $resource.name --resource-group $ResourceGroupName --yes 2>$null
                    }
                    default {
                        az resource delete --ids $resource.id 2>$null
                    }
                }
                
                Write-Host "      ? Deleted" -ForegroundColor Green
            }
            catch {
                Write-Host "      ? Failed: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
    }
    
    # Delete any remaining resources
    Write-Host ""
    Write-Host "  Deleting remaining resources..." -ForegroundColor Yellow
    $remainingResources = az resource list --resource-group $ResourceGroupName 2>$null | ConvertFrom-Json
    
    foreach ($resource in $remainingResources) {
        Write-Host "    Deleting: $($resource.name)" -ForegroundColor White
        az resource delete --ids $resource.id 2>$null
    }
    
    Write-Host "? Individual resource deletion completed" -ForegroundColor Green
}

function Remove-ResourceGroupFast {
    Write-Host ""
    Write-Host "???  Deleting resource group: $ResourceGroupName..." -ForegroundColor Cyan
    Write-Host "  This is faster - Azure deletes everything in background" -ForegroundColor Gray
    
    az group delete --name $ResourceGroupName --yes --no-wait
    
    Write-Host "? Deletion initiated in background" -ForegroundColor Green
}

function Wait-ForDeletion {
    Write-Host ""
    Write-Host "? Monitoring deletion progress..." -ForegroundColor Cyan
    
    $maxWaitMinutes = 15
    $waitedSeconds = 0
    $checkInterval = 10  # Check every 10 seconds
    
    while ($waitedSeconds -lt ($maxWaitMinutes * 60)) {
        Start-Sleep -Seconds $checkInterval
        $waitedSeconds += $checkInterval
        
        $rgExists = az group exists --name $ResourceGroupName 2>$null | ConvertFrom-Json
        
        if (-not $rgExists) {
            $totalMinutes = [math]::Round($waitedSeconds / 60, 1)
            Write-Host ""
            Write-Host "??? Resource group deleted successfully! ???" -ForegroundColor Green
            Write-Host "Time taken: $totalMinutes minutes" -ForegroundColor Cyan
            return $true
        }
        
        # Show progress
        $elapsed = [math]::Round($waitedSeconds / 60, 1)
        Write-Host "  [$elapsed/$maxWaitMinutes min] Still deleting..." -ForegroundColor Yellow
    }
    
    # Timeout
    Write-Host ""
    Write-Host "??  Deletion timeout after $maxWaitMinutes minutes" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  1. Wait more - deletion may still be in progress" -ForegroundColor White
    Write-Host "  2. Check Azure Portal: https://portal.azure.com" -ForegroundColor White
    Write-Host "  3. Re-run with -Nuclear flag to force delete stuck resources" -ForegroundColor White
    Write-Host ""
    
    # Show remaining resources
    Write-Host "Checking for stuck resources..." -ForegroundColor Cyan
    $resources = az resource list --resource-group $ResourceGroupName 2>$null | ConvertFrom-Json
    
    if ($resources) {
        Write-Host "Resources still remaining:" -ForegroundColor Yellow
        $resources | ForEach-Object {
            Write-Host "  - $($_.name) ($($_.type))" -ForegroundColor White
        }
    }
    
    Write-Host ""
    Write-Host "Resource group will continue deleting in background." -ForegroundColor Yellow
    Write-Host "Check status: az group show --name $ResourceGroupName" -ForegroundColor Cyan
    
    return $false
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

try {
    # Step 0: Confirmation
    if (-not $SkipConfirmation) {
        Show-Confirmation
    }
    
    # Step 1: Check if resource group exists
    Test-ResourceGroupExists
    
    # Step 2: Show resources
    $hasResources = Show-Resources
    
    # Step 3: Execute deletion based on mode
    if ($Nuclear) {
        # NUCLEAR MODE: Delete each resource individually
        Write-Host ""
        Write-Host "?????? NUCLEAR MODE ACTIVATED ??????" -ForegroundColor Magenta
        Write-Host "This will delete resources one by one (slower but more thorough)" -ForegroundColor Yellow
        
        Remove-ResourceLocks
        Remove-ResourcesIndividually
        Remove-ResourceGroupFast
        $success = Wait-ForDeletion
    }
    else {
        # FAST MODE: Delete entire resource group
        Write-Host ""
        Write-Host "? FAST MODE" -ForegroundColor Yellow
        Write-Host "Deleting entire resource group at once (faster)" -ForegroundColor Yellow
        
        Remove-ResourceGroupFast
        $success = Wait-ForDeletion
    }
    
    # Exit with appropriate code
    if ($success) {
        Write-Host ""
        Write-Host "?? Cleanup completed successfully!" -ForegroundColor Green
        exit 0
    }
    else {
        Write-Host ""
        Write-Host "??  Cleanup may not be complete. Check Azure Portal." -ForegroundColor Yellow
        exit 1
    }
}
catch {
    Write-Host ""
    Write-Host "? Cleanup failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack trace: $($_.ScriptStackTrace)" -ForegroundColor Red
    exit 1
}
