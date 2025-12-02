# Quick Start Development Environment
# This script starts all services needed for local development

Write-Host "?? Starting Banking System Development Environment..." -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check Docker
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "? Docker is not installed!" -ForegroundColor Red
    Write-Host "Please install Docker Desktop: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

# Check .env file
if (-not (Test-Path ".env")) {
    Write-Host "?? .env file not found!" -ForegroundColor Yellow
    Write-Host "Creating .env from .env.example..." -ForegroundColor Cyan
    
    if (Test-Path ".env.example") {
        Copy-Item ".env.example" ".env"
        Write-Host "? Created .env file" -ForegroundColor Green
        Write-Host ""
        Write-Host "?? IMPORTANT: Generate secrets with:" -ForegroundColor Yellow
        Write-Host "  .\generate-secrets.ps1" -ForegroundColor White
        Write-Host ""
        
        $response = Read-Host "Do you want to generate secrets now? (y/n)"
        if ($response -eq "y") {
            .\generate-secrets.ps1
        } else {
            Write-Host "Please run .\generate-secrets.ps1 before continuing" -ForegroundColor Yellow
            exit 0
        }
    } else {
        Write-Host "? .env.example not found!" -ForegroundColor Red
        exit 1
    }
}

Write-Host "? Prerequisites check passed" -ForegroundColor Green
Write-Host ""

# Ask which profile to use
Write-Host "?? Select Docker Compose Profile:" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Default (Core services only) - RECOMMENDED" -ForegroundColor Green
Write-Host "   ? PostgreSQL, Redis, API" -ForegroundColor White
Write-Host "   ?? Fast startup, minimal RAM (~500MB)" -ForegroundColor Gray
Write-Host ""
Write-Host "2. Monitoring (Core + Monitoring tools)" -ForegroundColor Yellow
Write-Host "   ? + Seq, Prometheus, Grafana" -ForegroundColor White
Write-Host "   ?? For debugging (~1.5GB RAM)" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Tools (Core + Database tools)" -ForegroundColor Yellow
Write-Host "   ? + PgAdmin" -ForegroundColor White
Write-Host "   ?? For database management (~700MB RAM)" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Full (All services)" -ForegroundColor Magenta
Write-Host "   ? Everything above" -ForegroundColor White
Write-Host "   ?? For full testing (~2GB RAM)" -ForegroundColor Gray
Write-Host ""

$choice = Read-Host "Enter choice (1-4, default=1)"
if ([string]::IsNullOrWhiteSpace($choice)) { $choice = "1" }

$profile = switch ($choice) {
    "1" { "" }
    "2" { "--profile monitoring" }
    "3" { "--profile tools" }
    "4" { "--profile full" }
    default { "" }
}

$profileName = switch ($choice) {
    "1" { "Default (Core only)" }
    "2" { "Monitoring" }
    "3" { "Tools" }
    "4" { "Full" }
    default { "Default (Core only)" }
}

Write-Host ""
Write-Host "Selected profile: $profileName" -ForegroundColor Cyan
Write-Host ""

# Stop any existing containers
Write-Host "Stopping existing containers..." -ForegroundColor Yellow
docker-compose down 2>$null
Write-Host "? Existing containers stopped" -ForegroundColor Green
Write-Host ""

# Pull latest images
Write-Host "Pulling latest images..." -ForegroundColor Yellow
if ($profile) {
    docker-compose $profile.Split() pull
} else {
    docker-compose pull
}
Write-Host "? Images updated" -ForegroundColor Green
Write-Host ""

# Build application
Write-Host "Building application..." -ForegroundColor Yellow
dotnet build BankingSystem.sln --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? Application built" -ForegroundColor Green
Write-Host ""

# Start services
Write-Host "Starting services with $profileName profile..." -ForegroundColor Yellow
if ($profile) {
    docker-compose $profile.Split() up -d
} else {
    docker-compose up -d
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Services started successfully!" -ForegroundColor Green
} else {
    Write-Host "? Failed to start services!" -ForegroundColor Red
    docker-compose logs
    exit 1
}
Write-Host ""

# Wait for services to be ready
Write-Host "Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Apply database migrations automatically
Write-Host "?? Checking database migrations..." -ForegroundColor Yellow
$dbRunning = docker ps --filter "name=bankingsystem-postgres-business" --filter "status=running" -q

if ($dbRunning) {
    Write-Host "Applying database migrations..." -ForegroundColor Cyan
    
    try {
        $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=BankingSystemDb;Username=postgres;Password=postgres123;Include Error Detail=true"
        
        dotnet ef database update `
            --project src/BankingSystem.Infrastructure `
            --startup-project src/BankingSystem.API `
            --context BankingSystemDbContext `
            2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Database migrations applied" -ForegroundColor Green
        } else {
            Write-Host "?? Migration failed - database may already be up to date" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "?? Could not apply migrations (may be normal if already applied)" -ForegroundColor Yellow
    }
} else {
    Write-Host "?? PostgreSQL not running, skipping migrations" -ForegroundColor Yellow
}

Write-Host ""

# Restart API to ensure migrations are applied
Write-Host "?? Restarting API..." -ForegroundColor Yellow
docker-compose restart banking-api 2>&1 | Out-Null
Write-Host "? API restarted" -ForegroundColor Green
Write-Host ""

# Check health
Write-Host "Checking service health..." -ForegroundColor Yellow

$maxAttempts = 30
$attempt = 0
$healthy = $false

while ($attempt -lt $maxAttempts -and -not $healthy) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:5000/health" -UseBasicParsing -TimeoutSec 2
        if ($response.StatusCode -eq 200) {
            $healthy = $true
            Write-Host "? API is healthy!" -ForegroundColor Green
        }
    } catch {
        $attempt++
        Write-Host "  Waiting for API... ($attempt/$maxAttempts)" -ForegroundColor Cyan
        Start-Sleep -Seconds 2
    }
}

if (-not $healthy) {
    Write-Host "?? API health check timeout (this might be normal on first run)" -ForegroundColor Yellow
    Write-Host "Check logs with: docker-compose logs -f banking-api" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "?? Development Environment is Ready!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Running Services ($profileName):" -ForegroundColor Cyan
Write-Host ""

# Core services (always)
Write-Host "  ? Core Services:" -ForegroundColor Green
Write-Host "    API:         http://localhost:5000" -ForegroundColor White
Write-Host "    Swagger:     http://localhost:5000/swagger" -ForegroundColor White
Write-Host "    Metrics:     http://localhost:5000/metrics" -ForegroundColor White
Write-Host "    Health:      http://localhost:5000/health" -ForegroundColor White
Write-Host "    PostgreSQL:  localhost:5432 (business)" -ForegroundColor White
Write-Host "    PostgreSQL:  localhost:5433 (hangfire)" -ForegroundColor White
Write-Host "    Redis:       localhost:6379" -ForegroundColor White
Write-Host ""

# Monitoring services (if profile includes)
if ($choice -eq "2" -or $choice -eq "4") {
    Write-Host "  ?? Monitoring Services:" -ForegroundColor Yellow
    Write-Host "    Seq Logs:    http://localhost:5341" -ForegroundColor White
    Write-Host "    Prometheus:  http://localhost:9090" -ForegroundColor White
    Write-Host "    Grafana:     http://localhost:3000 (admin/admin)" -ForegroundColor White
    Write-Host ""
}

# Tools (if profile includes)
if ($choice -eq "3" -or $choice -eq "4") {
    Write-Host "  ?? Database Tools:" -ForegroundColor Yellow
    Write-Host "    PgAdmin:     http://localhost:5050 (admin@bankingsystem.com/admin)" -ForegroundColor White
    Write-Host ""
}

Write-Host "?? Next steps:" -ForegroundColor Cyan
Write-Host "  - Open Swagger: start http://localhost:5000/swagger" -ForegroundColor White
Write-Host "  - View logs:    docker-compose logs -f banking-api" -ForegroundColor White
Write-Host "  - Run tests:    dotnet test" -ForegroundColor White
Write-Host "  - Stop all:     .\stop-dev.ps1" -ForegroundColor White
Write-Host ""
Write-Host "?? Quick commands:" -ForegroundColor Cyan
Write-Host "  docker-compose ps              # List services" -ForegroundColor White
Write-Host "  docker-compose logs -f         # View all logs" -ForegroundColor White
Write-Host "  docker-compose restart api     # Restart API" -ForegroundColor White
Write-Host "  .\stop-dev.ps1                 # Stop everything" -ForegroundColor White
Write-Host ""
Write-Host "?? Documentation:" -ForegroundColor Cyan
Write-Host "  docs/DOCKER-PROFILES.md        # Profile guide" -ForegroundColor White
Write-Host "  docs/LOCAL-DEVELOPMENT.md      # Development guide" -ForegroundColor White
Write-Host ""

# Open browser (optional)
$openBrowser = Read-Host "Open Swagger in browser? (y/n)"
if ($openBrowser -eq "y") {
    Start-Process "http://localhost:5000/swagger"
}

Write-Host "Happy coding! ??" -ForegroundColor Green
Write-Host ""
