// ============================================================================
// PostgreSQL Flexible Server Module
// ============================================================================

@description('PostgreSQL server name')
param name string

@description('Location for the resource')
param location string

@description('Resource tags')
param tags object = {}

@description('Admin username')
@secure()
param adminUsername string

@description('Admin password')
@secure()
param adminPassword string

@description('Database name to create')
param databaseName string

@description('PostgreSQL version')
@allowed([
  '16'
  '15'
  '14'
  '13'
])
param version string = '16'

@description('SKU name')
param skuName string = 'Standard_B2s'

@description('SKU tier')
@allowed([
  'Burstable'
  'GeneralPurpose'
  'MemoryOptimized'
])
param tier string = 'Burstable'

@description('Storage size in GB')
@minValue(32)
@maxValue(16384)
param storageSizeGB int = 32

@description('High availability mode')
@allowed([
  'Disabled'
  'ZoneRedundant'
  'SameZone'
])
param highAvailability string = 'Disabled'

@description('Backup retention days')
@minValue(7)
@maxValue(35)
param backupRetentionDays int = 7

@description('Geo-redundant backup')
param geoRedundantBackup bool = false

// ============================================================================
// RESOURCE
// ============================================================================

resource postgresqlServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-03-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: tier
  }
  properties: {
    version: version
    administratorLogin: adminUsername
    administratorLoginPassword: adminPassword
    storage: {
      storageSizeGB: storageSizeGB
      autoGrow: 'Enabled'
    }
    backup: {
      backupRetentionDays: backupRetentionDays
      geoRedundantBackup: geoRedundantBackup ? 'Enabled' : 'Disabled'
    }
    highAvailability: {
      mode: highAvailability
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
    availabilityZone: '1'
    createMode: 'Default'
  }
}

// Firewall rule to allow Azure services
resource firewallRuleAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-03-01-preview' = {
  parent: postgresqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Create database
resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-03-01-preview' = {
  parent: postgresqlServer
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

// PostgreSQL configuration for performance
resource config 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2023-03-01-preview' = {
  parent: postgresqlServer
  name: 'max_connections'
  properties: {
    value: '200'
    source: 'user-override'
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output id string = postgresqlServer.id
output name string = postgresqlServer.name
output fqdn string = postgresqlServer.properties.fullyQualifiedDomainName
output databaseName string = database.name
