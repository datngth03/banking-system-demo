# Generate Secrets for Banking System
# This script generates secure random secrets for JWT and Encryption

Write-Host "?? Generating Secrets for Banking System..." -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Function to generate random base64 string
function Generate-Secret {
    param([int]$Length = 32)
    
    $bytes = New-Object byte[] $Length
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $rng.GetBytes($bytes)
    $rng.Dispose()
    
    return [Convert]::ToBase64String($bytes)
}

# Generate secrets
Write-Host "Generating JWT Secret..." -ForegroundColor Yellow
$jwtSecret = Generate-Secret -Length 32
Write-Host "? JWT Secret generated" -ForegroundColor Green
Write-Host ""

Write-Host "Generating Encryption Key..." -ForegroundColor Yellow
$encryptionKey = Generate-Secret -Length 32
Write-Host "? Encryption Key generated" -ForegroundColor Green
Write-Host ""

# Display generated secrets
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "?? Generated Secrets:" -ForegroundColor Green
Write-Host ""
Write-Host "JWT_SECRET:" -ForegroundColor Yellow
Write-Host $jwtSecret -ForegroundColor White
Write-Host ""
Write-Host "ENCRYPTION_KEY:" -ForegroundColor Yellow
Write-Host $encryptionKey -ForegroundColor White
Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if .env exists
if (Test-Path ".env") {
    Write-Host "Found .env file" -ForegroundColor Cyan
    
    $update = Read-Host "Update .env file with these secrets? (y/n)"
    
    if ($update -eq "y") {
        Write-Host ""
        Write-Host "Updating .env file..." -ForegroundColor Yellow
        
        # Read current .env
        $envContent = Get-Content ".env" -Raw
        
        # Replace JWT_SECRET
        $envContent = $envContent -replace 'JwtSettings__Secret=.*', "JwtSettings__Secret=$jwtSecret"
        
        # Replace ENCRYPTION_KEY
        $envContent = $envContent -replace 'EncryptionSettings__Key=.*', "EncryptionSettings__Key=$encryptionKey"
        
        # Save updated .env
        $envContent | Set-Content ".env" -NoNewline
        
        Write-Host "? .env file updated successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "?? Updated values:" -ForegroundColor Cyan
        Write-Host "  - JwtSettings__Secret" -ForegroundColor White
        Write-Host "  - EncryptionSettings__Key" -ForegroundColor White
        Write-Host ""
        Write-Host "?? Keep these secrets secure!" -ForegroundColor Yellow
        Write-Host "   - Never commit to version control" -ForegroundColor White
        Write-Host "   - Use different secrets for production" -ForegroundColor White
        Write-Host "   - Rotate secrets every 90 days" -ForegroundColor White
        Write-Host ""
        Write-Host "? You can now run: .\start-dev.ps1" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "?? Secrets not saved to .env" -ForegroundColor Yellow
        Write-Host "   Copy the secrets above manually if needed" -ForegroundColor White
    }
} else {
    Write-Host "?? .env file not found!" -ForegroundColor Yellow
    Write-Host ""
    
    $create = Read-Host "Create .env file from .env.example? (y/n)"
    
    if ($create -eq "y") {
        if (Test-Path ".env.example") {
            Write-Host ""
            Write-Host "Creating .env from .env.example..." -ForegroundColor Yellow
            Copy-Item ".env.example" ".env"
            
            # Read and update
            $envContent = Get-Content ".env" -Raw
            $envContent = $envContent -replace 'JwtSettings__Secret=.*', "JwtSettings__Secret=$jwtSecret"
            $envContent = $envContent -replace 'EncryptionSettings__Key=.*', "EncryptionSettings__Key=$encryptionKey"
            $envContent | Set-Content ".env" -NoNewline
            
            Write-Host "? .env file created and configured!" -ForegroundColor Green
            Write-Host ""
            Write-Host "? You can now run: .\start-dev.ps1" -ForegroundColor Green
        } else {
            Write-Host "? .env.example not found!" -ForegroundColor Red
            Write-Host "   Please create .env manually and copy the secrets above" -ForegroundColor Yellow
        }
    } else {
        Write-Host ""
        Write-Host "Please copy the secrets above to your .env file manually" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Option to save to file
$save = Read-Host "Save secrets to a temporary file for backup? (y/n)"

if ($save -eq "y") {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $filename = "secrets_$timestamp.txt"
    
    @"
# Banking System Secrets - Generated $timestamp
# ?? DELETE THIS FILE AFTER COPYING SECRETS!
# DO NOT COMMIT TO VERSION CONTROL!

JWT_SECRET=$jwtSecret

ENCRYPTION_KEY=$encryptionKey

# Instructions:
# 1. Copy these values to your .env file
# 2. DELETE this file immediately
# 3. Never share these secrets
"@ | Out-File -FilePath $filename -Encoding utf8
    
    Write-Host ""
    Write-Host "? Secrets saved to: $filename" -ForegroundColor Green
    Write-Host ""
    Write-Host "??  IMPORTANT:" -ForegroundColor Red
    Write-Host "   - Copy secrets to .env file" -ForegroundColor Yellow
    Write-Host "   - DELETE $filename immediately!" -ForegroundColor Yellow
    Write-Host "   - Never commit this file to git!" -ForegroundColor Yellow
    Write-Host ""
}

Write-Host "?? Done!" -ForegroundColor Green
Write-Host ""
