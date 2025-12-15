@echo off
REM ===================================
REM Access Monitoring Tools
REM ===================================

echo.
echo ============================================
echo   Banking System - Monitoring Access
echo ============================================
echo.

set NAMESPACE=monitoring

REM Check if monitoring is deployed
kubectl get namespace %NAMESPACE% >nul 2>&1
if errorlevel 1 (
    echo ? Monitoring namespace not found!
    echo.
    echo Deploy monitoring first:
    echo   .\scripts\deploy-monitoring.bat
    echo.
    pause
    exit /b 1
)

echo ? Monitoring namespace found
echo.

REM Display menu
:menu
echo ============================================
echo   Select Tool to Access:
echo ============================================
echo.
echo   1. Prometheus (Metrics)
echo   2. Grafana (Dashboards)
echo   3. Seq (Logs)
echo   4. All (Open all in separate windows)
echo   5. Status (Check monitoring status)
echo   0. Exit
echo.
set /p choice="Enter choice (1-5): "

if "%choice%"=="1" goto prometheus
if "%choice%"=="2" goto grafana
if "%choice%"=="3" goto seq
if "%choice%"=="4" goto all
if "%choice%"=="5" goto status
if "%choice%"=="0" goto end
goto menu

:prometheus
echo.
echo ============================================
echo   ?? Starting Prometheus Port-Forward
echo ============================================
echo.
echo URL: http://localhost:9090
echo.
echo Press Ctrl+C to stop
echo.
kubectl port-forward -n %NAMESPACE% svc/prometheus 9090:9090
goto menu

:grafana
echo.
echo ============================================
echo   ?? Starting Grafana Port-Forward
echo ============================================
echo.
echo URL: http://localhost:3000
echo Username: admin
echo Password: admin123
echo.
echo Press Ctrl+C to stop
echo.
kubectl port-forward -n %NAMESPACE% svc/grafana 3000:3000
goto menu

:seq
echo.
echo ============================================
echo   ?? Starting Seq Port-Forward
echo ============================================
echo.
echo URL: http://localhost:5341
echo.
echo Press Ctrl+C to stop
echo.
kubectl port-forward -n %NAMESPACE% svc/seq 5341:80
goto menu

:all
echo.
echo ============================================
echo   ?? Opening All Monitoring Tools
echo ============================================
echo.
echo Starting port-forwards in new windows...
echo.

start "Prometheus" cmd /k "echo Prometheus: http://localhost:9090 && kubectl port-forward -n %NAMESPACE% svc/prometheus 9090:9090"
timeout /t 2 /nobreak >nul

start "Grafana" cmd /k "echo Grafana: http://localhost:3000 (admin/admin123) && kubectl port-forward -n %NAMESPACE% svc/grafana 3000:3000"
timeout /t 2 /nobreak >nul

start "Seq" cmd /k "echo Seq: http://localhost:5341 && kubectl port-forward -n %NAMESPACE% svc/seq 5341:80"
timeout /t 2 /nobreak >nul

echo.
echo ? All monitoring tools started!
echo.
echo   ?? Prometheus: http://localhost:9090
echo   ?? Grafana: http://localhost:3000
echo   ?? Seq: http://localhost:5341
echo.
pause
goto menu

:status
echo.
echo ============================================
echo   ?? Monitoring Stack Status
echo ============================================
echo.

echo Checking pods...
kubectl get pods -n %NAMESPACE%

echo.
echo Checking services...
kubectl get svc -n %NAMESPACE%

echo.
echo ============================================
echo.
pause
goto menu

:end
echo.
echo Exiting...
echo.
