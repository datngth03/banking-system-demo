@echo off
REM ===================================
REM Cleanup Banking System from K8s
REM ===================================

echo.
echo ============================================
echo   Cleanup Banking System from K8s
echo ============================================
echo.

set NAMESPACE=banking-system

REM Check if namespace exists
kubectl get namespace %NAMESPACE% >nul 2>&1
if errorlevel 1 (
    echo ? Already clean - namespace not found
    pause
    exit /b 0
)

echo This will DELETE:
echo   - All pods and deployments
echo   - All services
echo   - PostgreSQL databases and data
echo   - Entire namespace: %NAMESPACE%
echo.
echo ??  WARNING: This action cannot be undone!
echo.
set /p CONFIRM="Are you sure? Type 'yes' to confirm: "

if /i not "%CONFIRM%"=="yes" (
    echo Cancelled.
    exit /b 0
)

echo.
echo Deleting namespace and all resources...
echo.

kubectl delete namespace %NAMESPACE%

if errorlevel 1 (
    echo ? Failed to delete namespace
    pause
    exit /b 1
)

echo.
echo ? Cleanup completed!
echo.
echo All Banking System resources have been removed.
echo.

REM Ask if user wants to clean Docker images
echo.
set /p CLEAN_IMAGES="Do you want to remove Docker images too? (y/n): "

if /i "%CLEAN_IMAGES%"=="y" (
    echo.
    echo Removing Docker images...
    
    docker images | findstr banking-system-api >nul 2>&1
    if not errorlevel 1 (
        for /f "tokens=3" %%i in ('docker images ^| findstr banking-system-api') do (
            docker rmi %%i 2>nul
        )
        echo ? Docker images removed
    ) else (
        echo No banking-system images found
    )
)

echo.
echo ============================================
echo   Cleanup Summary
echo ============================================
echo.
echo ? Namespace deleted
echo ? All pods removed
echo ? All services removed
echo ? All data deleted
if /i "%CLEAN_IMAGES%"=="y" echo ? Docker images removed
echo.
echo You can redeploy anytime using: deploy-local.bat
echo.

pause
