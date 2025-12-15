@echo off
REM ===================================
REM Check Banking System K8s Status
REM ===================================

echo.
echo ============================================
echo   Banking System K8s Status
echo ============================================
echo.

set NAMESPACE=banking-system

REM Check if namespace exists
kubectl get namespace %NAMESPACE% >nul 2>&1
if errorlevel 1 (
    echo ? Namespace '%NAMESPACE%' not found!
    echo    Run deploy-local.bat first
    pause
    exit /b 1
)

echo [Namespace: %NAMESPACE%]
echo.

REM Check all resources
echo ----------------------------------------
echo   Deployments:
echo ----------------------------------------
kubectl get deployments -n %NAMESPACE%
echo.

echo ----------------------------------------
echo   Pods:
echo ----------------------------------------
kubectl get pods -n %NAMESPACE% -o wide
echo.

echo ----------------------------------------
echo   Services:
echo ----------------------------------------
kubectl get svc -n %NAMESPACE%
echo.

echo ----------------------------------------
echo   HPA (Auto-scaling):
echo ----------------------------------------
kubectl get hpa -n %NAMESPACE%
echo.

echo ----------------------------------------
echo   Recent Events:
echo ----------------------------------------
kubectl get events -n %NAMESPACE% --sort-by='.lastTimestamp' | tail -n 10
echo.

REM Check if API is ready
echo ----------------------------------------
echo   API Health Check:
echo ----------------------------------------

kubectl get pods -n %NAMESPACE% -l app=banking-api -o name >nul 2>&1
if errorlevel 1 (
    echo ? No API pods found
) else (
    for /f "tokens=*" %%i in ('kubectl get pods -n %NAMESPACE% -l app=banking-api -o name ^| head -n 1') do set POD_NAME=%%i
    
    echo Testing health endpoint...
    kubectl exec -n %NAMESPACE% %POD_NAME% -- curl -s http://localhost:80/health 2>nul
    if errorlevel 1 (
        echo ? Health check failed
    ) else (
        echo ? API is healthy
    )
)

echo.
echo ============================================
echo   Quick Commands:
echo ============================================
echo.
echo   ?? View logs:
echo   kubectl logs -n %NAMESPACE% -l app=banking-api --tail=50 -f
echo.
echo   ?? Port forward:
echo   kubectl port-forward -n %NAMESPACE% svc/banking-api-service 8080:80
echo.
echo   ???  Access database:
echo   kubectl exec -it -n %NAMESPACE% [postgres-pod] -- psql -U postgres -d BankingSystemDb
echo.
echo   ?? Describe pod:
echo   kubectl describe pod -n %NAMESPACE% [pod-name]
echo.
echo   ?? Delete all:
echo   kubectl delete namespace %NAMESPACE%
echo.

pause
