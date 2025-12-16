# ============================================================================
# Banking System - Azure Deployment Script
# Deploys complete infrastructure to Azure using Bicep templates
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('dev', 'staging', 'prod')]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$ResourceGroupName = "rg-banking-$Environment",
    
    [Parameter(Mandatory=$false)]
    [string]$Location = "southeastasia",
    
    [Parameter(Mandatory=$false)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipInfrastructure,
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipImageBuild,
    
    [Parameter(Mandatory=$false)]
    [string]$ImageTag = "latest"
)

# ============================================================================
# CONFIGURATION
# ============================================================================

$ErrorActionPreference = "Stop"
$InformationPreference = "Continue"

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent (Split-Path -Parent $scriptPath)
$bicepPath = Join-Path $scriptPath "..\bicep"
$parametersFile = Join-Path $bicepPath "parameters\$Environment.parameters.json"

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Banking System Azure Deployment" -ForegroundColor Cyan
Write-Host "  Environment: $Environment" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ============================================================================
# FUNCTIONS
# ============================================================================

function Test-AzureCLI {
    Write-Information "Checking Azure CLI installation..."
    
    try {
        $azVersion = az version | ConvertFrom-Json
        Write-Information "? Azure CLI version: $($azVersion.'azure-cli')"
    }
    catch {
        Write-Error "Azure CLI is not installed. Please install from: https://aka.ms/installazurecli"
        exit 1
    }
}

function Connect-AzureSubscription {
    Write-Information "Connecting to Azure..."
    
    # Check if already logged in
    $account = az account show 2>$null | ConvertFrom-Json
    
    if (-not $account) {
        Write-Information "Not logged in. Initiating login..."
        az login
    }
    else {
        Write-Information "? Already logged in as: $($account.user.name)"
    }
    
    # Set subscription if specified
    if ($SubscriptionId) {
        Write-Information "Setting subscription to: $SubscriptionId"
        az account set --subscription $SubscriptionId
    }
    
    $currentSubscription = az account show | ConvertFrom-Json
    Write-Information "? Using subscription: $($currentSubscription.name) ($($currentSubscription.id))"
    
    return $currentSubscription.id
}

function New-ResourceGroupIfNotExists {
    param(
        [string]$Name,
        [string]$Location
    )
    
    Write-Information "Checking resource group: $Name"
    
    $rgExists = az group exists --name $Name | ConvertFrom-Json
    
    if (-not $rgExists) {
        Write-Information "Creating resource group: $Name in $Location"
        az group create `
            --name $Name `
            --location $Location `
            --tags Environment=$Environment Project=BankingSystem ManagedBy=Bicep
        
        Write-Information "? Resource group created"
    }
    else {
        Write-Information "? Resource group already exists"
    }
}

function New-DeploymentSecrets {
    Write-Information "Generating deployment secrets..."
    
    # Generate secrets if not already set
    $secrets = @{
        postgresAdminPassword = -join ((65..90) + (97..122) + (48..57) + 33,35,36,37,38,42 | Get-Random -Count 24 | ForEach-Object {[char]$_})
        jwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
        encryptionKey = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
    }
    
    Write-Host ""
    Write-Host "?? GENERATED SECRETS (SAVE THESE SECURELY!):" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host "Postgres Admin Password: $($secrets.postgresAdminPassword)" -ForegroundColor Yellow
    Write-Host "JWT Secret: $($secrets.jwtSecret)" -ForegroundColor Yellow
    Write-Host "Encryption Key: $($secrets.encryptionKey)" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Yellow
    Write-Host ""
    
    $confirmation = Read-Host "Have you saved these secrets? (yes/no)"
    if ($confirmation -ne "yes") {
        Write-Error "Please save the secrets before continuing"
        exit 1
    }
    
    return $secrets
}

function Deploy-Infrastructure {
    param(
        [string]$ResourceGroupName,
        [hashtable]$Secrets
    )
    
    Write-Information "Deploying infrastructure using Bicep..."
    
    $deploymentName = "banking-infra-$(Get-Date -Format 'yyyyMMddHHmmss')"
    $mainBicep = Join-Path $bicepPath "main.bicep"
    
    Write-Information "Starting deployment: $deploymentName"
    Write-Information "This may take 10-15 minutes..."
    
    # Deploy with proper parameter format
    az deployment group create `
        --name $deploymentName `
        --resource-group $ResourceGroupName `
        --template-file $mainBicep `
        --parameters environment=$Environment `
        --parameters location=$Location `
        --parameters baseName=banking `
        --parameters postgresAdminUsername=bankingadmin `
        --parameters "postgresAdminPassword=$($Secrets.postgresAdminPassword)" `
        --parameters "jwtSecret=$($Secrets.jwtSecret)" `
        --parameters "encryptionKey=$($Secrets.encryptionKey)" `
        --parameters apiImageTag=$ImageTag `
        --parameters minReplicas=0 `
        --parameters maxReplicas=3 `
        --parameters enableAppInsights=false `
        --verbose
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Infrastructure deployment failed"
        exit 1
    }
    
    Write-Information "? Infrastructure deployed successfully"
    
    # Get deployment outputs
    $outputs = az deployment group show `
        --name $deploymentName `
        --resource-group $ResourceGroupName `
        --query properties.outputs `
        | ConvertFrom-Json
    
    return $outputs
}

function Build-AndPushImage {
    param(
        [string]$RegistryLoginServer,
        [string]$RegistryUsername,
        [string]$RegistryPassword,
        [string]$ImageTag
    )
    
    Write-Information "Building and pushing Docker image..."
    
    $imageName = "$RegistryLoginServer/banking-api:$ImageTag"
    
    # Login to ACR
    Write-Information "Logging in to Azure Container Registry..."
    az acr login --name ($RegistryLoginServer -split '\.')[0]
    
    # Build using ACR Build (faster)
    Write-Information "Building image in Azure (this may take 5-10 minutes)..."
    
    az acr build `
        --registry ($RegistryLoginServer -split '\.')[0] `
        --image "banking-api:$ImageTag" `
        --image "banking-api:latest" `
        --file "$rootPath\src\BankingSystem.API\Dockerfile" `
        --platform linux `
        $rootPath
    
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Image build failed"
        exit 1
    }
    
    Write-Information "? Image built and pushed successfully"
}

function Show-DeploymentSummary {
    param(
        [object]$Outputs
    )
    
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Green
    Write-Host "  DEPLOYMENT COMPLETED SUCCESSFULLY! ??" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "API URL:" -ForegroundColor Cyan
    Write-Host "  $($Outputs.apiUrl.value)" -ForegroundColor White
    Write-Host ""
    Write-Host "??  NOTE: Using public Microsoft sample image" -ForegroundColor Yellow
    Write-Host "    To deploy your app, create Container Registry manually" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "PostgreSQL Servers:" -ForegroundColor Cyan
    Write-Host "  Business: $($Outputs.postgresBusinessFqdn.value)" -ForegroundColor White
    Write-Host "  Hangfire: $($Outputs.postgresHangfireFqdn.value)" -ForegroundColor White
    Write-Host ""
    Write-Host "Redis Cache:" -ForegroundColor Cyan
    Write-Host "  $($Outputs.redisHostName.value)" -ForegroundColor White
    Write-Host ""
    Write-Host "Key Vault:" -ForegroundColor Cyan
    Write-Host "  $($Outputs.keyVaultUri.value)" -ForegroundColor White
    Write-Host ""
    
    if ($Outputs.appInsightsConnectionString) {
        Write-Host "Application Insights:" -ForegroundColor Cyan
        Write-Host "  Configured ?" -ForegroundColor White
        Write-Host ""
    }
    
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Test health endpoint: $($Outputs.apiUrl.value)/health" -ForegroundColor White
    Write-Host "  2. Access sample app: $($Outputs.apiUrl.value)" -ForegroundColor White
    Write-Host "  3. To deploy your Banking app:" -ForegroundColor White
    Write-Host "     - Create Container Registry manually in Azure Portal" -ForegroundColor White
    Write-Host "     - Build and push your Docker image" -ForegroundColor White
    Write-Host "     - Update Container App with your image" -ForegroundColor White
    Write-Host "  4. Check logs in Azure Portal -> Container Apps" -ForegroundColor White
    Write-Host ""
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

try {
    # Step 1: Validate prerequisites
    Test-AzureCLI
    
    # Step 2: Connect to Azure
    $subscriptionId = Connect-AzureSubscription
    
    # Step 3: Create resource group
    New-ResourceGroupIfNotExists -Name $ResourceGroupName -Location $Location
    
    # Step 4: Generate secrets
    $secrets = New-DeploymentSecrets
    
    # Step 5: Deploy infrastructure (unless skipped)
    if (-not $SkipInfrastructure) {
        $outputs = Deploy-Infrastructure -ResourceGroupName $ResourceGroupName -Secrets $secrets
    }
    else {
        Write-Information "??  Skipping infrastructure deployment"
        
        # Get existing deployment outputs
        $latestDeployment = az deployment group list `
            --resource-group $ResourceGroupName `
            --query "[?starts_with(name, 'banking-infra')] | sort_by(@, &properties.timestamp) | [-1]" `
            | ConvertFrom-Json
        
        $outputs = az deployment group show `
            --name $latestDeployment.name `
            --resource-group $ResourceGroupName `
            --query properties.outputs `
            | ConvertFrom-Json
    }
    
    # Step 6: Build and push image (unless skipped)
    if (-not $SkipImageBuild) {
        Build-AndPushImage `
            -RegistryLoginServer $outputs.containerRegistryLoginServer.value `
            -RegistryUsername $outputs.containerRegistryAdminUsername.value `
            -RegistryPassword "dummy" `
            -ImageTag $ImageTag
    }
    else {
        Write-Information "??  Skipping image build"
    }
    
    # Step 7: Show summary
    Show-DeploymentSummary -Outputs $outputs
    
    Write-Host "? Deployment completed successfully!" -ForegroundColor Green
    exit 0
}
catch {
    Write-Error "Deployment failed: $_"
    Write-Error $_.ScriptStackTrace
    exit 1
}
