# Azure Deployment Scripts

PowerShell scripts for automating Banking System deployment to Azure.

## ?? Files

- **deploy.ps1** - Main deployment script (infrastructure + application)
- **teardown.ps1** - Cleanup script to delete all resources

## ?? Quick Start

### Deploy to Development

```powershell
.\azure\scripts\deploy.ps1 -Environment dev
```

### Deploy to Production

```powershell
.\azure\scripts\deploy.ps1 -Environment prod -ImageTag v1.0.0
```

### Delete Environment

```powershell
.\azure\scripts\teardown.ps1 -Environment dev
```

## ?? deploy.ps1 Usage

### Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `-Environment` | Yes | - | Environment: `dev`, `staging`, or `prod` |
| `-ResourceGroupName` | No | `rg-banking-{env}` | Azure resource group name |
| `-Location` | No | `southeastasia` | Azure region |
| `-SubscriptionId` | No | Current | Azure subscription ID |
| `-SkipInfrastructure` | No | false | Skip Bicep deployment |
| `-SkipImageBuild` | No | false | Skip Docker image build |
| `-ImageTag` | No | `latest` | Docker image tag |

### Examples

**Full deployment (first time):**
```powershell
.\azure\scripts\deploy.ps1 `
    -Environment prod `
    -Location southeastasia `
    -ImageTag v1.0.0
```

**Deploy only new Docker image:**
```powershell
.\azure\scripts\deploy.ps1 `
    -Environment prod `
    -SkipInfrastructure `
    -ImageTag v1.0.1
```

**Deploy infrastructure only:**
```powershell
.\azure\scripts\deploy.ps1 `
    -Environment staging `
    -SkipImageBuild
```

**Use specific subscription:**
```powershell
.\azure\scripts\deploy.ps1 `
    -Environment prod `
    -SubscriptionId "12345678-1234-1234-1234-123456789012"
```

## ??? teardown.ps1 Usage

### Parameters

| Parameter | Required | Default | Description |
|-----------|----------|---------|-------------|
| `-Environment` | Yes | - | Environment to delete |
| `-ResourceGroupName` | No | `rg-banking-{env}` | Resource group to delete |
| `-Force` | No | false | Skip confirmation, wait for completion |

### Examples

**Delete with confirmation:**
```powershell
.\azure\scripts\teardown.ps1 -Environment dev
```

**Force delete (no confirmation, wait for completion):**
```powershell
.\azure\scripts\teardown.ps1 -Environment dev -Force
```

**Delete specific resource group:**
```powershell
.\azure\scripts\teardown.ps1 `
    -Environment staging `
    -ResourceGroupName "my-custom-rg" `
    -Force
```

## ?? Secrets Management

The deployment script generates secure secrets automatically:
- PostgreSQL admin password
- JWT secret (64 characters)
- Encryption key (32 bytes base64)

**Important:** Save these secrets when prompted! They are stored in Azure Key Vault.

## ?? What Gets Deployed?

The deployment script creates:

1. **Container Registry** - Docker images storage
2. **PostgreSQL Flexible Servers** (2x)
   - Business database
   - Hangfire database
3. **Redis Cache** - Caching layer
4. **Key Vault** - Secrets management
5. **Log Analytics Workspace** - Logging
6. **Application Insights** - APM
7. **Container Apps Environment** - Hosting environment
8. **Container App** - Banking API

## ?? Deployment Time

- **First deployment:** ~15-20 minutes
- **Image-only update:** ~5-10 minutes
- **Teardown:** ~5-10 minutes

## ?? Deployment Flow

```
1. ? Check Azure CLI
2. ? Login to Azure
3. ? Create Resource Group
4. ? Generate Secrets
5. ? Deploy Infrastructure (Bicep)
   - Container Registry
   - PostgreSQL Servers
   - Redis Cache
   - Key Vault
   - Monitoring
   - Container Apps
6. ? Build & Push Docker Image
7. ? Show Deployment Summary
```

## ?? Troubleshooting

### Error: "Azure CLI is not installed"

```powershell
# Install Azure CLI
winget install Microsoft.AzureCLI
```

### Error: "Not logged in"

```powershell
# Login to Azure
az login
```

### Error: "Deployment failed"

```powershell
# Check deployment logs
az deployment group list --resource-group rg-banking-dev --output table

# Show deployment details
az deployment group show \
    --name <deployment-name> \
    --resource-group rg-banking-dev \
    --query properties.error
```

### Error: "Image build failed"

```powershell
# Build locally instead
docker build -t banking-api:latest -f src/BankingSystem.API/Dockerfile .

# Push to ACR
az acr login --name bankingcr
docker tag banking-api:latest bankingcr.azurecr.io/banking-api:latest
docker push bankingcr.azurecr.io/banking-api:latest
```

## ?? Cost Estimation

**Development Environment:**
- ~$93/month

**Production Environment:**
- Small: ~$362/month
- Medium: ~$735/month

See `docs/AZURE-DEPLOYMENT-VI.md` for detailed cost breakdown.

## ?? Related Documentation

- [Azure Deployment Guide (VI)](../../docs/AZURE-DEPLOYMENT-VI.md)
- [Deployment Guide](../../docs/DEPLOYMENT-GUIDE.md)
- [Bicep Templates](../bicep/)

## ?? Tips

1. **Use Azure Cost Management** to track spending
2. **Tag resources** for cost tracking: `Environment`, `Project`
3. **Set budget alerts** to avoid surprises
4. **Delete dev/staging** environments when not in use
5. **Use Azure DevTest subscriptions** for lower pricing
6. **Enable Auto-shutdown** for non-prod environments

## ?? Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review Azure deployment logs
3. Check `docs/AZURE-DEPLOYMENT-VI.md`
4. Create an issue on GitHub
