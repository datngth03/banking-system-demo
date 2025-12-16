// ============================================================================
// Banking System - Main Infrastructure Deployment (Bicep)
// Deploys complete Azure infrastructure for Banking System
// ============================================================================

targetScope = 'resourceGroup'

// ============================================================================
// PARAMETERS
// ============================================================================

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

@description('Azure region for resources')
param location string = resourceGroup().location

@description('Base name for all resources')
param baseName string = 'banking'

@description('Admin username for PostgreSQL')
@secure()
param postgresAdminUsername string

@description('Admin password for PostgreSQL')
@secure()
param postgresAdminPassword string

@description('JWT secret for authentication')
@secure()
param jwtSecret string

@description('Encryption key for data encryption')
@secure()
param encryptionKey string

@description('Container registry name')
param containerRegistryName string = 'bankingcr${environment}${take(uniqueString(resourceGroup().id), 6)}'

@description('Container Apps environment name')
param containerAppsEnvName string = '${baseName}-env-${environment}'

@description('API container image tag')
param apiImageTag string = 'latest'

@description('Minimum replicas for Container Apps')
@minValue(0)  // Changed from 1 to 0 to allow scale-to-zero
@maxValue(30)
param minReplicas int = 0  // Changed default to 0

@description('Maximum replicas for Container Apps')
@minValue(1)
@maxValue(30)
param maxReplicas int = 10

@description('Enable Application Insights')
param enableAppInsights bool = true

@description('Enable monitoring stack (Prometheus, Grafana)')
param enableMonitoringStack bool = false

// ============================================================================
// VARIABLES
// ============================================================================

var resourceNamePrefix = '${baseName}-${environment}'
var tags = {
  Environment: environment
  Project: 'BankingSystem'
  ManagedBy: 'Bicep'
}

// ============================================================================
// MODULES
// ============================================================================

// Container Registry - COMMENTED OUT (not supported in Azure Student/Trial)
// Uncomment when needed for Docker image deployment
/*
module containerRegistry 'modules/container-registry.bicep' = {
  name: 'containerRegistry-deployment'
  params: {
    name: containerRegistryName
    location: location
    tags: tags
    sku: 'Basic'  // Always Basic - cheapest option
  }
}
*/

// Key Vault - Standard is OK (no Basic tier)
module keyVault 'modules/keyvault.bicep' = {
  name: 'keyVault-deployment'
  params: {
    name: '${resourceNamePrefix}-kv'
    location: location
    tags: tags
    secrets: [
      {
        name: 'jwt-secret'
        value: jwtSecret
      }
      {
        name: 'encryption-key'
        value: encryptionKey
      }
    ]
  }
}

// PostgreSQL Business Database - Always Burstable B1ms (smallest)
module postgresqlBusiness 'modules/postgresql.bicep' = {
  name: 'postgresql-business-deployment'
  params: {
    name: '${resourceNamePrefix}-db'
    location: location
    tags: union(tags, { Database: 'Business' })
    adminUsername: postgresAdminUsername
    adminPassword: postgresAdminPassword
    databaseName: 'BankingSystemDb'
    skuName: 'Standard_B1ms'  // Smallest Burstable SKU
    tier: 'Burstable'
    storageSizeGB: 32  // Minimum storage
    highAvailability: 'Disabled'
    backupRetentionDays: 7  // Minimum retention
  }
}

// PostgreSQL Hangfire Database - Always Burstable B1ms (smallest)
module postgresqlHangfire 'modules/postgresql.bicep' = {
  name: 'postgresql-hangfire-deployment'
  params: {
    name: '${resourceNamePrefix}-hangfire'
    location: location
    tags: union(tags, { Database: 'Hangfire' })
    adminUsername: postgresAdminUsername
    adminPassword: postgresAdminPassword
    databaseName: 'BankingSystemHangfire'
    skuName: 'Standard_B1ms'  // Smallest Burstable SKU
    tier: 'Burstable'
    storageSizeGB: 32  // Minimum storage
    highAvailability: 'Disabled'
    backupRetentionDays: 7  // Minimum retention
  }
}

// Redis Cache - Always Basic C0 (smallest, cheapest)
module redis 'modules/redis.bicep' = {
  name: 'redis-deployment'
  params: {
    name: '${resourceNamePrefix}-redis'
    location: location
    tags: tags
    sku: 'Basic'  // Always Basic - cheapest
    family: 'C'
    capacity: 0  // C0 - smallest size (250MB)
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

// Log Analytics Workspace - PerGB2018 (pay-as-you-go, cheapest)
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'logAnalytics-deployment'
  params: {
    name: '${resourceNamePrefix}-logs'
    location: location
    tags: tags
    sku: 'PerGB2018'  // Pay-as-you-go
    retentionInDays: 30  // Minimum retention for cost saving
  }
}

// Application Insights - Pay-as-you-go (no tiers, alerts disabled for Student subscription)
module appInsights 'modules/app-insights.bicep' = if (enableAppInsights) {
  name: 'appInsights-deployment'
  params: {
    name: '${resourceNamePrefix}-insights'
    location: location
    tags: tags
    workspaceResourceId: logAnalytics.outputs.id
  }
}

// Container Apps Environment - Consumption plan (pay-as-you-go)
module containerAppsEnv 'modules/container-apps-env.bicep' = {
  name: 'containerAppsEnv-deployment'
  params: {
    name: containerAppsEnvName
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalytics.outputs.customerId
    logAnalyticsWorkspaceKey: logAnalytics.outputs.primarySharedKey
  }
}

// Banking API Container App - Minimal resources (using public image, no registry)
module bankingApi 'modules/container-app.bicep' = {
  name: 'bankingApi-deployment'
  params: {
    name: '${resourceNamePrefix}-api'
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnv.outputs.id
    containerImage: 'mcr.microsoft.com/dotnet/samples:aspnetapp'  // Use public image for now
    containerRegistryServer: ''  // No registry needed for public images
    containerRegistryUsername: ''
    containerRegistryPassword: ''
    targetPort: 8080
    minReplicas: 0  // Scale to zero when no traffic (FREE!)
    maxReplicas: 3  // Lower max for cost control
    cpu: '0.5'  // Minimum CPU (0.5 vCPU)
    memory: '1Gi'  // Minimum memory (1GB)
    keyVaultName: keyVault.outputs.name
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: environment == 'prod' ? 'Production' : (environment == 'staging' ? 'Staging' : 'Development')
      }
      {
        name: 'ASPNETCORE_URLS'
        value: 'http://+:8080'
      }
      {
        name: 'ConnectionStrings__DefaultConnection'
        secretRef: 'db-business-connection'
      }
      {
        name: 'ConnectionStrings__HangfireConnection'
        secretRef: 'db-hangfire-connection'
      }
      {
        name: 'ConnectionStrings__Redis'
        secretRef: 'redis-connection'
      }
      {
        name: 'JwtSettings__Secret'
        secretRef: 'jwt-secret'
      }
      {
        name: 'JwtSettings__Issuer'
        value: 'https://${resourceNamePrefix}-api.azurecontainerapps.io'
      }
      {
        name: 'JwtSettings__Audience'
        value: 'https://${resourceNamePrefix}-api.azurecontainerapps.io'
      }
      {
        name: 'JwtSettings__ExpiryMinutes'
        value: '60'
      }
      {
        name: 'EncryptionSettings__Key'
        secretRef: 'encryption-key'
      }
      {
        name: 'RateLimitSettings__PermitLimit'
        value: environment == 'prod' ? '1000' : '5000'
      }
      {
        name: 'RateLimitSettings__WindowInSeconds'
        value: '60'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: enableAppInsights ? appInsights.outputs.connectionString : ''
      }
    ]
    secrets: [
      {
        name: 'db-business-connection'
        value: 'Host=${postgresqlBusiness.outputs.fqdn};Port=5432;Database=BankingSystemDb;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true'
      }
      {
        name: 'db-hangfire-connection'
        value: 'Host=${postgresqlHangfire.outputs.fqdn};Port=5432;Database=BankingSystemHangfire;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true'
      }
      {
        name: 'redis-connection'
        value: '${redis.outputs.hostName}:${redis.outputs.sslPort},password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
      }
      {
        name: 'jwt-secret'
        keyVaultUrl: '${keyVault.outputs.vaultUri}secrets/jwt-secret'
      }
      {
        name: 'encryption-key'
        keyVaultUrl: '${keyVault.outputs.vaultUri}secrets/encryption-key'
      }
    ]
  }
  dependsOn: [
    postgresqlBusiness
    postgresqlHangfire
    redis
    keyVault
  ]
}

// Update Key Vault with connection strings
module keyVaultSecrets 'modules/keyvault-secrets.bicep' = {
  name: 'keyVaultSecrets-deployment'
  params: {
    keyVaultName: keyVault.outputs.name
    secrets: [
      {
        name: 'db-business-connection'
        value: 'Host=${postgresqlBusiness.outputs.fqdn};Port=5432;Database=BankingSystemDb;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true'
      }
      {
        name: 'db-hangfire-connection'
        value: 'Host=${postgresqlHangfire.outputs.fqdn};Port=5432;Database=BankingSystemHangfire;Username=${postgresAdminUsername};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=true'
      }
      {
        name: 'redis-connection'
        value: '${redis.outputs.hostName}:${redis.outputs.sslPort},password=${redis.outputs.primaryKey},ssl=True,abortConnect=False'
      }
    ]
  }
  dependsOn: [
    postgresqlBusiness
    postgresqlHangfire
    redis
  ]
}

// Grant Container App Managed Identity access to Key Vault
module keyVaultAccessPolicy 'modules/keyvault-access.bicep' = {
  name: 'keyVaultAccess-deployment'
  params: {
    keyVaultName: keyVault.outputs.name
    principalId: bankingApi.outputs.managedIdentityPrincipalId
  }
  dependsOn: [
    bankingApi
    keyVault
  ]
}

// ============================================================================
// OUTPUTS
// ============================================================================

@description('API URL')
output apiUrl string = bankingApi.outputs.fqdn

// Container Registry outputs commented out - not deployed
/*
@description('Container Registry Login Server')
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer

@description('Container Registry Admin Username')
output containerRegistryAdminUsername string = containerRegistry.outputs.adminUsername
*/

@description('PostgreSQL Business Server FQDN')
output postgresBusinessFqdn string = postgresqlBusiness.outputs.fqdn

@description('PostgreSQL Hangfire Server FQDN')
output postgresHangfireFqdn string = postgresqlHangfire.outputs.fqdn

@description('Redis Host Name')
output redisHostName string = redis.outputs.hostName

@description('Key Vault URI')
output keyVaultUri string = keyVault.outputs.vaultUri

@description('Application Insights Connection String')
output appInsightsConnectionString string = enableAppInsights ? appInsights.outputs.connectionString : ''

@description('Log Analytics Workspace ID')
output logAnalyticsWorkspaceId string = logAnalytics.outputs.customerId

@description('Container App Managed Identity Principal ID')
output containerAppPrincipalId string = bankingApi.outputs.managedIdentityPrincipalId
