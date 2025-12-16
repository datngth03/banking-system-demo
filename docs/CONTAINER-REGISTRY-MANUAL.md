# Container Registry Manual Setup

## ?? Why Manual Setup?

Azure Student/Trial subscriptions may not support Container Registry in all regions or may have restrictions. This guide shows you how to create it manually.

---

## ?? **Option 1: Create via Azure Portal (Easiest)**

### **Step 1: Navigate to Container Registries**

```
1. Go to: https://portal.azure.com
2. Search for "Container Registries"
3. Click "Create"
```

### **Step 2: Fill in Details**

```
Basics:
- Subscription: Azure subscription 1
- Resource Group: rg-banking-dev
- Registry name: bankingcrdev (or any unique name)
- Location: East US (or any supported region)
- SKU: Basic

Networking:
- Public access: Enabled

Tags:
- Environment: dev
- Project: BankingSystem
```

### **Step 3: Create**

```
Click "Review + Create" ? "Create"
Wait 1-2 minutes for deployment
```

---

## ?? **Option 2: Create via Azure CLI**

```powershell
# Try different regions if one fails
$regions = @("eastus", "westus2", "northeurope", "westeurope")

foreach ($region in $regions) {
    Write-Host "Trying region: $region" -ForegroundColor Yellow
    
    az acr create `
        --resource-group rg-banking-dev `
        --name bankingcrdev `
        --sku Basic `
        --location $region `
        --admin-enabled true
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Success in region: $region" -ForegroundColor Green
        break
    }
}
```

---

## ?? **Option 3: Use Alternative - Docker Hub (FREE)**

If Container Registry is not available:

### **Step 1: Create Docker Hub Account**

```
1. Go to: https://hub.docker.com
2. Sign up (FREE)
3. Create repository: yourusername/banking-api
```

### **Step 2: Build and Push**

```powershell
# Login to Docker Hub
docker login

# Build image
docker build -t yourusername/banking-api:latest -f src/BankingSystem.API/Dockerfile .

# Push image
docker push yourusername/banking-api:latest
```

### **Step 3: Update Container App**

```powershell
# Update Container App to use Docker Hub image
az containerapp update `
    --name banking-dev-api `
    --resource-group rg-banking-dev `
    --image yourusername/banking-api:latest
```

---

## ?? **After Creating Container Registry**

### **Get Registry Credentials**

```powershell
# Get login server
az acr show --name bankingcrdev --query loginServer --output tsv

# Enable admin user
az acr update --name bankingcrdev --admin-enabled true

# Get credentials
az acr credential show --name bankingcrdev
```

### **Build and Push Your Image**

```powershell
# Login to ACR
az acr login --name bankingcrdev

# Build on Azure (recommended)
az acr build `
    --registry bankingcrdev `
    --image banking-api:latest `
    --file src/BankingSystem.API/Dockerfile `
    .

# Or build locally and push
docker build -t bankingcrdev.azurecr.io/banking-api:latest -f src/BankingSystem.API/Dockerfile .
docker push bankingcrdev.azurecr.io/banking-api:latest
```

### **Update Container App**

```powershell
# Get ACR credentials
$acrServer = az acr show --name bankingcrdev --query loginServer --output tsv
$acrUser = az acr credential show --name bankingcrdev --query username --output tsv
$acrPassword = az acr credential show --name bankingcrdev --query "passwords[0].value" --output tsv

# Update Container App
az containerapp update `
    --name banking-dev-api `
    --resource-group rg-banking-dev `
    --image "$acrServer/banking-api:latest" `
    --registry-server $acrServer `
    --registry-username $acrUser `
    --registry-password $acrPassword
```

---

## ?? **Cost Comparison**

| Option | Cost/Month | Pros | Cons |
|--------|------------|------|------|
| **Azure Container Registry Basic** | $5 | Fast, integrated | May not be available |
| **Docker Hub Free** | $0 | Always available | Slower, public images |
| **Docker Hub Pro** | $5 | Private repos | External service |

---

## ?? **Recommendation**

### **For Development:**
Use **Docker Hub Free** - No Azure restrictions, works everywhere

### **For Production:**
Use **Azure Container Registry** - Better integration, faster pulls in Azure

---

## ?? **Current Deployment Status**

Your deployment is using **Microsoft sample image** (`mcr.microsoft.com/dotnet/samples:aspnetapp`)

To deploy your Banking System:
1. ? Create Container Registry (manual or Docker Hub)
2. ? Build and push your image
3. ? Update Container App with your image

---

## ?? **Need Help?**

Check these resources:
- [Azure Container Registry Docs](https://learn.microsoft.com/azure/container-registry/)
- [Docker Hub Docs](https://docs.docker.com/docker-hub/)
- [Container Apps Image Pull](https://learn.microsoft.com/azure/container-apps/containers)
