@echo off
REM ===================================
REM Quick Test Banking System on K8s
REM ===================================

echo.
echo ============================================
echo   Quick K8s Deployment Test
echo ============================================
echo.

REM Check if kubectl is available
kubectl version --client >nul 2>&1
if errorlevel 1 (
    echo ? kubectl not found! Please install kubectl first.
    echo    Install: choco install kubernetes-cli
    pause
    exit /b 1
)

REM Check if Docker is running
docker ps >nul 2>&1
if errorlevel 1 (
    echo ? Docker is not running! Please start Docker Desktop.
    pause
    exit /b 1
)

echo ? Prerequisites OK
echo.

REM Ask user confirmation
echo This will:
echo   1. Build Docker image
echo   2. Deploy to Kubernetes
echo   3. Start port-forward on http://localhost:8080
echo.
set /p CONTINUE="Continue? (y/n): "
if /i not "%CONTINUE%"=="y" (
    echo Cancelled.
    exit /b 0
)

echo.
echo Starting deployment...
echo.

REM Run the full deployment
call "%~dp0deploy-local.bat"

if errorlevel 1 (
    echo.
    echo ? Deployment failed!
    pause
    exit /b 1
)

echo.
echo ============================================
echo   Deployment Successful!
echo ============================================
echo.
echo Starting port-forward...
echo Access at: http://localhost:8080
echo.
echo Press Ctrl+C to stop port-forward
echo.

REM Start port forward
kubectl port-forward -n banking-system svc/banking-api-service 8080:80
