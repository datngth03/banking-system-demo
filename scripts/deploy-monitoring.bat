@echo off
REM ===================================
REM Deploy Monitoring Stack to K8s
REM Prometheus + Grafana + Seq
REM ===================================

echo.
echo ============================================
echo   Deploying Monitoring Stack to K8s
echo   Prometheus + Grafana + Seq
echo ============================================
echo.

set NAMESPACE=monitoring

REM Step 1: Deploy monitoring stack
echo [Step 1] ?? Deploying Prometheus, Grafana, and Seq...
kubectl apply -f k8s\monitoring.yml

if errorlevel 1 (
    echo ? Deployment failed
    pause
    exit /b 1
)

echo ? Monitoring stack deployed
echo.

REM Step 2: Wait for pods to be ready
echo [Step 2] ? Waiting for monitoring pods to be ready...
timeout /t 10 /nobreak >nul

echo.
echo Waiting for Prometheus...
kubectl wait --for=condition=ready pod -l app=prometheus -n %NAMESPACE% --timeout=180s

echo.
echo Waiting for Grafana...
kubectl wait --for=condition=ready pod -l app=grafana -n %NAMESPACE% --timeout=180s

echo.
echo Waiting for Seq...
kubectl wait --for=condition=ready pod -l app=seq -n %NAMESPACE% --timeout=180s

echo.
echo ? All monitoring services are ready!
echo.

REM Step 3: Display status
echo [Step 3] ?? Monitoring Stack Status
echo ============================================
kubectl get all -n %NAMESPACE%

echo.
echo ============================================
echo ? Monitoring Stack Deployed Successfully!
echo ============================================
echo.
echo ?? Access the monitoring tools:
echo.
echo   ?? Prometheus (run in separate terminals):
echo   kubectl port-forward -n %NAMESPACE% svc/prometheus 9090:9090
echo   Then open: http://localhost:9090
echo.
echo   ?? Grafana:
echo   kubectl port-forward -n %NAMESPACE% svc/grafana 3000:3000
echo   Then open: http://localhost:3000
echo   Username: admin
echo   Password: admin123
echo.
echo   ?? Seq:
echo   kubectl port-forward -n %NAMESPACE% svc/seq 5341:80
echo   Then open: http://localhost:5341
echo.
echo ============================================
echo.
echo ?? Quick commands:
echo.
echo   Check status:
echo   kubectl get pods -n %NAMESPACE%
echo.
echo   View Prometheus logs:
echo   kubectl logs -n %NAMESPACE% -l app=prometheus
echo.
echo   View Grafana logs:
echo   kubectl logs -n %NAMESPACE% -l app=grafana
echo.
echo   View Seq logs:
echo   kubectl logs -n %NAMESPACE% -l app=seq
echo.

pause
