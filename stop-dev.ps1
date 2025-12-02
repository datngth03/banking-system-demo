# Stop Development Environment
# This script stops all running services

Write-Host "?? Stopping Banking System Development Environment..." -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Ask for confirmation
$confirmation = Read-Host "This will stop all services. Continue? (y/n)"
if ($confirmation -ne "y") {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

# Check if docker-compose.yml exists
if (-not (Test-Path "docker-compose.yml")) {
    Write-Host "? docker-compose.yml not found!" -ForegroundColor Red
    Write-Host "Make sure you're in the project root directory." -ForegroundColor Yellow
    exit 1
}

# Stop and remove containers
Write-Host "Stopping containers..." -ForegroundColor Yellow
docker-compose down

if ($LASTEXITCODE -eq 0) {
    Write-Host "? Containers stopped successfully" -ForegroundColor Green
} else {
    Write-Host "? Error stopping containers" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Optional: Remove volumes
$removeVolumes = Read-Host "Remove volumes (database data will be lost)? (y/n)"
if ($removeVolumes -eq "y") {
    Write-Host "Removing volumes..." -ForegroundColor Yellow
    docker-compose down -v
    Write-Host "? Volumes removed" -ForegroundColor Green
} else {
    Write-Host "?? Volumes preserved (database data retained)" -ForegroundColor Cyan
}

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "? Development Environment Stopped" -ForegroundColor Green
Write-Host ""
Write-Host "To start again, run: .\start-dev.ps1" -ForegroundColor Cyan
Write-Host ""

# Show Docker status
Write-Host "?? Docker Status:" -ForegroundColor Cyan
docker ps -a --filter "name=banking" --format "table {{.Names}}\t{{.Status}}"
Write-Host ""
