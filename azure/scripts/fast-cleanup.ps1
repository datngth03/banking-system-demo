# ============================================================================
# Banking System - FAST Cleanup Script
# Xóa resource group tr?c ti?p (không xóa t?ng resource)
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-banking-$Environment",
    
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

Write-Host "============================================" -ForegroundColor Red
Write-Host "  FAST CLEANUP - Delete Resource Group" -ForegroundColor Red
Write-Host "  Environment: $Environment" -ForegroundColor Red
Write-Host "============================================" -ForegroundColor Red
Write-Host ""

# ============================================================================
# CONFIRMATION
# ============================================================================

if (-not $Force) {
    Write-Host "??  WARNING: This will DELETE:" -ForegroundColor Yellow
    Write-Host "  - Resource Group: $ResourceGroupName" -ForegroundColor Yellow
    Write-Host "  - ALL resources inside" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "?? THIS ACTION CANNOT BE UNDONE!" -ForegroundColor Red
    Write-Host ""
    
    $confirmation = Read-Host "Type 'DELETE' to confirm"
    
    if ($confirmation -ne 'DELETE') {
        Write-Host "Cleanup cancelled" -ForegroundColor Green
        exit 0
    }
}

# ============================================================================
# CHECK EXISTENCE
# ============================================================================

Write-Host "Checking resource group: $ResourceGroupName" -ForegroundColor Yellow

$rgExists = az group exists --name $ResourceGroupName | ConvertFrom-Json

if (-not $rgExists) {
    Write-Host "? Resource group does not exist. Nothing to delete." -ForegroundColor Green
    exit 0
}

# ============================================================================
# FAST DELETE
# ============================================================================

Write-Host ""
Write-Host "Deleting resource group (this is MUCH FASTER)..." -ForegroundColor Cyan

# Delete without waiting (Azure will delete in background)
az group delete --name $ResourceGroupName --yes --no-wait

Write-Host "? Deletion initiated in background" -ForegroundColor Green
Write-Host ""
Write-Host "Monitoring deletion progress..." -ForegroundColor Cyan

# ============================================================================
# MONITOR DELETION (with timeout)
# ============================================================================

$maxWaitMinutes = 15
$waitedMinutes = 0
$checkInterval = 10  # Check every 10 seconds

while ($waitedMinutes -lt $maxWaitMinutes) {
    Start-Sleep -Seconds $checkInterval
    
    $rgExists = az group exists --name $ResourceGroupName 2>$null | ConvertFrom-Json
    
    if (-not $rgExists) {
        Write-Host ""
        Write-Host "??? Resource group deleted successfully! ???" -ForegroundColor Green
        Write-Host "Time taken: $waitedMinutes minutes" -ForegroundColor Cyan
        exit 0
    }
    
    # Show progress
    $elapsed = [math]::Round($waitedMinutes, 1)
    Write-Host "  [$elapsed/$maxWaitMinutes min] Still deleting..." -ForegroundColor Yellow
    
    $waitedMinutes += ($checkInterval / 60)
}

# ============================================================================
# TIMEOUT
# ============================================================================

Write-Host ""
Write-Host "? Deletion is taking longer than expected ($maxWaitMinutes min)" -ForegroundColor Yellow
Write-Host ""
Write-Host "Options:" -ForegroundColor Cyan
Write-Host "  1. Wait more time - deletion may still be in progress" -ForegroundColor White
Write-Host "  2. Check Azure Portal: https://portal.azure.com" -ForegroundColor White
Write-Host "  3. Force cancel stuck deployments (see below)" -ForegroundColor White
Write-Host ""

# Show resources that might be stuck
Write-Host "Checking for stuck resources..." -ForegroundColor Cyan
$resources = az resource list --resource-group $ResourceGroupName 2>$null | ConvertFrom-Json

if ($resources) {
    Write-Host "Resources still remaining:" -ForegroundColor Yellow
    $resources | ForEach-Object {
        Write-Host "  - $($_.name) ($($_.type))" -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "To force delete stuck resources:" -ForegroundColor Cyan
    Write-Host "  1. Go to Azure Portal" -ForegroundColor White
    Write-Host "  2. Navigate to the resource" -ForegroundColor White
    Write-Host "  3. Delete manually" -ForegroundColor White
}

Write-Host ""
Write-Host "Resource group will continue deleting in background." -ForegroundColor Yellow
Write-Host "Check status: az group show --name $ResourceGroupName" -ForegroundColor Cyan

exit 1
