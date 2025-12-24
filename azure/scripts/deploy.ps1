# ============================================================================
# Complete Banking System Deployment
# All-in-one script: Infrastructure + Application + Password Sync
# ============================================================================

#Requires -Version 7.0

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, HelpMessage="Docker Hub username for image repository")]
    [ValidateNotNullOrEmpty()]
    [string]$DockerHubUsername,
    
    [Parameter(Mandatory=$false, HelpMessage="Deployment mode: infrastructure, app, or all")]
    [ValidateSet('infrastructure', 'app', 'all')]
    [string]$DeployMode = "all",
    
    [Parameter(Mandatory=$false, HelpMessage="Environment name (dev, staging, prod)")]
    [ValidatePattern('^[a-z0-9-]+$')]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false, HelpMessage="Docker image tag version")]
    [string]$ImageTag = "1.0.0",
    
    [Parameter(Mandatory=$false, HelpMessage="Azure resource group name")]
    [string]$ResourceGroup = "rg-banking-$Environment",
    
    [Parameter(Mandatory=$false, HelpMessage="Azure region location")]
    [string]$Location = "southeastasia",
    
    [Parameter(Mandatory=$false, HelpMessage="Container App name")]
    [string]$ContainerAppName = "banking-$Environment-api",
    
    [Parameter(Mandatory=$false, HelpMessage="Skip Docker image build")]
    [switch]$SkipImageBuild,
    
    [Parameter(Mandatory=$false, HelpMessage="Use existing password instead of generating new one")]
    [switch]$UseExistingPassword,
    
    [Parameter(Mandatory=$false, HelpMessage="Existing PostgreSQL password")]
    [string]$ExistingPassword = ""
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ============================================================================
# CONSTANTS AND CONFIGURATION
# ============================================================================

$script:Config = @{
    ImageRepository = "banking-api"
    DatabaseName = "BankingSystemDb"
    DatabaseUsername = "bankingadmin"
    PasswordLength = 32
    HealthCheckTimeout = 10
    RestartWaitTime = 30
    ProviderNamespace = "Microsoft.App"
    Tags = @{
        Environment = $Environment
        Project = "BankingSystem"
    }
}

$script:Paths = @{
    ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
    RootPath = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path))
}
$script:Paths.DockerfilePath = Join-Path $script:Paths.RootPath "src\BankingSystem.API\Dockerfile"

# ============================================================================
# HELPER FUNCTIONS
# ============================================================================

function Write-Banner {
    param(
        [string]$Title,
        [ConsoleColor]$Color = 'Cyan',
        [int]$Width = 44
    )
    
    $separator = "=" * $Width
    Write-Host ""
    Write-Host $separator -ForegroundColor $Color
    Write-Host "  $Title" -ForegroundColor $Color
    Write-Host $separator -ForegroundColor $Color
    Write-Host ""
}

function Write-Step {
    param(
        [string]$Message,
        [ConsoleColor]$Color = 'Cyan'
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "??  $Message" -ForegroundColor Yellow
}

function Write-Info {
    param(
        [string]$Label,
        [string]$Value,
        [ConsoleColor]$Color = 'White'
    )
    Write-Host "  ${Label}: " -NoNewline -ForegroundColor Gray
    Write-Host $Value -ForegroundColor $Color
}

function Test-CommandExists {
    param([string]$Command)
    
    $null = Get-Command $Command -ErrorAction SilentlyContinue
    return $?
}

function Invoke-AzCommand {
    param(
        [string]$Description,
        [scriptblock]$Command
    )
    
    Write-Step $Description
    
    try {
        $result = & $Command
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed with exit code: $LASTEXITCODE"
        }
        return $result
    }
    catch {
        Write-Error "Failed to execute: $Description. Error: $($_.Exception.Message)"
        throw
    }
}

function New-SecurePassword {
    param([int]$Length = 32)
    
    $chars = (65..90) + (97..122) + (48..57)
    $password = -join ($chars | Get-Random -Count $Length | ForEach-Object { [char]$_ })
    return $password
}

function Save-DeploymentSecrets {
    param(
        [string]$Password,
        [string]$JwtSecret = "",
        [string]$EncryptionKey = "",
        [string]$Environment
    )
    
    $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $secretsFile = Join-Path $script:Paths.RootPath "deployment-secrets-$timestamp.txt"
    
    $secretsContent = @"
============================================
BANKING SYSTEM DEPLOYMENT SECRETS
============================================
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Environment: $Environment
============================================

PostgreSQL Admin Username: $($script:Config.DatabaseUsername)
PostgreSQL Admin Password: $Password

"@
    
    # Add JWT Secret if provided
    if (-not [string]::IsNullOrEmpty($JwtSecret)) {
        $secretsContent += @"
JWT Secret: $JwtSecret

"@
    }
    
    # Add Encryption Key if provided
    if (-not [string]::IsNullOrEmpty($EncryptionKey)) {
        $secretsContent += @"
Encryption Key: $EncryptionKey

"@
    }
    
    $secretsContent += @"
============================================
??  SAVE THIS FILE SECURELY!
============================================

IMPORTANT NOTES:
- Store this file in a secure password manager (1Password, LastPass, etc.)
- DO NOT commit this file to Git
- If you lose JWT Secret, all existing user tokens will be invalidated
- If you lose Encryption Key, encrypted data (cards, CVV) cannot be decrypted
- Backup this file to at least 2 secure locations

Recommended Actions:
1. Save to password manager immediately
2. Email to your secure email (encrypted)
3. Store encrypted backup in cloud storage
4. Add to Azure Key Vault for production use

============================================
"@
    
    $secretsContent | Out-File -FilePath $secretsFile -Encoding UTF8
    
    Write-Success "Secrets saved to: $secretsFile"
    
    # Copy PostgreSQL password to clipboard (most immediately needed)
    try {
        $Password | Set-Clipboard
        Write-Success "PostgreSQL password copied to clipboard!"
        Write-Host "  (JWT Secret and Encryption Key are in the file)" -ForegroundColor Gray
    }
    catch {
        Write-Warning "Could not copy to clipboard"
    }
    
    return $secretsFile
}

function Get-DatabasePassword {
    param(
        [string]$ExistingPassword,
        [bool]$UseExisting,
        [ref]$JwtSecretOut,
        [ref]$EncryptionKeyOut
    )
    
    if ($UseExisting -and -not [string]::IsNullOrEmpty($ExistingPassword)) {
        Write-Step "Using existing password provided" -Color Cyan
        Write-Warning "JWT Secret and Encryption Key must be provided separately when using existing password"
        return $ExistingPassword
    }
    
    # Generate all secrets
    $password = New-SecurePassword -Length $script:Config.PasswordLength
    $jwtSecret = New-SecurePassword -Length 64  # JWT needs longer secret
    $encryptionKey = New-SecurePassword -Length 32
    
    Write-Host "Generated Secrets:" -ForegroundColor Yellow
    Write-Info "PostgreSQL Password" "$($password.Substring(0, 8))..." -Color Yellow
    Write-Info "JWT Secret" "$($jwtSecret.Substring(0, 8))..." -Color Yellow
    Write-Info "Encryption Key" "$($encryptionKey.Substring(0, 8))..." -Color Yellow
    Write-Host "  (Full values saved to file)" -ForegroundColor Gray
    Write-Host ""
    
    # Save all secrets to file
    $secretsFile = Save-DeploymentSecrets `
        -Password $password `
        -JwtSecret $jwtSecret `
        -EncryptionKey $encryptionKey `
        -Environment $Environment
    
    Write-Host ""
    Write-Host "?? CRITICAL: Secrets File Location" -ForegroundColor Magenta
    Write-Host "  $secretsFile" -ForegroundColor White
    Write-Host ""
    Write-Host "??  You MUST save these secrets securely!" -ForegroundColor Red
    Write-Host "  - Add to password manager NOW" -ForegroundColor Yellow
    Write-Host "  - Backup to secure cloud storage" -ForegroundColor Yellow
    Write-Host "  - Email encrypted copy to yourself" -ForegroundColor Yellow
    Write-Host ""
    
    $confirmation = Read-Host "Have you saved ALL secrets securely? Type 'yes' to continue"
    if ($confirmation -ne "yes") {
        throw "Please save all secrets before continuing. Check file: $secretsFile"
    }
    
    # Return JWT and Encryption Key via reference parameters
    $JwtSecretOut.Value = $jwtSecret
    $EncryptionKeyOut.Value = $encryptionKey
    
    return $password
}

function Save-PostgreSqlPassword {
    param(
        [string]$ContainerAppName,
        [string]$ResourceGroup,
        [string]$Password
    )
    
    Write-Step "Saving PostgreSQL password to Container App..." -Color Cyan
    
    Invoke-AzCommand "Updating Container App secret" {
        az containerapp secret set `
            --name $ContainerAppName `
            --resource-group $ResourceGroup `
            --secrets `
                "pgadmin-password=$Password" `
                "pgadmin-email=admin@example.com" `
            --output none
    }
    
    Write-Success "PostgreSQL password saved to Container App"
}

function Deploy-Infrastructure {
    param(
        [string]$Environment,
        [string]$ResourceGroup,
        [string]$Location
    )
    
    Write-Banner "STEP 1: Deploy Infrastructure" -Color Yellow
    
    $deployScript = Join-Path $script:Paths.ScriptPath "deploy.ps1"
    
    if (Test-Path $deployScript) {
        Write-Step "Deploying infrastructure using deploy.ps1..." -Color Yellow
        & $deployScript -Environment $Environment -SkipImageBuild
    }
    else {
        Write-Warning "deploy.ps1 not found, using manual deployment"
        
        $rgExists = az group exists --name $ResourceGroup | ConvertFrom-Json
        if (-not $rgExists) {
            Invoke-AzCommand "Creating resource group: $ResourceGroup" {
                az group create `
                    --name $ResourceGroup `
                    --location $Location `
                    --tags Environment=$Environment Project=BankingSystem `
                    --output none
            }
        }
        
        Write-Success "Resource group ready"
    }
    
    Write-Host ""
    Write-Success "Infrastructure deployment complete!"
    Write-Host ""
}

function Build-DockerImage {
    param(
        [string]$ImageName,
        [string]$ImageTag
    )
    
    Write-Step "Building Docker image..." -Color Cyan
    
    docker build `
        -t $ImageName `
        -f $script:Paths.DockerfilePath `
        --build-arg BUILD_VERSION=$ImageTag `
        $script:Paths.RootPath
    
    if ($LASTEXITCODE -ne 0) {
        throw "Docker build failed!"
    }
    
    Write-Success "Image built: $ImageName"
}

function Publish-DockerImage {
    param(
        [string]$DockerHubUsername,
        [string]$ImageTag
    )
    
    $imageName = "$DockerHubUsername/$($script:Config.ImageRepository):$ImageTag"
    $latestImage = "$DockerHubUsername/$($script:Config.ImageRepository):latest"
    
    Build-DockerImage -ImageName $imageName -ImageTag $ImageTag
    
    Write-Host ""
    Write-Step "Tagging as latest..." -Color Cyan
    docker tag $imageName $latestImage
    
    Write-Host ""
    Write-Step "Login to Docker Hub..." -Color Cyan
    docker login
    
    if ($LASTEXITCODE -ne 0) {
        throw "Docker login failed!"
    }
    
    Write-Host ""
    Write-Step "Pushing to Docker Hub..." -Color Cyan
    docker push $imageName
    docker push $latestImage
    
    if ($LASTEXITCODE -ne 0) {
        throw "Docker push failed!"
    }
    
    Write-Success "Image pushed to Docker Hub"
    
    return $imageName
}

function Update-ContainerAppImage {
    param(
        [string]$ContainerAppName,
        [string]$ResourceGroup,
        [string]$ImageName
    )
    
    Write-Step "Updating Container App with new image..." -Color Cyan
    
    # Ensure Microsoft.App provider is registered
    $providerState = az provider show `
        --namespace $script:Config.ProviderNamespace `
        --query "registrationState" `
        -o tsv
    
    if ($providerState -ne "Registered") {
        Write-Step "Registering $($script:Config.ProviderNamespace) provider..." -Color Yellow
        az provider register --namespace $script:Config.ProviderNamespace --wait
    }
    
    Invoke-AzCommand "Updating Container App image" {
        az containerapp update `
            --name $ContainerAppName `
            --resource-group $ResourceGroup `
            --image $ImageName `
            --output none
    }
    
    Write-Success "Container App updated with new image"
}

function Deploy-Application {
    param(
        [string]$DockerHubUsername,
        [string]$ImageTag,
        [string]$ContainerAppName,
        [string]$ResourceGroup,
        [bool]$SkipBuild
    )
    
    Write-Banner "STEP 2: Build & Deploy Application" -Color Yellow
    
    if ($SkipBuild) {
        Write-Warning "Skipping image build"
        return
    }
    
    $imageName = Publish-DockerImage -DockerHubUsername $DockerHubUsername -ImageTag $ImageTag
    
    Write-Host ""
    Update-ContainerAppImage `
        -ContainerAppName $ContainerAppName `
        -ResourceGroup $ResourceGroup `
        -ImageName $imageName
    
    Write-Host ""
}

function Get-PostgreSQLServer {
    param(
        [string]$ResourceGroup
    )
    
    Write-Step "Detecting PostgreSQL server..." -Color Cyan
    
    $servers = az postgres flexible-server list `
        --resource-group $ResourceGroup `
        --query "[].name" `
        -o tsv
    
    if ([string]::IsNullOrEmpty($servers)) {
        return $null
    }
    
    $serverArray = $servers -split "`n"
    $serverName = $serverArray[0].Trim()
    
    Write-Success "Found server: $serverName"
    
    return $serverName
}

function Get-ServerFQDN {
    param(
        [string]$ServerName,
        [string]$ResourceGroup
    )
    
    $fqdn = az postgres flexible-server show `
        --resource-group $ResourceGroup `
        --name $ServerName `
        --query "fullyQualifiedDomainName" `
        -o tsv
    
    return $fqdn
}

function New-ConnectionString {
    param(
        [string]$ServerFQDN,
        [string]$Database,
        [string]$Username,
        [string]$Password
    )
    
    return "Host=$ServerFQDN;Database=$Database;Username=$Username;Password=$Password;SSL Mode=Require;Trust Server Certificate=true"
}

function Sync-DatabasePassword {
    param(
        [string]$Password,
        [string]$ContainerAppName,
        [string]$ResourceGroup
    )
    
    Write-Banner "STEP 3: Sync Database Password" -Color Yellow
    
    $serverName = Get-PostgreSQLServer -ResourceGroup $ResourceGroup
    
    if ([string]::IsNullOrEmpty($serverName)) {
        Write-Warning "No PostgreSQL server found. Skipping password sync."
        return
    }
    
    $serverFqdn = Get-ServerFQDN -ServerName $serverName -ResourceGroup $ResourceGroup
    
    $connString = New-ConnectionString `
        -ServerFQDN $serverFqdn `
        -Database $script:Config.DatabaseName `
        -Username $script:Config.DatabaseUsername `
        -Password $Password
    
    Write-Step "Updating Container App environment variables..." -Color Cyan
    
    Invoke-AzCommand "Syncing database password" {
        az containerapp update `
            --name $ContainerAppName `
            --resource-group $ResourceGroup `
            --set-env-vars `
                "ConnectionStrings__BusinessDatabase=$connString" `
                "ConnectionStrings__HangfireDatabase=$connString" `
            --output none
    }
    
    Write-Success "Password synced to Container App"
    Write-Host ""
}

function Test-Deployment {
    param(
        [string]$ContainerAppName,
        [string]$ResourceGroup
    )
    
    Write-Banner "STEP 4: Verify Deployment" -Color Yellow
    
    Write-Step "Waiting for Container App to restart ($($script:Config.RestartWaitTime) seconds)..." -Color Cyan
    Start-Sleep -Seconds $script:Config.RestartWaitTime
    
    $apiUrl = az containerapp show `
        --name $ContainerAppName `
        --resource-group $ResourceGroup `
        --query "properties.configuration.ingress.fqdn" `
        -o tsv
    
    $fullUrl = "https://$apiUrl"
    
    Write-Host ""
    Write-Step "Testing health endpoint..." -Color Cyan
    
    try {
        $health = Invoke-RestMethod "$fullUrl/health" -TimeoutSec $script:Config.HealthCheckTimeout
        
        Write-Host ""
        Write-Banner "? DEPLOYMENT SUCCESSFUL!" -Color Green
        
        Write-Host "API URL: $fullUrl" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Health Check Result:" -ForegroundColor Yellow
        $health | ConvertTo-Json
        Write-Host ""
        Write-Host "Endpoints:" -ForegroundColor Yellow
        Write-Info "Health" "$fullUrl/health"
        Write-Info "Swagger" "$fullUrl/swagger"
        Write-Info "Register" "$fullUrl/api/auth/register"
        Write-Host ""
    }
    catch {
        Write-Host ""
        Write-Warning "Health check failed: $($_.Exception.Message)"
        Write-Host ""
        Write-Host "Check logs:" -ForegroundColor Cyan
        Write-Info "Command" "az containerapp logs show --name $ContainerAppName --resource-group $ResourceGroup --follow"
        Write-Host ""
        Write-Host "Try health check again:" -ForegroundColor Cyan
        Write-Info "Command" "Invoke-RestMethod '$fullUrl/health'"
        Write-Host ""
    }
}

# ============================================================================
# MAIN EXECUTION
# ============================================================================

function Main {
    try {
        # Display deployment configuration
        Write-Banner "?? Banking System Complete Deployment" -Color Cyan
        
        Write-Info "Environment" $Environment
        Write-Info "Mode" $DeployMode
        Write-Info "Docker Hub" $DockerHubUsername
        Write-Info "Image Tag" $ImageTag
        Write-Host ""
        
        # Initialize secret variables
        $postgresPassword = ""
        $jwtSecret = ""
        $encryptionKey = ""
        
        # Step 1: Deploy Infrastructure
        if ($DeployMode -in @('infrastructure', 'all')) {
            # Generate or get secrets
            $jwtSecretRef = [ref]""]
            $encryptionKeyRef = [ref]""]
            
            $postgresPassword = Get-DatabasePassword `
                -ExistingPassword $ExistingPassword `
                -UseExisting $UseExistingPassword `
                -JwtSecretOut $jwtSecretRef `
                -EncryptionKeyOut $encryptionKeyRef
            
            $jwtSecret = $jwtSecretRef.Value
            $encryptionKey = $encryptionKeyRef.Value
            
            Write-Host ""
            Write-Info "Secrets generated" "? All secrets ready for deployment" -Color Green
            Write-Host ""
            
            Deploy-Infrastructure `
                -Environment $Environment `
                -ResourceGroup $ResourceGroup `
                -Location $Location
        }
        
        # Step 2: Build and Deploy Application
        if ($DeployMode -in @('app', 'all')) {
            Deploy-Application `
                -DockerHubUsername $DockerHubUsername `
                -ImageTag $ImageTag `
                -ContainerAppName $ContainerAppName `
                -ResourceGroup $ResourceGroup `
                -SkipBuild $SkipImageBuild
            
            # Step 3: Sync Database Password
            if ([string]::IsNullOrEmpty($postgresPassword)) {
                if ($UseExistingPassword -and -not [string]::IsNullOrEmpty($ExistingPassword)) {
                    $postgresPassword = $ExistingPassword
                }
                else {
                    $postgresPassword = Get-PasswordInteractive
                }
            }
            
            Sync-DatabasePassword `
                -Password $postgresPassword `
                -ContainerAppName $ContainerAppName `
                -ResourceGroup $ResourceGroup
        }
        
        # Step 4: Verify Deployment
        Test-Deployment `
            -ContainerAppName $ContainerAppName `
            -ResourceGroup $ResourceGroup
        
        Write-Host ""
        Write-Success "?? Deployment process complete!"
        Write-Host ""
        
        # Final reminder about secrets
        if (-not [string]::IsNullOrEmpty($jwtSecret)) {
            Write-Host "?? REMINDER: Secrets File Location" -ForegroundColor Cyan
            Write-Host "  Check your workspace root for: deployment-secrets-*.txt" -ForegroundColor White
            Write-Host "  This file contains ALL critical secrets (DB, JWT, Encryption)" -ForegroundColor White
            Write-Host ""
        }
    }
    catch {
        Write-Host ""
        Write-Host "? Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
        exit 1
    }
}

# Execute main function
Main
