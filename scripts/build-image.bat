@echo off
REM ===================================
REM Build Docker Image Only
REM ===================================

echo.
echo ============================================
echo   Building Docker Image
echo ============================================
echo.

cd /d "%~dp0.."

echo Current directory: %CD%
echo.

REM Check if Dockerfile exists
if not exist "src\BankingSystem.API\Dockerfile" (
    echo ? ERROR: Dockerfile not found at src\BankingSystem.API\Dockerfile
    pause
    exit /b 1
)

echo ? Dockerfile found
echo.

REM Check if solution exists
if not exist "BankingSystem.sln" (
    echo ? ERROR: Solution file not found
    pause
    exit /b 1
)

echo ? Solution file found
echo.

echo Building image...
echo Command: docker build -f src\BankingSystem.API\Dockerfile -t banking-system-api:latest --build-arg BUILD_VERSION=1.0.0 .
echo.

docker build -f src\BankingSystem.API\Dockerfile -t banking-system-api:latest --build-arg BUILD_VERSION=1.0.0 .

if errorlevel 1 (
    echo.
    echo ? Build failed!
    echo.
    echo Troubleshooting:
    echo 1. Make sure Docker Desktop is running
    echo 2. Check if you have enough disk space
    echo 3. Try: docker system prune
    echo 4. Check Dockerfile syntax
    echo 5. Check for NuGet package vulnerabilities
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================
echo ? Build successful!
echo ============================================
echo.

docker images banking-system-api

echo.
echo Next steps:
echo 1. Test the image: docker run -p 8080:8080 banking-system-api:latest
echo 2. Deploy to K8s: .\scripts\deploy-local.bat
echo.

pause
