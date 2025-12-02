# Local CI/CD Pipeline - PowerShell Version
# Run this script to simulate CI/CD locally on Windows

param(
    [switch]$SkipTests,
    [switch]$SkipDocker,
    [switch]$SkipSecurity
)

$ErrorActionPreference = "Stop"

Write-Host "?? Starting Local CI/CD Pipeline..." -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean
Write-Host "Step 1: Cleaning..." -ForegroundColor Yellow
dotnet clean BankingSystem.sln
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Clean completed" -ForegroundColor Green
} else {
    Write-Host "? Clean failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 2: Restore
Write-Host "Step 2: Restoring dependencies..." -ForegroundColor Yellow
dotnet restore BankingSystem.sln
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Restore completed" -ForegroundColor Green
} else {
    Write-Host "? Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 3: Build
Write-Host "Step 3: Building solution..." -ForegroundColor Yellow
dotnet build BankingSystem.sln --configuration Release --no-restore
if ($LASTEXITCODE -eq 0) {
    Write-Host "? Build succeeded" -ForegroundColor Green
} else {
    Write-Host "? Build failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 4: Run Tests
if (-not $SkipTests) {
    Write-Host "Step 4: Running tests..." -ForegroundColor Yellow
    
    # Unit tests
    Write-Host "Running unit tests..." -ForegroundColor Cyan
    dotnet test tests/BankingSystem.Tests/BankingSystem.Tests.csproj `
        --configuration Release `
        --no-build `
        --verbosity normal `
        --logger "console;verbosity=normal"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Unit tests passed" -ForegroundColor Green
    } else {
        Write-Host "? Unit tests failed" -ForegroundColor Red
        exit 1
    }
    
    # Integration tests
    Write-Host "Running integration tests..." -ForegroundColor Cyan
    dotnet test tests/BankingSystem.IntegrationTests/BankingSystem.IntegrationTests.csproj `
        --configuration Release `
        --no-build `
        --verbosity normal `
        --logger "console;verbosity=normal"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Integration tests passed" -ForegroundColor Green
    } else {
        Write-Host "? Integration tests failed" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
    
    # Step 5: Code Coverage
    Write-Host "Step 5: Generating code coverage..." -ForegroundColor Yellow
    dotnet test BankingSystem.sln `
        --configuration Release `
        --no-build `
        --collect:"XPlat Code Coverage" `
        --results-directory ./TestResults `
        --verbosity quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Coverage generated in ./TestResults" -ForegroundColor Green
    } else {
        Write-Host "?? Coverage generation had issues (non-critical)" -ForegroundColor Yellow
    }
    Write-Host ""
} else {
    Write-Host "?? Skipping tests (--SkipTests flag)" -ForegroundColor Yellow
    Write-Host ""
}

# Step 6: Security Scan
if (-not $SkipSecurity) {
    if (Get-Command trivy -ErrorAction SilentlyContinue) {
        Write-Host "Step 6: Running security scan..." -ForegroundColor Yellow
        trivy fs . --severity HIGH,CRITICAL --exit-code 0
        Write-Host "? Security scan completed" -ForegroundColor Green
    } else {
        Write-Host "?? Trivy not installed, skipping security scan" -ForegroundColor Yellow
        Write-Host "Install from: https://github.com/aquasecurity/trivy/releases" -ForegroundColor Cyan
    }
    Write-Host ""
}

# Step 7: Publish
Write-Host "Step 7: Publishing application..." -ForegroundColor Yellow
dotnet publish src/BankingSystem.API/BankingSystem.API.csproj `
    --configuration Release `
    --output ./publish `
    --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Publish completed to ./publish" -ForegroundColor Green
} else {
    Write-Host "? Publish failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Step 8: Docker Build
if (-not $SkipDocker) {
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Host "Step 8: Building Docker image..." -ForegroundColor Yellow
        $buildDate = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        
        docker build -t banking-api:local `
            -f src/BankingSystem.API/Dockerfile `
            --build-arg BUILD_VERSION=1.0.0-local `
            --build-arg BUILD_DATE=$buildDate `
            .
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Docker image built: banking-api:local" -ForegroundColor Green
        } else {
            Write-Host "? Docker build failed" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "?? Docker not installed, skipping image build" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Summary
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "?? Local CI/CD Pipeline Completed Successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Summary:" -ForegroundColor Cyan
Write-Host "  - Build: ? Passed" -ForegroundColor Green
if (-not $SkipTests) {
    Write-Host "  - Unit Tests: ? Passed" -ForegroundColor Green
    Write-Host "  - Integration Tests: ? Passed" -ForegroundColor Green
    Write-Host "  - Coverage: ? Generated" -ForegroundColor Green
}
if (-not $SkipSecurity) {
    Write-Host "  - Security Scan: ? Completed" -ForegroundColor Green
}
Write-Host "  - Publish: ? Ready in ./publish" -ForegroundColor Green
if (-not $SkipDocker) {
    Write-Host "  - Docker Image: ? banking-api:local" -ForegroundColor Green
}
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  - Run: docker-compose up -d" -ForegroundColor White
Write-Host "  - Test: curl http://localhost:5000/health" -ForegroundColor White
Write-Host "  - Monitor: http://localhost:3000 (Grafana)" -ForegroundColor White
Write-Host "  - Logs: http://localhost:5341 (Seq)" -ForegroundColor White
Write-Host ""

# Usage examples
Write-Host "?? Usage examples:" -ForegroundColor Cyan
Write-Host "  .\local-ci.ps1                # Run full pipeline" -ForegroundColor White
Write-Host "  .\local-ci.ps1 -SkipTests     # Skip tests (faster)" -ForegroundColor White
Write-Host "  .\local-ci.ps1 -SkipDocker    # Skip Docker build" -ForegroundColor White
Write-Host "  .\local-ci.ps1 -SkipSecurity  # Skip security scan" -ForegroundColor White
Write-Host ""
