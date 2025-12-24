# Azure-Specific App Settings

Configuration files optimized for Azure deployment.

## ?? Files

- **appsettings.Azure.Development.json** - Development configuration
- **appsettings.Azure.Production.json** - Production configuration

## ?? Usage

These files are **reference configurations** showing recommended settings for Azure environments. 

**Note:** Most values are injected via Azure Container Apps environment variables or Azure Key Vault secrets.

### Copy to Project (Optional)

If you want to use these configurations in your project:

```powershell
# Copy to API project
Copy-Item azure/appsettings/*.json src/BankingSystem.API/
```

## ?? Secrets Management

**DO NOT** put secrets directly in these files. Use Azure Key Vault instead.

### Connection Strings
Injected via Container App environment variables:
- `ConnectionStrings__DefaultConnection` ? from Key Vault secret `db-business-connection`
- `ConnectionStrings__HangfireConnection` ? from Key Vault secret `db-hangfire-connection`
- `ConnectionStrings__Redis` ? from Key Vault secret `redis-connection`

### JWT & Encryption
Injected via Container App environment variables:
- `JwtSettings__Secret` ? from Key Vault secret `jwt-secret`
- `EncryptionSettings__Key` ? from Key Vault secret `encryption-key`

## ?? Configuration Differences

### Development vs Production

| Setting | Development | Production | Reason |
|---------|-------------|------------|--------|
| **Logging Level** | Information | Warning | Reduce log volume |
| **Rate Limit** | 5000/min | 1000/min | Protect resources |
| **Worker Count** | 10 | 20 | Handle more load |
| **CORS Origins** | localhost + dev site | Production domains only | Security |
| **Swagger** | Enabled | Disabled | Security |

## ?? Key Features

### Logging with Serilog
- Console output for Azure Container Apps logs
- Application Insights integration
- Structured logging with correlation IDs

### Rate Limiting
```json
"RateLimitSettings": {
  "Enabled": true,
  "PermitLimit": 1000,
  "WindowInSeconds": 60
}
```

### Health Checks
```json
"HealthChecks": {
  "Enabled": true,
  "PostgreSQL": { "Timeout": 10 },
  "Redis": { "Timeout": 10 },
  "Hangfire": { "Timeout": 15 }
}
```

### Hangfire Configuration
```json
"Hangfire": {
  "ServerName": "banking-prod-api",
  "WorkerCount": 20,
  "Queues": ["critical", "default", "background"]
}
```

## ?? How Values Are Injected

Values are set via Azure Container Apps:

```bicep
environmentVariables: [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Production'
  },
  {
    name: 'ConnectionStrings__DefaultConnection'
    secretRef: 'db-business-connection'
  },
  {
    name: 'JwtSettings__Secret'
    secretRef: 'jwt-secret'
  }
]
```

Secrets reference Azure Key Vault:

```bicep
secrets: [
  {
    name: 'jwt-secret'
    keyVaultUrl: '${keyVault.vaultUri}secrets/jwt-secret'
  }
]
```

## ?? Customization

To customize for your environment:

1. Update CORS origins
2. Adjust rate limits
3. Configure Hangfire workers
4. Set logging levels
5. Update health check timeouts

## ?? Related Documentation

- [Azure Deployment Guide](../../docs/AZURE-DEPLOYMENT-VI.md)
- [Rate Limiting Guide](../../docs/RATE-LIMITING-CONFIG.md)
- [Monitoring Guide](../../docs/MONITORING-GUIDE.md)
