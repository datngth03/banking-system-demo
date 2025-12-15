@echo off
REM ===================================
REM Pre-flight Check for K8s Deployment
REM ===================================

echo.
echo ============================================
echo   Pre-flight Checks
echo ============================================
echo.

set "PASS=?"
set "FAIL=?"
set "WARN=??"
set ALL_OK=1

REM Check Docker
echo Checking Docker...
docker --version >nul 2>&1
if errorlevel 1 (
    echo %FAIL% Docker not found
    echo    Install: https://www.docker.com/products/docker-desktop
    set ALL_OK=0
) else (
    for /f "tokens=*" %%i in ('docker --version') do echo %PASS% %%i
)

REM Check Docker daemon
docker ps >nul 2>&1
if errorlevel 1 (
    echo %FAIL% Docker daemon not running
    echo    Please start Docker Desktop
    set ALL_OK=0
) else (
    echo %PASS% Docker daemon is running
)

echo.

REM Check kubectl
echo Checking kubectl...
kubectl version --client >nul 2>&1
if errorlevel 1 (
    echo %FAIL% kubectl not found
    echo    Install: choco install kubernetes-cli
    set ALL_OK=0
) else (
    for /f "tokens=*" %%i in ('kubectl version --client --short 2^>nul') do echo %PASS% %%i
)

echo.

REM Check Kubernetes cluster
echo Checking Kubernetes cluster...
kubectl cluster-info >nul 2>&1
if errorlevel 1 (
    echo %FAIL% Kubernetes cluster not accessible
    echo    Enable Kubernetes in Docker Desktop:
    echo    Settings ? Kubernetes ? ?? Enable Kubernetes
    set ALL_OK=0
) else (
    echo %PASS% Kubernetes cluster is accessible
    kubectl get nodes 2>nul | findstr "Ready"
)

echo.

REM Check Minikube (optional)
echo Checking Minikube (optional)...
minikube version >nul 2>&1
if errorlevel 1 (
    echo %WARN% Minikube not found (optional - using Docker Desktop K8s)
) else (
    minikube status >nul 2>&1
    if errorlevel 1 (
        echo %WARN% Minikube not running (using Docker Desktop K8s)
    ) else (
        echo %PASS% Minikube is running
        minikube status | findstr "host\|kubelet\|apiserver"
    )
)

echo.

REM Check if solution file exists
echo Checking project files...
if exist "BankingSystem.sln" (
    echo %PASS% Solution file found
) else (
    echo %FAIL% Solution file not found
    echo    Make sure you're in the project root directory
    set ALL_OK=0
)

if exist "src\BankingSystem.API\Dockerfile" (
    echo %PASS% Dockerfile found
) else (
    echo %FAIL% Dockerfile not found
    set ALL_OK=0
)

if exist "k8s\deployment.yml" (
    echo %PASS% K8s deployment config found
) else (
    echo %FAIL% K8s deployment config not found
    set ALL_OK=0
)

if exist "k8s\postgres.yml" (
    echo %PASS% PostgreSQL config found
) else (
    echo %FAIL% PostgreSQL config not found
    set ALL_OK=0
)

echo.

REM Check available images
echo Checking existing Docker images...
docker images banking-system-api 2>nul | findstr "banking-system-api" >nul
if errorlevel 1 (
    echo %WARN% banking-system-api image not built yet (will be built during deployment)
) else (
    echo %PASS% banking-system-api image exists:
    docker images banking-system-api --format "    {{.Repository}}:{{.Tag}} ({{.Size}})"
)

echo.

REM Check namespace
echo Checking Kubernetes namespace...
kubectl get namespace banking-system >nul 2>&1
if errorlevel 1 (
    echo %WARN% Namespace 'banking-system' does not exist (will be created)
) else (
    echo %PASS% Namespace 'banking-system' exists
    kubectl get all -n banking-system 2>nul | findstr "pod\|deployment\|service" | findstr -v "NAME"
)

echo.
echo ============================================

if %ALL_OK%==1 (
    echo %PASS% All checks passed! Ready to deploy.
    echo.
    echo Run: .\scripts\deploy-local.bat
) else (
    echo %FAIL% Some checks failed. Please fix the issues above.
)

echo ============================================
echo.

pause
