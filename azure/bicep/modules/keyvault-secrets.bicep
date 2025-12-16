// ============================================================================
// Key Vault Secrets Module
// Adds secrets to existing Key Vault
// ============================================================================

@description('Key Vault name')
param keyVaultName string

@description('Secrets to add')
param secrets array

// ============================================================================
// RESOURCE
// ============================================================================

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource keyVaultSecrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [for secret in secrets: {
  parent: keyVault
  name: secret.name
  properties: {
    value: secret.value
    contentType: 'text/plain'
  }
}]

// ============================================================================
// OUTPUTS
// ============================================================================

output secretNames array = [for (secret, i) in secrets: keyVaultSecrets[i].name]
