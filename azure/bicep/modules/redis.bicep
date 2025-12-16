// ============================================================================
// Redis Cache Module
// ============================================================================

@description('Redis Cache name')
param name string

@description('Location for the resource')
param location string

@description('Resource tags')
param tags object = {}

@description('Redis SKU')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Basic'

@description('Redis SKU family')
@allowed([
  'C'
  'P'
])
param family string = 'C'

@description('Redis capacity (size)')
@minValue(0)
@maxValue(6)
param capacity int = 0

@description('Enable non-SSL port (not recommended)')
param enableNonSslPort bool = false

@description('Minimum TLS version')
@allowed([
  '1.0'
  '1.1'
  '1.2'
])
param minimumTlsVersion string = '1.2'

// ============================================================================
// RESOURCE
// ============================================================================

resource redisCache 'Microsoft.Cache/redis@2023-08-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
      family: family
      capacity: capacity
    }
    enableNonSslPort: enableNonSslPort
    minimumTlsVersion: minimumTlsVersion
    publicNetworkAccess: 'Enabled'
    redisConfiguration: {
      'maxmemory-policy': 'allkeys-lru'
      'maxmemory-reserved': sku == 'Premium' ? '50' : '10'
      'maxfragmentationmemory-reserved': sku == 'Premium' ? '50' : '10'
    }
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output id string = redisCache.id
output name string = redisCache.name
output hostName string = redisCache.properties.hostName
output sslPort int = redisCache.properties.sslPort
output port int = redisCache.properties.port
output primaryKey string = listKeys(redisCache.id, redisCache.apiVersion).primaryKey
output secondaryKey string = listKeys(redisCache.id, redisCache.apiVersion).secondaryKey
