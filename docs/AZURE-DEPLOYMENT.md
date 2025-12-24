# Azure Deployment Guide

Complete guide for deploying the Banking System to Microsoft Azure using Container Apps.

---

## Overview

This guide covers deploying a production-ready .NET 8 banking application to Azure using:

- **Azure Container Apps** - Serverless containers with auto-scaling
- **PostgreSQL Flexible Server** - Managed database (Business + Hangfire)
- **Azure Cache for Redis** - Distributed caching
- **Azure Key Vault** - Secrets management
- **Application Insights** - Application monitoring
- **Container Registry** - Docker image storage (or GitHub Container Registry)

**Deployment time:** ~15-20 minutes  
**Estimated cost:** $93/month (dev) | $362/month (prod)

---

## Prerequisites

**Local environment:**
```bash
# Azure CLI
az --version

# .NET 8 SDK
dotnet --version

# Docker (optional)
docker --version
```

**Azure requirements:**
- Active Azure subscription
- Contributor or Owner role
- Budget: $50-150/month

---

## Architecture

```
Internet
    ?
    ??> Azure Container Apps (API)
    ?       ??> PostgreSQL (Business)
    ?       ??> PostgreSQL (Hangfire)
    ?       ??> Redis Cache
    ?       ??> Key Vault (Secrets)
    ?
    ??> Application Insights (Monitoring)
```

---

## Quick Start

### 1. Login and Setup

```bash
# Login
az login
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# Create resource group
az group create \
  --name rg-banking-prod \
  --location southeastasia \
  --tags Environment=Production Project=BankingSystem
```

### 2. Generate Secrets

```bash
# JWT Secret (64 characters)
openssl rand -base64 64

# Encryption Key (32 bytes)
openssl rand -base64 32

# Database Password
openssl rand -base64 24 | tr -d "=+/" | cut -c1-20
```

**Save these values securely - you'll need them later.**

---

## Deployment Steps

### Step 1: Container Registry

**Option A: Use GitHub Container Registry (GHCR)** - Recommended, no Azure setup needed

Already configured in `.github/workflows/cd.yml`. Skip to Step 2.

**Option B: Use Azure Container Registry**

```bash
# Create ACR
az acr create \
  --resource-group rg-banking-prod \
  --name bankingcr \
  --sku Standard \
  --admin-enabled true

# Build and push
az acr build \
  --registry bankingcr \
  --image banking-api:latest \
  --file src/BankingSystem.API/Dockerfile \
  .
```

---

### Step 2: PostgreSQL Database

```bash
# Business database
az postgres flexible-server create \
  --resource-group rg-banking-prod \
  --name banking-db-prod \
  --location southeastasia \
  --admin-user bankingadmin \
  --admin-password "YOUR_DB_PASSWORD" \
  --sku-name Standard_B2s \
  --tier Burstable \
  --storage-size 32 \
  --version 16 \
  --public-access 0.0.0.0

az postgres flexible-server db create \
  --resource-group rg-banking-prod \
  --server-name banking-db-prod \
  --database-name BankingSystemDb

# Hangfire database
az postgres flexible-server create \
  --resource-group rg-banking-prod \
  --name banking-hangfire-prod \
  --location southeastasia \
  --admin-user hangfireadmin \
  --admin-password "YOUR_DB_PASSWORD" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16 \
  --public-access 0.0.0.0

az postgres flexible-server db create \
  --resource-group rg-banking-prod \
  --server-name banking-hangfire-prod \
  --database-name BankingSystemHangfire

# Allow Azure services
az postgres flexible-server firewall-rule create \
  --resource-group rg-banking-prod \
  --name banking-db-prod \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

**Connection strings (save these):**
```
Business: Host=banking-db-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemDb;Username=bankingadmin;Password=YOUR_PASSWORD;SSL Mode=Require

Hangfire: Host=banking-hangfire-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemHangfire;Username=hangfireadmin;Password=YOUR_PASSWORD;SSL Mode=Require
```

---

### Step 3: Redis Cache

```bash
# Create Redis
az redis create \
  --resource-group rg-banking-prod \
  --name banking-redis-prod \
  --location southeastasia \
  --sku Basic \
  --vm-size c0 \
  --enable-non-ssl-port false \
  --minimum-tls-version 1.2

# Get connection details
az redis show \
  --resource-group rg-banking-prod \
  --name banking-redis-prod \
  --query "[hostName,sslPort]" \
  --output tsv

az redis list-keys \
  --resource-group rg-banking-prod \
  --name banking-redis-prod \
  --query primaryKey \
  --output tsv
```

**Connection string format:**
```
banking-redis-prod.redis.cache.windows.net:6380,password=YOUR_KEY,ssl=True,abortConnect=False
```

---

### Step 4: Key Vault

```bash
# Create Key Vault
az keyvault create \
  --resource-group rg-banking-prod \
  --name kv-banking-prod \
  --location southeastasia \
  --sku standard

# Add secrets
az keyvault secret set \
  --vault-name kv-banking-prod \
  --name jwt-secret \
  --value "YOUR_JWT_SECRET"

az keyvault secret set \
  --vault-name kv-banking-prod \
  --name encryption-key \
  --value "YOUR_ENCRYPTION_KEY"

az keyvault secret set \
  --vault-name kv-banking-prod \
  --name db-business-connection \
  --value "Host=banking-db-prod.postgres.database.azure.com;..."

az keyvault secret set \
  --vault-name kv-banking-prod \
  --name db-hangfire-connection \
  --value "Host=banking-hangfire-prod.postgres.database.azure.com;..."

az keyvault secret set \
  --vault-name kv-banking-prod \
  --name redis-connection \
  --value "banking-redis-prod.redis.cache.windows.net:6380,..."
```

---

### Step 5: Container Apps Environment

```bash
# Create environment
az containerapp env create \
  --resource-group rg-banking-prod \
  --name banking-env-prod \
  --location southeastasia
```

**Optional: Add Log Analytics for logging**

```bash
# Create workspace
az monitor log-analytics workspace create \
  --resource-group rg-banking-prod \
  --workspace-name banking-logs \
  --location southeastasia

# Get workspace details
workspaceId=$(az monitor log-analytics workspace show \
  --resource-group rg-banking-prod \
  --workspace-name banking-logs \
  --query customerId \
  --output tsv)

workspaceKey=$(az monitor log-analytics workspace get-shared-keys \
  --resource-group rg-banking-prod \
  --workspace-name banking-logs \
  --query primarySharedKey \
  --output tsv)

# Recreate environment with logging
az containerapp env create \
  --resource-group rg-banking-prod \
  --name banking-env-prod \
  --location southeastasia \
  --logs-workspace-id $workspaceId \
  --logs-workspace-key $workspaceKey
```

---

### Step 6: Deploy Container App

```bash
# Deploy (using GHCR image)
az containerapp create \
  --resource-group rg-banking-prod \
  --name banking-api \
  --environment banking-env-prod \
  --image ghcr.io/YOUR_GITHUB_USERNAME/banking-api:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 2 \
  --max-replicas 10 \
  --cpu 1.0 \
  --memory 2.0Gi

# Get app URL
az containerapp show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query properties.configuration.ingress.fqdn \
  --output tsv
```

---

### Step 7: Configure Secrets and Environment

```bash
# Enable Managed Identity
az containerapp identity assign \
  --resource-group rg-banking-prod \
  --name banking-api \
  --system-assigned

# Get principal ID
principalId=$(az containerapp identity show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query principalId \
  --output tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name kv-banking-prod \
  --object-id $principalId \
  --secret-permissions get list

# Get secret URIs
jwtSecretUri=$(az keyvault secret show \
  --vault-name kv-banking-prod \
  --name jwt-secret \
  --query id \
  --output tsv)

dbBusinessUri=$(az keyvault secret show \
  --vault-name kv-banking-prod \
  --name db-business-connection \
  --query id \
  --output tsv)

# Configure secrets
az containerapp secret set \
  --resource-group rg-banking-prod \
  --name banking-api \
  --secrets \
    jwt-secret="keyvaultref:$jwtSecretUri,identityref:system" \
    db-connection="keyvaultref:$dbBusinessUri,identityref:system"

# Set environment variables
az containerapp update \
  --resource-group rg-banking-prod \
  --name banking-api \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__DefaultConnection=secretref:db-connection \
    JwtSettings__Secret=secretref:jwt-secret \
    RateLimitSettings__PermitLimit=1000 \
    RateLimitSettings__WindowInSeconds=60
```

---

### Step 8: Database Migrations

**Option A: Automatic** (configured in `Program.cs`)

Migrations run automatically on container startup.

**Option B: Manual** (recommended for production)

```bash
# Generate migration script
dotnet ef migrations script \
  --project src/BankingSystem.Infrastructure \
  --startup-project src/BankingSystem.API \
  --output migration.sql \
  --idempotent

# Apply migrations
psql "Host=banking-db-prod.postgres.database.azure.com;..." -f migration.sql
```

---

### Step 9: Verify Deployment

```bash
# Get app URL
appUrl=$(az containerapp show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query properties.configuration.ingress.fqdn \
  --output tsv)

# Test health endpoint
curl https://$appUrl/health

# View logs
az containerapp logs show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --tail 50
```

---

## Monitoring Setup

### Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --resource-group rg-banking-prod \
  --app banking-api-insights \
  --location southeastasia

# Get connection string
connectionString=$(az monitor app-insights component show \
  --resource-group rg-banking-prod \
  --app banking-api-insights \
  --query connectionString \
  --output tsv)

# Add to Container App
az containerapp update \
  --resource-group rg-banking-prod \
  --name banking-api \
  --set-env-vars \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$connectionString"
```

### Alerts

```bash
# Create action group
az monitor action-group create \
  --resource-group rg-banking-prod \
  --name banking-alerts \
  --short-name BankAlert \
  --email-receiver name=admin email=admin@example.com

# Create alert rule (high error rate)
az monitor metrics alert create \
  --resource-group rg-banking-prod \
  --name high-error-rate \
  --condition "avg Replicas > 0" \
  --window-size 5m \
  --action banking-alerts
```

---

## Cost Estimation

### Development Environment (~$93/month)

| Service | SKU | Cost |
|---------|-----|------|
| Container Apps | Consumption (2 replicas) | $25 |
| PostgreSQL Business | Burstable B1ms | $12 |
| PostgreSQL Hangfire | Burstable B1ms | $12 |
| Redis Cache | Basic C0 | $16 |
| Key Vault | Standard | $3 |
| Container Registry | Standard | $20 |
| Application Insights | Pay-as-you-go | $5 |
| **Total** | | **~$93** |

### Production Environment (~$362/month)

| Service | SKU | Cost |
|---------|-----|------|
| Container Apps | Consumption (3 replicas) | $75 |
| PostgreSQL Business | General Purpose D2s_v3 | $120 |
| PostgreSQL Hangfire | Burstable B2s | $30 |
| Redis Cache | Standard C1 | $62 |
| Key Vault | Standard | $5 |
| Container Registry | Standard | $20 |
| Application Insights | Pay-as-you-go | $25 |
| Log Analytics | Pay-as-you-go | $15 |
| Backup & Storage | | $10 |
| **Total** | | **~$362** |

**Cost optimization:**
- Use Reserved Instances for PostgreSQL (30-50% savings)
- Auto-scale Container Apps down during off-hours
- Delete unused development environments
- Set budget alerts

```bash
az consumption budget create \
  --budget-name banking-monthly \
  --amount 500 \
  --resource-group rg-banking-prod \
  --time-grain Monthly
```

---

## Troubleshooting

### Container not starting

```bash
# Check logs
az containerapp logs show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --tail 100

# Check revisions
az containerapp revision list \
  --resource-group rg-banking-prod \
  --name banking-api
```

### Database connection failed

```bash
# Test connection
psql "Host=banking-db-prod.postgres.database.azure.com;..." -c "\l"

# Check firewall
az postgres flexible-server firewall-rule list \
  --resource-group rg-banking-prod \
  --name banking-db-prod
```

### Secrets not accessible

```bash
# Verify Managed Identity
az containerapp identity show \
  --resource-group rg-banking-prod \
  --name banking-api

# Check Key Vault access
az keyvault show \
  --name kv-banking-prod \
  --query properties.accessPolicies
```

---

## CI/CD Integration

The project includes GitHub Actions workflows (`.github/workflows/cd.yml`) for automated deployment.

**Setup:**
1. Add Azure credentials to GitHub Secrets
2. Push to main branch or create a tag
3. Workflow automatically builds and deploys

See `WORKFLOW-ARCHITECTURE.md` for details.

---

## Security Best Practices

- All secrets in Key Vault (no hardcoded values)
- Managed Identity for authentication
- HTTPS enforced (TLS 1.2+)
- Database firewall rules (Azure services only)
- Rate limiting enabled (1000 req/min)
- CORS restricted to specific origins
- Regular security updates

---

## Next Steps

After successful deployment:

1. Test all API endpoints
2. Configure custom domain and SSL
3. Set up automated backups
4. Configure multi-region deployment (if needed)
5. Implement CI/CD automation
6. Monitor performance and optimize

---

## Resources

- [Azure Container Apps Docs](https://learn.microsoft.com/azure/container-apps/)
- [Azure PostgreSQL Docs](https://learn.microsoft.com/azure/postgresql/)
- [Azure Key Vault Docs](https://learn.microsoft.com/azure/key-vault/)
- [Application Insights Docs](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)

---

**Deployment complete! Your banking system is now running on Azure.**

*Last updated: December 2025*
