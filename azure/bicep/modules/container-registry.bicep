// ============================================================================
// Container Registry Module
// ============================================================================

@description('Container Registry name')
param name string

@description('Location for the resource')
param location string

@description('Resource tags')
param tags object = {}

@description('SKU for Container Registry')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param sku string = 'Standard'

@description('Enable admin user')
param adminUserEnabled bool = true

// ============================================================================
// RESOURCE
// ============================================================================

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: adminUserEnabled
    publicNetworkAccess: 'Enabled'
    zoneRedundancy: sku == 'Premium' ? 'Enabled' : 'Disabled'
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
      trustPolicy: {
        type: 'Notary'
        status: 'disabled'
      }
      retentionPolicy: {
        days: 30
        status: 'enabled'
      }
    }
    encryption: {
      status: 'disabled'
    }
    dataEndpointEnabled: false
    networkRuleBypassOptions: 'AzureServices'
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output id string = containerRegistry.id
output name string = containerRegistry.name
output loginServer string = containerRegistry.properties.loginServer
output adminUsername string = listCredentials(containerRegistry.id, containerRegistry.apiVersion).username
output adminPassword string = listCredentials(containerRegistry.id, containerRegistry.apiVersion).passwords[0].value
