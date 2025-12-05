# Environment Configuration Verification Script
# Validates monitoring configuration for all environments

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Development", "Docker", "Test", "Staging", "Production", "All")]
    [string]$Environment = "All"
)

$ErrorActionPreference = "Stop"

function Test-JsonFile {
    param($FilePath)
    
    if (Test-Path $FilePath) {
        try {
            $null = Get-Content $FilePath | ConvertFrom-Json
            return $true
        } catch {
            Write-Host "   ? Invalid JSON: $_" -ForegroundColor Red
            return $false
        }
    } else {
        Write-Host "   ? File not found: $FilePath" -ForegroundColor Red
        return $false
    }
}

function Test-EnvironmentConfig {
    param(
        [string]$EnvName,
        [string]$ConfigFile
    )
    
    Write-Host "`n???????????????????????????????????????" -ForegroundColor Cyan
    Write-Host "?? Testing $EnvName Environment" -ForegroundColor Cyan
    Write-Host "???????????????????????????????????????" -ForegroundColor Cyan
    
    $configPath = "src/BankingSystem.API/$ConfigFile"
    
    # 1. Config file exists and valid
    Write-Host "`n1?? Configuration File" -ForegroundColor Yellow
    if (Test-JsonFile $configPath) {
        Write-Host "   ? $ConfigFile is valid" -ForegroundColor Green
        $config = Get-Content $configPath | ConvertFrom-Json
    } else {
        return $false
    }
    
    # 2. Logging configuration
    Write-Host "`n2?? Logging Configuration" -ForegroundColor Yellow
    if ($config.Logging) {
        Write-Host "   ? Logging section present" -ForegroundColor Green
        
        if ($config.Logging.LogLevel) {
            Write-Host "   ? Log levels configured" -ForegroundColor Green
            Write-Host "      Default: $($config.Logging.LogLevel.Default)" -ForegroundColor Gray
        } else {
            Write-Host "   ??  No log levels defined" -ForegroundColor Yellow
        }
        
        if ($EnvName -ne "Test") {
            if ($config.Logging.Seq) {
                Write-Host "   ? Seq configuration present" -ForegroundColor Green
                if ($config.Logging.Seq.Url) {
                    Write-Host "      URL: $($config.Logging.Seq.Url)" -ForegroundColor Gray
                } else {
                    Write-Host "   ??  Seq URL empty (OK if set via env var)" -ForegroundColor Yellow
                }
            } else {
                Write-Host "   ??  No Seq configuration" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "   ? No logging configuration" -ForegroundColor Red
        return $false
    }
    
    # 3. Connection strings
    Write-Host "`n3?? Connection Strings" -ForegroundColor Yellow
    if ($config.ConnectionStrings) {
        Write-Host "   ? Connection strings present" -ForegroundColor Green
        
        $connections = @("DefaultConnection", "HangfireConnection", "Redis")
        foreach ($conn in $connections) {
            if ($config.ConnectionStrings.PSObject.Properties.Name -contains $conn) {
                $value = $config.ConnectionStrings.$conn
                if ($value -eq "" -and ($EnvName -eq "Staging" -or $EnvName -eq "Production")) {
                    Write-Host "   ? ${conn}: (empty - will be set via env var)" -ForegroundColor Green
                } elseif ($value -ne "") {
                    Write-Host "   ? ${conn}: Configured" -ForegroundColor Green
                } else {
                    Write-Host "   ??  ${conn}: Empty" -ForegroundColor Yellow
                }
            } else {
                if ($EnvName -eq "Staging" -or $EnvName -eq "Production") {
                    Write-Host "   ??  ${conn}: Not defined (should be empty string for env var override)" -ForegroundColor Yellow
                } else {
                    Write-Host "   ? ${conn}: Missing" -ForegroundColor Red
                }
            }
        }
    } else {
        Write-Host "   ? No connection strings" -ForegroundColor Red
    }
    
    # 4. JWT Settings
    Write-Host "`n4?? JWT Settings" -ForegroundColor Yellow
    if ($config.JwtSettings) {
        Write-Host "   ? JWT settings present" -ForegroundColor Green
        
        if ($config.JwtSettings.PSObject.Properties.Name -contains "Secret") {
            $secret = $config.JwtSettings.Secret
            if ($secret -eq "" -and ($EnvName -eq "Staging" -or $EnvName -eq "Production")) {
                Write-Host "   ? Secret: (empty - will be set via env var)" -ForegroundColor Green
            } elseif ($secret -and $secret.Length -ge 32) {
                Write-Host "   ? Secret: Configured ($($secret.Length) chars)" -ForegroundColor Green
            } elseif ($secret -and $secret.Length -lt 32) {
                Write-Host "   ??  Secret: Too short ($($secret.Length) chars, need 32+)" -ForegroundColor Yellow
            } else {
                Write-Host "   ? Secret: Missing" -ForegroundColor Red
            }
        } else {
            Write-Host "   ? Secret: Not defined" -ForegroundColor Red
        }
        
        if ($config.JwtSettings.Issuer -and $config.JwtSettings.Audience) {
            Write-Host "   ? Issuer/Audience configured" -ForegroundColor Green
        } else {
            Write-Host "   ??  Issuer/Audience missing" -ForegroundColor Yellow
        }
    } else {
        Write-Host "   ? No JWT settings" -ForegroundColor Red
    }
    
    # 5. Rate Limiting
    Write-Host "`n5?? Rate Limiting" -ForegroundColor Yellow
    if ($config.RateLimitSettings) {
        Write-Host "   ? Rate limiting configured" -ForegroundColor Green
        Write-Host "      Limit: $($config.RateLimitSettings.PermitLimit) requests / $($config.RateLimitSettings.WindowInSeconds)s" -ForegroundColor Gray
    } else {
        Write-Host "   ??  No rate limiting configuration" -ForegroundColor Yellow
    }
    
    # 6. Environment-specific checks
    Write-Host "`n6?? Environment-Specific Validation" -ForegroundColor Yellow
    
    switch ($EnvName) {
        "Development" {
            if ($config.Logging.LogLevel.Default -eq "Debug" -or $config.Logging.LogLevel.Default -eq "Information") {
                Write-Host "   ? Appropriate log level for development" -ForegroundColor Green
            } else {
                Write-Host "   ??  Consider Debug/Info log level for development" -ForegroundColor Yellow
            }
        }
        "Production" {
            if ($config.Logging.LogLevel.Default -eq "Warning" -or $config.Logging.LogLevel.Default -eq "Error") {
                Write-Host "   ? Appropriate log level for production" -ForegroundColor Green
            } else {
                Write-Host "   ??  Consider Warning/Error log level for production" -ForegroundColor Yellow
            }
            
            if ($config.AllowedHosts -and $config.AllowedHosts -ne "*") {
                Write-Host "   ? AllowedHosts restricted" -ForegroundColor Green
            } else {
                Write-Host "   ??  AllowedHosts should be restricted in production" -ForegroundColor Yellow
            }
        }
        "Test" {
            if ($config.ConnectionStrings.DefaultConnection -like "*:memory:*") {
                Write-Host "   ? Using in-memory database for tests" -ForegroundColor Green
            }
        }
    }
    
    Write-Host "`n? $EnvName configuration validation complete!" -ForegroundColor Green
    return $true
}

# Main execution
Write-Host "???????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?? ENVIRONMENT CONFIGURATION VALIDATOR" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????" -ForegroundColor Cyan

$environments = @(
    @{ Name = "Development"; File = "appsettings.Development.json" },
    @{ Name = "Docker"; File = "appsettings.Docker.json" },
    @{ Name = "Test"; File = "appsettings.Test.json" },
    @{ Name = "Staging"; File = "appsettings.Staging.json" },
    @{ Name = "Production"; File = "appsettings.Production.json" }
)

$testEnvironments = if ($Environment -eq "All") {
    $environments
} else {
    $environments | Where-Object { $_.Name -eq $Environment }
}

$allPassed = $true

foreach ($env in $testEnvironments) {
    $result = Test-EnvironmentConfig -EnvName $env.Name -ConfigFile $env.File
    if (-not $result) {
        $allPassed = $false
    }
}

# Summary
Write-Host "`n???????????????????????????????????????" -ForegroundColor Cyan
Write-Host "?? VALIDATION SUMMARY" -ForegroundColor Cyan
Write-Host "???????????????????????????????????????" -ForegroundColor Cyan

if ($allPassed) {
    Write-Host "`n? All environment configurations are valid!" -ForegroundColor Green
} else {
    Write-Host "`n??  Some configurations have issues - review above" -ForegroundColor Yellow
}

Write-Host "`n?? Configuration Files:" -ForegroundColor Cyan
Write-Host "  • appsettings.json (base)" -ForegroundColor White
Write-Host "  • appsettings.Development.json" -ForegroundColor White
Write-Host "  • appsettings.Docker.json" -ForegroundColor White
Write-Host "  • appsettings.Test.json" -ForegroundColor White
Write-Host "  • appsettings.Staging.json" -ForegroundColor White
Write-Host "  • appsettings.Production.json" -ForegroundColor White

Write-Host "`n?? Usage:" -ForegroundColor Cyan
Write-Host "  .\verify-config.ps1                    # Test all environments" -ForegroundColor Gray
Write-Host "  .\verify-config.ps1 -Environment Docker # Test specific environment" -ForegroundColor Gray

Write-Host ""
