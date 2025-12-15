# ?? Deployment Scripts

This directory contains automated scripts for deploying Banking System to Kubernetes.

## ?? Available Scripts

### ?? Main Deployment

#### `deploy-local.bat` (Windows)
**Full automated deployment to local Kubernetes**

```powershell
.\scripts\deploy-local.bat
```

**What it does:**
1. ? Builds Docker image
2. ? Loads image to Kubernetes
3. ? Deploys PostgreSQL databases
4. ? Deploys Banking API
5. ? Waits for all pods to be ready
6. ? Shows access instructions

**Requirements:**
- Docker Desktop with Kubernetes enabled
- kubectl CLI tool

---

#### `deploy-local.sh` (Linux/Mac)
Same as above but for Unix systems

```bash
chmod +x scripts/deploy-local.sh
./scripts/deploy-local.sh
```

---

### ? Quick Actions

#### `quick-test.bat`
**One-click deploy + auto port-forward**

```powershell
.\scripts\quick-test.bat
```

Automatically:
- Runs full deployment
- Starts port-forward to localhost:8080
- Opens browser to Swagger UI (optional)

---

#### `preflight-check.bat`
**Verify system requirements before deployment**

```powershell
.\scripts\preflight-check.bat
```

Checks:
- ? Docker installed and running
- ? kubectl installed
- ? Kubernetes cluster accessible
- ? Project files exist
- ? Existing deployments (if any)

**Run this first if you encounter issues!**

---

#### `build-image.bat`
**Build Docker image only (for testing)**

```powershell
.\scripts\build-image.bat
```

Useful for:
- Testing Docker build without deploying
- Debugging Dockerfile issues
- Pre-building image before deployment

---

### ?? Monitoring

#### `check-status.bat`
**Check deployment status and health**

```powershell
.\scripts\check-status.bat
```

Shows:
- Deployment status
- Pod status
- Service endpoints
- Recent events
- Health check results
- Useful commands

---

### ?? Cleanup

#### `cleanup.bat`
**Remove all Kubernetes resources**

```powershell
.\scripts\cleanup.bat
```

**Warning:** This deletes:
- ? All pods and deployments
- ? All services
- ? PostgreSQL databases and data
- ? Entire namespace

Cannot be undone!

---

## ?? Typical Workflow

### First Time Setup
```powershell
# 1. Check prerequisites
.\scripts\preflight-check.bat

# 2. Deploy
.\scripts\deploy-local.bat

# 3. Check status
.\scripts\check-status.bat

# 4. Access application
kubectl port-forward -n banking-system svc/banking-api-service 8080:80
# Open: http://localhost:8080/swagger
```

### Development Workflow
```powershell
# Make code changes...

# Rebuild and redeploy
.\scripts\build-image.bat
kubectl rollout restart deployment/banking-api -n banking-system

# Check status
.\scripts\check-status.bat

# View logs
kubectl logs -n banking-system -l app=banking-api -f
```

### When Things Go Wrong
```powershell
# 1. Run pre-flight check
.\scripts\preflight-check.bat

# 2. Check current status
.\scripts\check-status.bat

# 3. Check logs
kubectl logs -n banking-system -l app=banking-api --tail=100

# 4. If needed, clean slate
.\scripts\cleanup.bat
.\scripts\deploy-local.bat
```

---

## ?? Script Comparison

| Script | Purpose | Time | When to Use |
|--------|---------|------|-------------|
| **preflight-check.bat** | Verify setup | 10s | Before first deploy or when troubleshooting |
| **build-image.bat** | Build Docker image | 2-5m | Test Dockerfile changes |
| **deploy-local.bat** | Full deployment | 5-10m | Initial setup, major changes |
| **quick-test.bat** | Deploy + forward | 5-10m | Quick testing |
| **check-status.bat** | Check health | 5s | Anytime to verify status |
| **cleanup.bat** | Remove all | 30s | Clean slate, before redeploy |

---

## ??? Customization

### Change Docker Image Tag
Edit in `deploy-local.bat`:
```batch
set IMAGE_TAG=v1.0.0
```

### Change Kubernetes Namespace
Edit in `deploy-local.bat`:
```batch
set NAMESPACE=my-banking-system
```

### Adjust Deployment Replicas
Edit in `deploy-local.bat`:
```batch
... -replace 'replicas: 3', 'replicas: 5' ...
```

### Change Port Forward Port
Edit in `quick-test.bat`:
```batch
kubectl port-forward -n banking-system svc/banking-api-service 9090:80
```

---

## ?? Common Issues

### Issue: "Docker build failed"
**Solution:**
```powershell
# Check Docker is running
docker ps

# Run pre-flight check
.\scripts\preflight-check.bat

# See TROUBLESHOOTING-K8S.md
```

### Issue: "ImagePullBackOff"
**Solution:**
```powershell
# For Docker Desktop - image should auto-load
# For Minikube - manually load
minikube image load banking-system-api:latest

# Or change pull policy in deployment.yml
imagePullPolicy: IfNotPresent
```

### Issue: "CrashLoopBackOff"
**Solution:**
```powershell
# Check logs
kubectl logs -n banking-system -l app=banking-api --tail=100

# Check PostgreSQL is running
kubectl get pods -n banking-system | findstr postgres

# See TROUBLESHOOTING-K8S.md
```

### Issue: "Port already in use"
**Solution:**
```powershell
# Find process using port
netstat -ano | findstr :8080

# Kill process
taskkill /PID <process-id> /F

# Or use different port
kubectl port-forward -n banking-system svc/banking-api-service 8081:80
```

---

## ?? Documentation

| File | Description |
|------|-------------|
| **QUICKSTART-K8S.md** | Quick start guide |
| **TROUBLESHOOTING-K8S.md** | Detailed troubleshooting guide |
| **k8s/README.md** | Complete K8s documentation |
| **k8s/QUICK-REFERENCE.md** | Command reference |

---

## ?? Behind the Scenes

### What `deploy-local.bat` Actually Does

```powershell
# 1. Navigate to project root
cd D:\WorkSpace\Personal\Dotnet\Bank

# 2. Build Docker image
docker build -f src\BankingSystem.API\Dockerfile -t banking-system-api:latest .

# 3. Load to K8s (if using Minikube)
minikube image load banking-system-api:latest

# 4. Create namespace
kubectl create namespace banking-system

# 5. Deploy PostgreSQL
kubectl apply -f k8s\postgres.yml

# 6. Wait for PostgreSQL
kubectl wait --for=condition=ready pod -l app=postgres -n banking-system

# 7. Deploy API (with modifications)
cat k8s\deployment.yml | 
  sed 's/image: .*/image: banking-system-api:latest/' |
  sed 's/imagePullPolicy: Always/imagePullPolicy: IfNotPresent/' |
  sed 's/replicas: 3/replicas: 2/' |
  kubectl apply -f -

# 8. Wait for API deployment
kubectl rollout status deployment/banking-api -n banking-system

# 9. Show status
kubectl get all -n banking-system
```

---

## ?? Tips

**Speed up development:**
```powershell
# Keep port-forward running in separate terminal
kubectl port-forward -n banking-system svc/banking-api-service 8080:80

# Quick rebuild and restart
docker build -f src\BankingSystem.API\Dockerfile -t banking-system-api:latest . && ^
kubectl rollout restart deployment/banking-api -n banking-system
```

**Monitor logs continuously:**
```powershell
# In separate terminal
kubectl logs -n banking-system -l app=banking-api -f
```

**Quick status check:**
```powershell
kubectl get pods -n banking-system -w
```

---

## ?? Learn More

- [Kubernetes Basics](https://kubernetes.io/docs/tutorials/kubernetes-basics/)
- [Docker Documentation](https://docs.docker.com/)
- [kubectl Cheat Sheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)

---

**Ready to deploy?**

```powershell
.\scripts\deploy-local.bat
```

??
