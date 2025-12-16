// ============================================================================
// Container Apps Environment Module
// ============================================================================

@description('Container Apps Environment name')
param name string

@description('Location for the resource')
param location string

@description('Resource tags')
param tags object = {}

@description('Log Analytics Workspace ID')
param logAnalyticsWorkspaceId string

@description('Log Analytics Workspace Key')
@secure()
param logAnalyticsWorkspaceKey string

// ============================================================================
// RESOURCE
// ============================================================================

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspaceId
        sharedKey: logAnalyticsWorkspaceKey
      }
    }
    zoneRedundant: false
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output id string = containerAppsEnvironment.id
output name string = containerAppsEnvironment.name
output defaultDomain string = containerAppsEnvironment.properties.defaultDomain
