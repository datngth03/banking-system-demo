// ============================================================================
// Container App Module
// ============================================================================

@description('Container App name')
param name string

@description('Location for the resource')
param location string

@description('Resource tags')
param tags object = {}

@description('Container Apps Environment ID')
param containerAppsEnvironmentId string

@description('Container image')
param containerImage string

@description('Container registry server')
param containerRegistryServer string

@description('Container registry username')
param containerRegistryUsername string

@description('Container registry password')
@secure()
param containerRegistryPassword string

@description('Target port for container')
param targetPort int = 8080

@description('Minimum replicas')
@minValue(0)
@maxValue(30)
param minReplicas int = 1

@description('Maximum replicas')
@minValue(1)
@maxValue(30)
param maxReplicas int = 10

@description('CPU cores')
param cpu string = '1.0'

@description('Memory size')
param memory string = '2Gi'

@description('Environment variables')
param environmentVariables array = []

@description('Secrets')
param secrets array = []

@description('Key Vault name for secret references')
param keyVaultName string = ''

// ============================================================================
// RESOURCE
// ============================================================================

resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: targetPort
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
        transport: 'auto'
      }
      registries: [
        {
          server: containerRegistryServer
          username: containerRegistryUsername
          passwordSecretRef: 'container-registry-password'
        }
      ]
      secrets: concat([
        {
          name: 'container-registry-password'
          value: containerRegistryPassword
        }
      ], secrets)
    }
    template: {
      containers: [
        {
          name: name
          image: containerImage
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: environmentVariables
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-rule'
            http: {
              metadata: {
                concurrentRequests: '100'
              }
            }
          }
        ]
      }
    }
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
output managedIdentityPrincipalId string = containerApp.identity.principalId
output latestRevisionName string = containerApp.properties.latestRevisionName
