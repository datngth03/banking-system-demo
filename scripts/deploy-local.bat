@echo off
REM ===================================
REM Build and Deploy to Local K8s (Windows)
REM Usage: deploy-local.bat
REM ===================================

setlocal enabledelayedexpansion

echo ============================================
echo 🚀 Building and Deploying Banking System to Local Kubernetes
echo ============================================
echo.

REM Configuration
set IMAGE_NAME=banking-system-api
set IMAGE_TAG=latest
set BUILD_VERSION=1.0.0
set NAMESPACE=banking-system

REM Step 1: Build Docker Image
echo [Step 1] 📦 Building Docker image...
cd /d "%~dp0.."

REM Build with proper syntax and valid version
docker build -f src/BankingSystem.API/Dockerfile -t %IMAGE_NAME%:%IMAGE_TAG% --build-arg BUILD_VERSION=%BUILD_VERSION% .

if errorlevel 1 (
    echo ❌ Docker build failed
    pause
    exit /b 1
)

echo ✅ Docker image built successfully
echo.

REM Step 2: Load image to Kubernetes
echo [Step 2] 📤 Loading image to Kubernetes...

REM Check for Minikube
minikube status >nul 2>&1
if %errorlevel% equ 0 (
    echo Using Minikube...
    minikube image load %IMAGE_NAME%:%IMAGE_TAG%
) else (
    echo Using Docker Desktop Kubernetes - image already available
)

echo ✅ Image loaded to Kubernetes
echo.

REM Step 3: Create Namespace
echo [Step 3] 🏗️ Creating namespace...
kubectl create namespace %NAMESPACE% --dry-run=client -o yaml | kubectl apply -f -

REM Step 4: Deploy PostgreSQL
echo [Step 4] 🔐 Deploying PostgreSQL databases...
kubectl apply -f k8s\postgres.yml

REM Wait for PostgreSQL
echo ⏳ Waiting for PostgreSQL to be ready (this may take a minute)...
timeout /t 30 /nobreak >nul
kubectl wait --for=condition=ready pod -l app=postgres -n %NAMESPACE% --timeout=180s
if errorlevel 1 (
    echo ⚠️ Warning: PostgreSQL main database not ready yet, continuing...
)
kubectl wait --for=condition=ready pod -l app=postgres-hangfire -n %NAMESPACE% --timeout=180s
if errorlevel 1 (
    echo ⚠️ Warning: PostgreSQL Hangfire database not ready yet, continuing...
)

echo ✅ PostgreSQL deployment completed
echo.

REM Step 5: Update and deploy API
echo [Step 5] 🔄 Deploying Banking API...

REM Create temporary deployment file with updated image
powershell -Command "(Get-Content k8s\deployment.yml) -replace 'image: your-docker-registry/banking-system-api:latest', 'image: %IMAGE_NAME%:%IMAGE_TAG%' -replace 'imagePullPolicy: Always', 'imagePullPolicy: IfNotPresent' -replace 'replicas: 3', 'replicas: 2' | kubectl apply -f -"

if errorlevel 1 (
    echo ❌ Deployment failed
    pause
    exit /b 1
)

echo ✅ Deployment applied
echo.

REM Step 6: Wait for deployment
echo [Step 6] ⏳ Waiting for API deployment to be ready...
kubectl rollout status deployment/banking-api -n %NAMESPACE% --timeout=100s

if errorlevel 1 (
    echo ⚠️ Warning: Deployment may not be fully ready. Check status with: kubectl get pods -n %NAMESPACE%
)

REM Step 7: Display status
echo.
echo [Step 7] 📊 Deployment Status
echo ============================================
kubectl get all -n %NAMESPACE%

REM Success
echo.
echo ============================================
echo ✅ Deployment completed successfully!
echo ============================================
echo.
echo 🌐 Access the application:
echo.
echo   📌 Port forward to localhost:8080
echo   kubectl port-forward -n %NAMESPACE% svc/banking-api-service 8080:80
echo.
echo   📌 Or get Minikube service URL
echo   minikube service banking-api-service -n %NAMESPACE%
echo.
echo   📌 Check logs
echo   kubectl logs -n %NAMESPACE% -l app=banking-api --tail=100 -f
echo.
echo   📌 Access Swagger UI (after port-forward)
echo   http://localhost:8080/swagger
echo.
echo   📌 Access Hangfire Dashboard (after port-forward)
echo   http://localhost:8080/hangfire
echo   Username: admin
echo   Password: HangfireAdmin@2024
echo.
echo ============================================

pause
