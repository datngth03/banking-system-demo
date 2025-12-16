// ============================================================================
// Application Insights Module
// ============================================================================

@description('Application Insights name')
param name string

@description('Location for the resource')
param location string

@description('Resource tags')
param tags object = {}

@description('Log Analytics Workspace resource ID')
param workspaceResourceId string

@description('Application type')
@allowed([
  'web'
  'other'
])
param applicationType string = 'web'

// ============================================================================
// RESOURCE
// ============================================================================

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: applicationType
  properties: {
    Application_Type: applicationType
    WorkspaceResourceId: workspaceResourceId
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    DisableIpMasking: false
    DisableLocalAuth: false
    // Disable smart detection/alerts (requires Microsoft.AlertsManagement)
    // Not supported in Azure Student/Trial subscriptions
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output id string = applicationInsights.id
output name string = applicationInsights.name
output instrumentationKey string = applicationInsights.properties.InstrumentationKey
output connectionString string = applicationInsights.properties.ConnectionString
