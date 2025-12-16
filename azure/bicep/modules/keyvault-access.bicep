// ============================================================================
// Key Vault Access Policy Module
// Grants access to a managed identity
// ============================================================================

@description('Key Vault name')
param keyVaultName string

@description('Principal ID (Managed Identity) to grant access')
param principalId string

@description('Permissions for secrets')
param secretPermissions array = [
  'get'
  'list'
]

@description('Permissions for keys')
param keyPermissions array = []

@description('Permissions for certificates')
param certificatePermissions array = []

// ============================================================================
// RESOURCE
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource accessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: principalId
        permissions: {
          keys: keyPermissions
          secrets: secretPermissions
          certificates: certificatePermissions
        }
      }
    ]
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output principalId string = principalId
