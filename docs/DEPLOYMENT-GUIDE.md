# ?? DEPLOYMENT GUIDE

Complete guide for deploying Banking System API to various environments.

---

## ?? **PRE-DEPLOYMENT CHECKLIST**

### **Required Before Any Deployment**
- [ ] All tests passing locally (`dotnet test`)
- [ ] Load tests completed successfully (`.\test-workflow.ps1`)
- [ ] Database migrations reviewed
- [ ] Environment variables configured
- [ ] Secrets generated and secured
- [ ] Backup strategy in place
- [ ] Rollback plan documented
- [ ] Monitoring configured

---

## ?? **ENVIRONMENT SETUP**

### **1. Generate Required Secrets**

```powershell
# Generate JWT Secret (minimum 32 characters)
openssl rand -base64 32
# Output: Copy this to JWT_SECRET

# Generate Encryption Key (32 bytes)
openssl rand -base64 32
# Output: Copy this to ENCRYPTION_KEY

# Generate strong database password
openssl rand -base64 24 | tr -d "=+/" | cut -c1-20
# Output: Copy this to database password
```

### **2. Configure Environment Files**

#### **Development (.env)**
```bash
cp .env.example .env
# Edit .env with development values
```

#### **Staging (appsettings.Staging.json)**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=staging-db;Database=BankingStaging;...",
    "Redis": "staging-redis:6379"
  },
  "JwtSettings": {
    "Secret": "[FROM-SECURE-STORAGE]"
  }
}
```

#### **Production (appsettings.Production.json)**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=prod-db;Database=BankingProd;SSL Mode=Require;...",
    "Redis": "prod-redis:6379,ssl=true"
  },
  "JwtSettings": {
    "Secret": "[FROM-AZURE-KEY-VAULT]"
  }
}
```

---

## ?? **DOCKER DEPLOYMENT**

### **Option 1: Docker Compose (Simple)**

```powershell
# 1. Build production image
docker build -t banking-api:v1.0.0 `
  -f src/BankingSystem.API/Dockerfile `
  --build-arg BUILD_VERSION=1.0.0 `
  .

# 2. Tag for registry
docker tag banking-api:v1.0.0 your-registry.azurecr.io/banking-api:v1.0.0

# 3. Push to registry
docker push your-registry.azurecr.io/banking-api:v1.0.0

# 4. Deploy
docker-compose -f docker-compose.prod.yml up -d
```

### **Option 2: Docker Swarm (Production)**

```powershell
# 1. Initialize swarm
docker swarm init

# 2. Create secrets
echo "your-jwt-secret" | docker secret create jwt_secret -
echo "your-encryption-key" | docker secret create encryption_key -
echo "your-db-password" | docker secret create db_password -

# 3. Deploy stack
docker stack deploy -c docker-compose.swarm.yml banking-system

# 4. Monitor
docker stack services banking-system
docker service logs banking-system_api
```

---

## ?? **KUBERNETES DEPLOYMENT**

### **Prerequisites**
```powershell
# Install kubectl
# Install helm (optional)
```

### **1. Create Namespace**
```yaml
# k8s/namespace.yml
apiVersion: v1
kind: Namespace
metadata:
  name: banking-system
```

```powershell
kubectl apply -f k8s/namespace.yml
```

### **2. Create Secrets**
```powershell
# Create from literals
kubectl create secret generic banking-secrets `
  --from-literal=jwt-secret=your-secret `
  --from-literal=encryption-key=your-key `
  --from-literal=db-password=your-password `
  -n banking-system

# Or from file
kubectl create secret generic banking-secrets `
  --from-env-file=.env.production `
  -n banking-system
```

### **3. Deploy Application**
```powershell
# Apply all manifests
kubectl apply -f k8s/ -n banking-system

# Check status
kubectl get pods -n banking-system
kubectl get services -n banking-system

# View logs
kubectl logs -f deployment/banking-api -n banking-system
```

### **4. Scale Application**
```powershell
# Manual scaling
kubectl scale deployment banking-api --replicas=3 -n banking-system

# Autoscaling
kubectl autoscale deployment banking-api `
  --min=2 --max=10 `
  --cpu-percent=70 `
  -n banking-system
```

---

## ?? **AZURE DEPLOYMENT**

> **?? For detailed Vietnamese guide, see:** [docs/AZURE-DEPLOYMENT-VI.md](./AZURE-DEPLOYMENT-VI.md)

### **Quick Start: Automated Deployment with Bicep**

The fastest way to deploy to Azure using Infrastructure as Code:

```powershell
# Deploy complete infrastructure to development
.\azure\scripts\deploy.ps1 -Environment dev

# Deploy to production with specific image tag
.\azure\scripts\deploy.ps1 -Environment prod -ImageTag v1.0.0

# Deploy only infrastructure (skip image build)
.\azure\scripts\deploy.ps1 -Environment staging -SkipImageBuild

# Deploy only new image (skip infrastructure)
.\azure\scripts\deploy.ps1 -Environment prod -SkipInfrastructure -ImageTag v1.0.1
```

**What gets deployed:**
- ? Azure Container Registry
- ? PostgreSQL Flexible Servers (Business + Hangfire)
- ? Azure Cache for Redis
- ? Azure Key Vault (with secrets)
- ? Log Analytics Workspace
- ? Application Insights
- ? Container Apps Environment
- ? Banking API Container App

**Deployment time:** ~15-20 minutes

See [azure/scripts/README.md](../azure/scripts/README.md) for detailed script documentation.

---

### **Manual Deployment Options**

### **Option 1: Azure Container Apps (Recommended)**

```powershell
# 1. Login to Azure
az login

# 2. Create resource group
az group create `
  --name rg-banking-prod `
  --location eastus

# 3. Create Container Registry
az acr create `
  --resource-group rg-banking-prod `
  --name bankingcr `
  --sku Standard

# 4. Build and push image
az acr build `
  --registry bankingcr `
  --image banking-api:v1.0.0 `
  --file src/BankingSystem.API/Dockerfile `
  .

# 5. Create Container App Environment
az containerapp env create `
  --name banking-env `
  --resource-group rg-banking-prod `
  --location eastus

# 6. Create Container App
az containerapp create `
  --name banking-api `
  --resource-group rg-banking-prod `
  --environment banking-env `
  --image bankingcr.azurecr.io/banking-api:v1.0.0 `
  --target-port 8080 `
  --ingress external `
  --min-replicas 2 `
  --max-replicas 10 `
  --cpu 1.0 `
  --memory 2.0Gi `
  --env-vars `
    ASPNETCORE_ENVIRONMENT=Production `
    ConnectionStrings__DefaultConnection=secretref:db-connection `
    JwtSettings__Secret=secretref:jwt-secret

# 7. Set secrets
az containerapp secret set `
  --name banking-api `
  --resource-group rg-banking-prod `
  --secrets `
    db-connection="Host=..." `
    jwt-secret="your-secret"
```

### **Option 2: Azure App Service**

```powershell
# 1. Create App Service Plan
az appservice plan create `
  --name banking-plan `
  --resource-group rg-banking-prod `
  --is-linux `
  --sku P1V2

# 2. Create Web App
az webapp create `
  --name banking-api `
  --resource-group rg-banking-prod `
  --plan banking-plan `
  --deployment-container-image-name bankingcr.azurecr.io/banking-api:v1.0.0

# 3. Configure app settings
az webapp config appsettings set `
  --name banking-api `
  --resource-group rg-banking-prod `
  --settings `
    ASPNETCORE_ENVIRONMENT=Production `
    ConnectionStrings__DefaultConnection="@Microsoft.KeyVault(SecretUri=...)" `
    JwtSettings__Secret="@Microsoft.KeyVault(SecretUri=...)"

# 4. Enable CI/CD
az webapp deployment container config `
  --name banking-api `
  --resource-group rg-banking-prod `
  --enable-cd true
```

### **3. Azure Database for PostgreSQL**

```powershell
# Create PostgreSQL server
az postgres flexible-server create `
  --name banking-db `
  --resource-group rg-banking-prod `
  --location eastus `
  --admin-user bankingadmin `
  --admin-password "your-secure-password" `
  --sku-name Standard_B2s `
  --tier Burstable `
  --storage-size 32 `
  --version 16

# Create database
az postgres flexible-server db create `
  --resource-group rg-banking-prod `
  --server-name banking-db `
  --database-name BankingSystemProd

# Configure firewall
az postgres flexible-server firewall-rule create `
  --resource-group rg-banking-prod `
  --name banking-db `
  --rule-name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0
```

### **4. Azure Key Vault**

```powershell
# Create Key Vault
az keyvault create `
  --name kv-banking-prod `
  --resource-group rg-banking-prod `
  --location eastus

# Add secrets
az keyvault secret set `
  --vault-name kv-banking-prod `
  --name jwt-secret `
  --value "your-secret"

az keyvault secret set `
  --vault-name kv-banking-prod `
  --name encryption-key `
  --value "your-key"

az keyvault secret set `
  --vault-name kv-banking-prod `
  --name db-connection `
  --value "Host=banking-db.postgres.database.azure.com;..."

# Grant Container App access
az containerapp identity assign `
  --name banking-api `
  --resource-group rg-banking-prod `
  --system-assigned

# Get principal ID and assign to Key Vault
$principalId = az containerapp identity show `
  --name banking-api `
  --resource-group rg-banking-prod `
  --query principalId -o tsv

az keyvault set-policy `
  --name kv-banking-prod `
  --object-id $principalId `
  --secret-permissions get list
```

### **5. Azure Cache for Redis**

```powershell
# Create Redis Cache
az redis create `
  --resource-group rg-banking-prod `
  --name banking-redis-prod `
  --location eastus `
  --sku Standard `
  --vm-size C1 `
  --enable-non-ssl-port false `
  --minimum-tls-version 1.2

# Get connection string
$redisHost = az redis show `
  --resource-group rg-banking-prod `
  --name banking-redis-prod `
  --query hostName -o tsv

$redisKey = az redis list-keys `
  --resource-group rg-banking-prod `
  --name banking-redis-prod `
  --query primaryKey -o tsv

# Connection string format:
# {redisHost}:6380,password={redisKey},ssl=True,abortConnect=False
```

**SKU Options:**
- **Basic C0** ($16/month) - Development/Testing, 250MB
- **Standard C1** ($62/month) - Production, 1GB with HA replication
- **Premium P1** ($321/month) - Enterprise, 6GB with clustering

---

## ?? **AZURE COST ESTIMATION**

### **Development Environment (~$93/month)**

| Service | SKU | Cost/Month |
|---------|-----|------------|
| Container Apps | Consumption (2 replicas avg) | $25 |
| PostgreSQL Business | Burstable B1ms | $12 |
| PostgreSQL Hangfire | Burstable B1ms | $12 |
| Redis Cache | Basic C0 | $16 |
| Key Vault | Standard | $3 |
| Container Registry | Standard | $20 |
| Application Insights | Pay-as-you-go | $5 |
| **Total** | | **~$93** |

### **Production Environment (~$362/month)**

| Service | SKU | Cost/Month |
|---------|-----|------------|
| Container Apps | Consumption (3 replicas avg) | $75 |
| PostgreSQL Business | General Purpose D2s_v3 | $120 |
| PostgreSQL Hangfire | Burstable B2s | $30 |
| Redis Cache | Standard C1 | $62 |
| Key Vault | Standard | $5 |
| Container Registry | Standard | $20 |
| Application Insights | Pay-as-you-go | $25 |
| Log Analytics | Pay-as-you-go | $15 |
| Backup & Storage | | $10 |
| **Total** | | **~$362** |

### **Cost Optimization Tips:**

1. **Reserved Instances** - Save 30-50% on PostgreSQL with 1-3 year commitment
2. **Auto-scaling** - Container Apps scale down when idle
3. **Dev/Test Subscriptions** - Get discounted pricing for non-production
4. **Delete unused environments** - Teardown dev/staging when not in use
5. **Monitor costs** - Set budget alerts via Azure Cost Management

```powershell
# Set budget alert
az consumption budget create `
  --budget-name banking-monthly `
  --amount 500 `
  --resource-group rg-banking-prod `
  --time-grain Monthly `
  --start-date 2025-01-01
```

See [docs/AZURE-DEPLOYMENT-VI.md](./AZURE-DEPLOYMENT-VI.md) for detailed cost breakdown.

---

## ?? **DATABASE MIGRATION**

### **Method 1: Automatic (EF Core)**
```powershell
# Connection string in environment variable
$env:ConnectionStrings__DefaultConnection = "Host=prod-db;..."

# Apply migrations
dotnet ef database update `
  --project src/BankingSystem.Infrastructure `
  --startup-project src/BankingSystem.API `
  --configuration Production
```

### **Method 2: SQL Script (Recommended for Production)**
```powershell
# 1. Generate SQL script
dotnet ef migrations script `
  --project src/BankingSystem.Infrastructure `
  --startup-project src/BankingSystem.API `
  --output migration.sql `
  --idempotent

# 2. Review script manually

# 3. Execute script
psql -h prod-db.postgres.database.azure.com `
  -U bankingadmin `
  -d BankingSystemProd `
  -f migration.sql
```

---

## ?? **SECURITY HARDENING**

### **1. Enable HTTPS**
```json
// appsettings.Production.json
{
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://*:443",
        "Certificate": {
          "Path": "/app/cert.pfx",
          "Password": "[FROM-KEY-VAULT]"
        }
      }
    }
  }
}
```

### **2. Restrict CORS**
```json
{
  "CorsSettings": {
    "AllowedOrigins": [
      "https://banking.yourdomain.com"
    ]
  }
}
```

### **3. Enable Rate Limiting**
```json
{
  "RateLimiting": {
    "Enabled": true,
    "RequestsPerMinute": 100
  }
}
```

---

## ?? **MONITORING SETUP**

### **1. Application Insights (Azure)**
```powershell
# Create Application Insights
az monitor app-insights component create `
  --app banking-api-insights `
  --location eastus `
  --resource-group rg-banking-prod

# Get instrumentation key
$instrumentationKey = az monitor app-insights component show `
  --app banking-api-insights `
  --resource-group rg-banking-prod `
  --query instrumentationKey -o tsv

# Add to app settings
az containerapp update `
  --name banking-api `
  --resource-group rg-banking-prod `
  --set-env-vars `
    APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$instrumentationKey"
```

### **2. Configure Alerts**
```powershell
# Create action group for notifications
az monitor action-group create `
  --name banking-alerts `
  --resource-group rg-banking-prod `
  --short-name BankAlert `
  --email-receiver name=admin email=admin@company.com

# Create metric alert
az monitor metrics alert create `
  --name high-error-rate `
  --resource-group rg-banking-prod `
  --scopes /subscriptions/.../banking-api `
  --condition "avg requests/failed > 10" `
  --window-size 5m `
  --evaluation-frequency 1m `
  --action banking-alerts
```

---

## ?? **DEPLOYMENT STRATEGIES**

### **Blue-Green Deployment**
```powershell
# 1. Deploy to "green" slot
az containerapp revision copy `
  --name banking-api `
  --resource-group rg-banking-prod `
  --from-revision banking-api--blue `
  --image bankingcr.azurecr.io/banking-api:v2.0.0

# 2. Test green environment
curl https://banking-api--green.azurecontainerapps.io/health

# 3. Switch traffic
az containerapp ingress traffic set `
  --name banking-api `
  --resource-group rg-banking-prod `
  --revision-weight banking-api--green=100 banking-api--blue=0

# 4. If issues, rollback
az containerapp ingress traffic set `
  --name banking-api `
  --resource-group rg-banking-prod `
  --revision-weight banking-api--green=0 banking-api--blue=100
```

### **Canary Deployment**
```powershell
# 1. Deploy new version
# 2. Route 10% traffic to new version
az containerapp ingress traffic set `
  --name banking-api `
  --resource-group rg-banking-prod `
  --revision-weight banking-api--v2=10 banking-api--v1=90

# 3. Monitor metrics
# 4. Gradually increase traffic
az containerapp ingress traffic set `
  --name banking-api `
  --resource-group rg-banking-prod `
  --revision-weight banking-api--v2=50 banking-api--v1=50

# 5. Complete rollout
az containerapp ingress traffic set `
  --name banking-api `
  --resource-group rg-banking-prod `
  --revision-weight banking-api--v2=100 banking-api--v1=0
```

---

## ?? **ROLLBACK PROCEDURES**

### **Quick Rollback**
```powershell
# Docker
docker service update --rollback banking-system_api

# Kubernetes
kubectl rollout undo deployment/banking-api -n banking-system

# Azure Container Apps
az containerapp revision set-mode `
  --name banking-api `
  --resource-group rg-banking-prod `
  --mode single

az containerapp revision activate `
  --name banking-api `
  --resource-group rg-banking-prod `
  --revision banking-api--previous-revision
```

### **Database Rollback**
```powershell
# 1. Stop application
# 2. Restore database backup
# 3. Restart application with previous version
```

---

## ? **POST-DEPLOYMENT CHECKLIST**

- [ ] Health check endpoint returns 200
- [ ] Swagger UI accessible (if enabled)
- [ ] Database migrations applied
- [ ] All environment variables set
- [ ] Monitoring dashboards showing data
- [ ] Alerts configured
- [ ] SSL certificate valid
- [ ] Logs flowing to centralized logging
- [ ] Backup job running
- [ ] Performance within acceptable range
- [ ] Security scan passed
- [ ] Smoke tests passed

---

## ?? **TROUBLESHOOTING**

### **Container won't start**
```powershell
# Check logs
kubectl logs pod/banking-api-xxx -n banking-system
docker logs container-id

# Check events
kubectl describe pod/banking-api-xxx -n banking-system

# Check configuration
kubectl get configmap -n banking-system
kubectl get secret -n banking-system
```

### **Database connection issues**
```powershell
# Test connection
psql -h your-db-host -U admin -d BankingSystemProd

# Check firewall
az postgres flexible-server firewall-rule list `
  --resource-group rg-banking-prod `
  --name banking-db

# Check connection string
# Verify in Key Vault or environment variables
```

### **Performance issues**
```powershell
# Scale up
kubectl scale deployment banking-api --replicas=5 -n banking-system

# Check metrics
kubectl top pods -n banking-system

# Check database performance
# Run EXPLAIN ANALYZE on slow queries
```

---

## ?? **SUPPORT**

For deployment issues:
1. Check logs in Seq: http://seq-url:5341
2. Check Application Insights
3. Review Grafana dashboards
4. Contact DevOps team

---

**Remember:**
- Always test in staging first
- Have a rollback plan
- Monitor during and after deployment
- Keep secrets secure
- Document any issues

?? **Happy Deploying!**
