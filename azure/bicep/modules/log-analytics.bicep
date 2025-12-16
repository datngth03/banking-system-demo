// ============================================================================
// Log Analytics Workspace Module
// ============================================================================

@description('Log Analytics Workspace name')
param name string

@description('Location for the resource')
param location string

@description('Resource tags')
param tags object = {}

@description('SKU for Log Analytics')
@allowed([
  'PerGB2018'
  'CapacityReservation'
])
param sku string = 'PerGB2018'

@description('Retention in days')
@minValue(30)
@maxValue(730)
param retentionInDays int = 30

// ============================================================================
// RESOURCE
// ============================================================================

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      name: sku
    }
    retentionInDays: retentionInDays
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output id string = logAnalyticsWorkspace.id
output name string = logAnalyticsWorkspace.name
output customerId string = logAnalyticsWorkspace.properties.customerId
output primarySharedKey string = logAnalyticsWorkspace.listKeys().primarySharedKey
