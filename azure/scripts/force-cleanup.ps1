# ============================================================================
# Banking System - Force Cleanup Script
# Xóa TOÀN B? resources khi teardown b? stuck
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-banking-$Environment"
)

$ErrorActionPreference = "Continue"  # Continue on errors

Write-Host "============================================" -ForegroundColor Red
Write-Host "  FORCE CLEANUP - Nuclear Option" -ForegroundColor Red
Write-Host "  Environment: $Environment" -ForegroundColor Red
Write-Host "============================================" -ForegroundColor Red
Write-Host ""

# ============================================================================
# Check if resource group exists
# ============================================================================

Write-Host "Checking resource group: $ResourceGroupName" -ForegroundColor Yellow

$rgExists = az group exists --name $ResourceGroupName | ConvertFrom-Json

if (-not $rgExists) {
    Write-Host "? Resource group does not exist. Nothing to clean up." -ForegroundColor Green
    exit 0
}

# ============================================================================
# Remove locks (if any)
# ============================================================================

Write-Host ""
Write-Host "Step 1: Removing locks..." -ForegroundColor Cyan

$locks = az lock list --resource-group $ResourceGroupName | ConvertFrom-Json

if ($locks.Count -gt 0) {
    foreach ($lock in $locks) {
        Write-Host "  Removing lock: $($lock.name)" -ForegroundColor Yellow
        az lock delete --ids $lock.id
    }
    Write-Host "? Locks removed" -ForegroundColor Green
} else {
    Write-Host "? No locks found" -ForegroundColor Green
}

# ============================================================================
# List all resources
# ============================================================================

Write-Host ""
Write-Host "Step 2: Listing resources..." -ForegroundColor Cyan

$resources = az resource list --resource-group $ResourceGroupName | ConvertFrom-Json

if ($resources.Count -eq 0) {
    Write-Host "? No resources found in resource group" -ForegroundColor Green
} else {
    Write-Host "Found $($resources.Count) resources:" -ForegroundColor Yellow
    $resources | ForEach-Object {
        Write-Host "  - $($_.name) ($($_.type))" -ForegroundColor White
    }
}

# ============================================================================
# Delete specific resources in order
# ============================================================================

Write-Host ""
Write-Host "Step 3: Deleting resources..." -ForegroundColor Cyan

# Delete Container Apps first
Write-Host ""
Write-Host "  Deleting Container Apps..." -ForegroundColor Yellow
$containerApps = $resources | Where-Object { $_.type -eq "Microsoft.App/containerApps" }
foreach ($app in $containerApps) {
    Write-Host "    Deleting: $($app.name)" -ForegroundColor White
    az containerapp delete --name $app.name --resource-group $ResourceGroupName --yes 2>$null
}

# Delete Container Apps Environment
Write-Host ""
Write-Host "  Deleting Container Apps Environment..." -ForegroundColor Yellow
$containerEnvs = $resources | Where-Object { $_.type -eq "Microsoft.App/managedEnvironments" }
foreach ($env in $containerEnvs) {
    Write-Host "    Deleting: $($env.name)" -ForegroundColor White
    az containerapp env delete --name $env.name --resource-group $ResourceGroupName --yes 2>$null
}

# Delete PostgreSQL servers
Write-Host ""
Write-Host "  Deleting PostgreSQL servers..." -ForegroundColor Yellow
$postgresServers = $resources | Where-Object { $_.type -eq "Microsoft.DBforPostgreSQL/flexibleServers" }
foreach ($server in $postgresServers) {
    Write-Host "    Deleting: $($server.name)" -ForegroundColor White
    az postgres flexible-server delete --name $server.name --resource-group $ResourceGroupName --yes 2>$null
}

# Delete Redis Cache
Write-Host ""
Write-Host "  Deleting Redis Cache..." -ForegroundColor Yellow
$redisCaches = $resources | Where-Object { $_.type -eq "Microsoft.Cache/redis" }
foreach ($redis in $redisCaches) {
    Write-Host "    Deleting: $($redis.name)" -ForegroundColor White
    az redis delete --name $redis.name --resource-group $ResourceGroupName --yes 2>$null
}

# Delete Container Registry
Write-Host ""
Write-Host "  Deleting Container Registry..." -ForegroundColor Yellow
$registries = $resources | Where-Object { $_.type -eq "Microsoft.ContainerRegistry/registries" }
foreach ($registry in $registries) {
    Write-Host "    Deleting: $($registry.name)" -ForegroundColor White
    az acr delete --name $registry.name --resource-group $ResourceGroupName --yes 2>$null
}

# Delete Key Vault (with purge protection disabled)
Write-Host ""
Write-Host "  Deleting Key Vault..." -ForegroundColor Yellow
$keyVaults = $resources | Where-Object { $_.type -eq "Microsoft.KeyVault/vaults" }
foreach ($vault in $keyVaults) {
    Write-Host "    Deleting: $($vault.name)" -ForegroundColor White
    az keyvault delete --name $vault.name --resource-group $ResourceGroupName 2>$null
    az keyvault purge --name $vault.name 2>$null
}

# Delete Application Insights
Write-Host ""
Write-Host "  Deleting Application Insights..." -ForegroundColor Yellow
$appInsights = $resources | Where-Object { $_.type -eq "Microsoft.Insights/components" }
foreach ($insight in $appInsights) {
    Write-Host "    Deleting: $($insight.name)" -ForegroundColor White
    az monitor app-insights component delete --app $insight.name --resource-group $ResourceGroupName 2>$null
}

# Delete Log Analytics
Write-Host ""
Write-Host "  Deleting Log Analytics..." -ForegroundColor Yellow
$logWorkspaces = $resources | Where-Object { $_.type -eq "Microsoft.OperationalInsights/workspaces" }
foreach ($workspace in $logWorkspaces) {
    Write-Host "    Deleting: $($workspace.name)" -ForegroundColor White
    az monitor log-analytics workspace delete --workspace-name $workspace.name --resource-group $ResourceGroupName --yes 2>$null
}

# Delete any remaining resources
Write-Host ""
Write-Host "  Deleting remaining resources..." -ForegroundColor Yellow
$remainingResources = az resource list --resource-group $ResourceGroupName | ConvertFrom-Json
foreach ($resource in $remainingResources) {
    Write-Host "    Deleting: $($resource.name)" -ForegroundColor White
    az resource delete --ids $resource.id 2>$null
}

# ============================================================================
# Delete resource group
# ============================================================================

Write-Host ""
Write-Host "Step 4: Deleting resource group..." -ForegroundColor Cyan

az group delete --name $ResourceGroupName --yes --no-wait

Write-Host "? Resource group deletion initiated" -ForegroundColor Green

# ============================================================================
# Wait for deletion
# ============================================================================

Write-Host ""
Write-Host "Step 5: Waiting for deletion to complete..." -ForegroundColor Cyan

$maxWaitMinutes = 15
$waitedMinutes = 0

while ($waitedMinutes -lt $maxWaitMinutes) {
    $rgExists = az group exists --name $ResourceGroupName | ConvertFrom-Json
    
    if (-not $rgExists) {
        Write-Host ""
        Write-Host "? Resource group deleted successfully!" -ForegroundColor Green
        exit 0
    }
    
    Write-Host "  Still deleting... ($waitedMinutes/$maxWaitMinutes minutes)" -ForegroundColor Yellow
    Start-Sleep -Seconds 60
    $waitedMinutes++
}

Write-Host ""
Write-Host "? Resource group still exists after $maxWaitMinutes minutes" -ForegroundColor Yellow
Write-Host "Please check Azure Portal for status" -ForegroundColor Yellow
Write-Host "Portal: https://portal.azure.com" -ForegroundColor Cyan

exit 1
