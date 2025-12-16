# ============================================================================
# Banking System - Azure Teardown Script
# Cleans up all Azure resources for an environment
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
Write-Host "  Banking System Azure Teardown" -ForegroundColor Red
Write-Host "  Environment: $Environment" -ForegroundColor Red
Write-Host "============================================" -ForegroundColor Red
Write-Host ""

# ============================================================================
# CONFIRMATION
# ============================================================================

if (-not $Force) {
    Write-Host "??  WARNING: This will DELETE the following:" -ForegroundColor Yellow
    Write-Host "  - Resource Group: $ResourceGroupName" -ForegroundColor Yellow
    Write-Host "  - All resources inside the resource group" -ForegroundColor Yellow
    Write-Host "  - Container Apps, Databases, Cache, Key Vault, etc." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "?? THIS ACTION CANNOT BE UNDONE!" -ForegroundColor Red
    Write-Host ""
    
    $confirmation = Read-Host "Type 'DELETE' to confirm deletion of $Environment environment"
    
    if ($confirmation -ne 'DELETE') {
        Write-Host "Teardown cancelled" -ForegroundColor Green
        exit 0
    }
}

# ============================================================================
# TEARDOWN
# ============================================================================

try {
    Write-Information "Checking if resource group exists..."
    
    $rgExists = az group exists --name $ResourceGroupName | ConvertFrom-Json
    
    if (-not $rgExists) {
        Write-Information "Resource group $ResourceGroupName does not exist. Nothing to delete."
        exit 0
    }
    
    # List resources that will be deleted
    Write-Information "Resources that will be deleted:"
    az resource list --resource-group $ResourceGroupName --query "[].{Name:name, Type:type}" --output table
    
    Write-Host ""
    Write-Information "Deleting resource group: $ResourceGroupName"
    Write-Information "This may take 5-10 minutes (deletion runs in background)..."
    
    # Delete with --no-wait for faster execution
    az group delete `
        --name $ResourceGroupName `
        --yes `
        --no-wait
    
    Write-Host ""
    Write-Host "? Resource group deletion initiated" -ForegroundColor Green
    Write-Host "  Note: Deletion is running in background" -ForegroundColor Cyan
    
    # Monitor deletion with timeout
    if ($Force) {
        Write-Information "Monitoring deletion progress (max 15 minutes)..."
        
        $maxWait = 15 * 60  # 15 minutes in seconds
        $waited = 0
        $interval = 10  # Check every 10 seconds
        
        while ($waited -lt $maxWait) {
            $rgExists = az group exists --name $ResourceGroupName 2>$null | ConvertFrom-Json
            
            if (-not $rgExists) {
                Write-Host ""
                Write-Host "? Resource group deleted successfully in $([math]::Round($waited/60, 1)) minutes" -ForegroundColor Green
                exit 0
            }
            
            Write-Host "  Still deleting... ($([math]::Round($waited/60, 1))/15 min)" -ForegroundColor Yellow
            Start-Sleep -Seconds $interval
            $waited += $interval
        }
        
        Write-Host ""
        Write-Host "? Deletion timeout after 15 minutes" -ForegroundColor Yellow
        Write-Host "   Resource group may still be deleting in background" -ForegroundColor Cyan
        Write-Host "   Check: az group show --name $ResourceGroupName" -ForegroundColor Cyan
        exit 1
    }
}
catch {
    Write-Error "Teardown failed: $_"
    exit 1
}
