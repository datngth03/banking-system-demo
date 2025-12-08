# Complete GitHub Secrets Setup for Staging
# Run this script to setup all required secrets

param(
    [switch]$ProductionAlso
)

Write-Host "`n?? GITHUB SECRETS SETUP - STAGING`n" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Check prerequisites
Write-Host "`n1?? Checking prerequisites..." -ForegroundColor Yellow

# Check gh CLI
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "? GitHub CLI not installed!" -ForegroundColor Red
    Write-Host "Install from: https://cli.github.com/" -ForegroundColor Yellow
    exit 1
}

# Check authentication
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Not logged in to GitHub CLI!" -ForegroundColor Red
    Write-Host "Run: gh auth login" -ForegroundColor Yellow
    exit 1
}

Write-Host "? GitHub CLI authenticated" -ForegroundColor Green

# Generate auto-generated secrets
Write-Host "`n2?? Generating auto-generated secrets..." -ForegroundColor Yellow

# Use Invoke-Expression to properly capture output
$jwtSecret = (Invoke-Expression "openssl rand -base64 32").Trim()
if ([string]::IsNullOrWhiteSpace($jwtSecret)) {
    Write-Host "? Failed to generate JWT secret!" -ForegroundColor Red
    exit 1
}
Write-Host "? JWT Secret generated ($($jwtSecret.Length) chars)" -ForegroundColor Green

$encKey = (Invoke-Expression "openssl rand -base64 32").Trim()
if ([string]::IsNullOrWhiteSpace($encKey)) {
    Write-Host "? Failed to generate Encryption Key!" -ForegroundColor Red
    exit 1
}
Write-Host "? Encryption Key generated ($($encKey.Length) chars)" -ForegroundColor Green

$encIv = (Invoke-Expression "openssl rand -base64 16").Trim()
if ([string]::IsNullOrWhiteSpace($encIv)) {
    Write-Host "? Failed to generate Encryption IV!" -ForegroundColor Red
    exit 1
}
Write-Host "? Encryption IV generated ($($encIv.Length) chars)" -ForegroundColor Green

# Display generated values
Write-Host "`n?? Generated Values (save these!):" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "JWT_SECRET_STAGING:" -ForegroundColor Yellow
Write-Host $jwtSecret -ForegroundColor White
Write-Host "`nENCRYPTION_KEY_STAGING:" -ForegroundColor Yellow
Write-Host $encKey -ForegroundColor White
Write-Host "`nENCRYPTION_IV_STAGING:" -ForegroundColor Yellow
Write-Host $encIv -ForegroundColor White
Write-Host "=================================" -ForegroundColor Cyan

# Save to file
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$secretsFile = "secrets_staging_$timestamp.txt"

@"
# STAGING SECRETS - Generated $timestamp
# ?? DELETE THIS FILE AFTER USE!

JWT_SECRET_STAGING=$jwtSecret
ENCRYPTION_KEY_STAGING=$encKey
ENCRYPTION_IV_STAGING=$encIv

# Manual secrets (fill these in):
DB_CONNECTION_STAGING=
HANGFIRE_CONNECTION_STAGING=
REDIS_CONNECTION_STAGING=
SEQ_API_KEY_STAGING=
APP_INSIGHTS_CONNECTION_STRING_STAGING=
EMAIL_USERNAME=
EMAIL_PASSWORD=
"@ | Out-File -FilePath $secretsFile -Encoding UTF8

Write-Host "`n?? Saved to: $secretsFile" -ForegroundColor Green
Write-Host "??  DELETE this file after setting secrets!" -ForegroundColor Red

# Set auto-generated secrets
Write-Host "`n3?? Setting auto-generated secrets in GitHub..." -ForegroundColor Yellow

echo $jwtSecret | gh secret set JWT_SECRET_STAGING 2>&1 | Out-Null
Write-Host "? JWT_SECRET_STAGING" -ForegroundColor Green

echo $encKey | gh secret set ENCRYPTION_KEY_STAGING 2>&1 | Out-Null
Write-Host "? ENCRYPTION_KEY_STAGING" -ForegroundColor Green

echo $encIv | gh secret set ENCRYPTION_IV_STAGING 2>&1 | Out-Null
Write-Host "? ENCRYPTION_IV_STAGING" -ForegroundColor Green

# Manual secrets
Write-Host "`n4?? Setting manual secrets..." -ForegroundColor Yellow
Write-Host "??  For each secret, paste the value and press Ctrl+D (Windows: Ctrl+Z + Enter)" -ForegroundColor Cyan
Write-Host ""

$manualSecrets = @(
    @{
        Name = "DB_CONNECTION_STAGING"
        Description = "PostgreSQL connection string"
        Example = "Host=staging-db.postgres.database.azure.com;Database=BankingStaging;Username=admin;Password=xxx;SSL Mode=Require"
    },
    @{
        Name = "HANGFIRE_CONNECTION_STAGING"
        Description = "Hangfire PostgreSQL connection"
        Example = "Host=staging-db.postgres.database.azure.com;Database=HangfireStaging;Username=admin;Password=xxx;SSL Mode=Require"
    },
    @{
        Name = "REDIS_CONNECTION_STAGING"
        Description = "Redis connection string"
        Example = "staging-redis.redis.cache.windows.net:6380,ssl=true,password=xxx"
    },
    @{
        Name = "SEQ_API_KEY_STAGING"
        Description = "Seq API key (optional)"
        Example = "Get from Seq Dashboard ? Settings ? API Keys"
    },
    @{
        Name = "APP_INSIGHTS_CONNECTION_STRING_STAGING"
        Description = "Application Insights connection"
        Example = "InstrumentationKey=xxx;IngestionEndpoint=https://..."
    },
    @{
        Name = "EMAIL_USERNAME"
        Description = "SendGrid username or API key name"
        Example = "apikey or your-username"
    },
    @{
        Name = "EMAIL_PASSWORD"
        Description = "SendGrid API key or password"
        Example = "SG.xxxxxxxxxxxxx"
    }
)

foreach ($secret in $manualSecrets) {
    Write-Host "`n?????????????????????????????????" -ForegroundColor Cyan
    Write-Host "Secret: $($secret.Name)" -ForegroundColor Yellow
    Write-Host "Purpose: $($secret.Description)" -ForegroundColor Gray
    Write-Host "Example: $($secret.Example)" -ForegroundColor Gray
    Write-Host "?????????????????????????????????" -ForegroundColor Cyan
    
    $skip = Read-Host "Skip this secret? (y/N)"
    if ($skip -ne "y") {
        gh secret set $secret.Name
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? $($secret.Name) set" -ForegroundColor Green
        } else {
            Write-Host "??  $($secret.Name) - may have been skipped or failed" -ForegroundColor Yellow
        }
    } else {
        Write-Host "??  Skipped $($secret.Name)" -ForegroundColor Yellow
    }
}

# Verify secrets
Write-Host "`n5?? Verifying secrets..." -ForegroundColor Yellow
$secretsList = gh secret list 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n?? Current GitHub Secrets:" -ForegroundColor Cyan
    $secretsList | Select-String "STAGING"
} else {
    Write-Host "??  Could not list secrets" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=================================" -ForegroundColor Cyan
Write-Host "? SECRETS SETUP COMPLETE!" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Cyan

Write-Host "`n?? Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Delete secrets file: $secretsFile" -ForegroundColor White
Write-Host "  2. Trigger deployment:" -ForegroundColor White
Write-Host "     git commit --allow-empty -m 'chore: trigger staging deployment'" -ForegroundColor Gray
Write-Host "     git push origin main" -ForegroundColor Gray
Write-Host "  3. Monitor deployment:" -ForegroundColor White
Write-Host "     https://github.com/datngth03/banking-system-demo/actions" -ForegroundColor Gray

Write-Host "`n??  IMPORTANT:" -ForegroundColor Red
Write-Host "  - Delete $secretsFile immediately!" -ForegroundColor White
Write-Host "  - Never commit secrets to Git!" -ForegroundColor White
Write-Host "  - Keep backup in secure location (Azure Key Vault, 1Password)" -ForegroundColor White

Write-Host ""
