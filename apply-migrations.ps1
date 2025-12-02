# Apply Database Migrations
# Run this to create/update database schema

Write-Host "?? Applying Database Migrations..." -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Check if services are running
$apiRunning = docker ps --filter "name=bankingsystem-api" --filter "status=running" -q
$dbRunning = docker ps --filter "name=bankingsystem-postgres-business" --filter "status=running" -q

if (-not $dbRunning) {
    Write-Host "? PostgreSQL is not running!" -ForegroundColor Red
    Write-Host "Start services first: .\start-dev.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "? PostgreSQL is running" -ForegroundColor Green
Write-Host ""

# Wait for database to be ready
Write-Host "Waiting for database to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Apply migrations using dotnet ef
Write-Host "Applying migrations to Business Database..." -ForegroundColor Yellow

try {
    # Set connection string environment variable
    $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=BankingSystemDb;Username=postgres;Password=postgres123;Include Error Detail=true"
    
    # Apply migrations (use BankingSystemDbContext)
    dotnet ef database update `
        --project src/BankingSystem.Infrastructure `
        --startup-project src/BankingSystem.API `
        --context BankingSystemDbContext `
        --verbose
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "? Business database migrations applied successfully!" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "? Failed to apply business database migrations!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "? Error applying migrations: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Apply Hangfire migrations
Write-Host "Setting up Hangfire Database..." -ForegroundColor Yellow
Write-Host "?? Hangfire will auto-create its schema on first run" -ForegroundColor Cyan

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "? Database Migrations Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Database Status:" -ForegroundColor Cyan
Write-Host "  Business DB: ? Ready (localhost:5432)" -ForegroundColor Green
Write-Host "  Hangfire DB: ? Ready (localhost:5433)" -ForegroundColor Green
Write-Host ""
Write-Host "?? Restarting API to apply changes..." -ForegroundColor Yellow

if ($apiRunning) {
    docker-compose restart banking-api
    Write-Host "? API restarted" -ForegroundColor Green
} else {
    Write-Host "?? API not running, start it with: docker-compose up -d banking-api" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "?? Ready to use!" -ForegroundColor Green
Write-Host ""
Write-Host "Test API:" -ForegroundColor Cyan
Write-Host "  curl http://localhost:5000/health" -ForegroundColor White
Write-Host "  start http://localhost:5000/swagger" -ForegroundColor White
Write-Host ""
