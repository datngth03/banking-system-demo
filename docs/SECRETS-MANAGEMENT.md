# ?? Secrets Management Guide

Complete guide for managing secrets across all environments.

---

## ?? Table of Contents

1. [Overview](#overview)
2. [Required Secrets](#required-secrets)
3. [Generating Secrets](#generating-secrets)
4. [Setting Secrets in GitHub](#setting-secrets-in-github)
5. [Azure Key Vault Integration](#azure-key-vault-integration)
6. [Best Practices](#best-practices)
7. [Rotation Strategy](#rotation-strategy)

---

## ?? Overview

The Banking System requires different secrets for each environment:

| Environment | Secrets Source | Usage |
|-------------|---------------|-------|
| **Development** | `.env` file | Local development |
| **Docker** | `docker-compose.yml` env vars | Local Docker |
| **Test** | GitHub Services | CI/CD tests |
| **Staging** | GitHub Secrets | Staging deployment |
| **Production** | GitHub Secrets + Azure Key Vault | Production deployment |

---

## ?? Required Secrets

### **Staging Environment**

| Secret Name | Description | How to Generate |
|-------------|-------------|-----------------|
| `DB_CONNECTION_STAGING` | PostgreSQL connection string | Azure Portal or manual |
| `HANGFIRE_CONNECTION_STAGING` | Hangfire PostgreSQL connection | Azure Portal or manual |
| `REDIS_CONNECTION_STAGING` | Redis connection string | Azure Portal or manual |
| `JWT_SECRET_STAGING` | JWT signing key (32+ chars) | `openssl rand -base64 32` |
| `SEQ_API_KEY_STAGING` | Seq ingestion API key | Seq Settings ? API Keys |
| `ENCRYPTION_KEY_STAGING` | Data encryption key (32 bytes) | `openssl rand -base64 32` |
| `ENCRYPTION_IV_STAGING` | Encryption IV (16 bytes) | `openssl rand -base64 16` |
| `APP_INSIGHTS_CONNECTION_STRING_STAGING` | Application Insights | Azure Portal ? App Insights |
| `EMAIL_USERNAME` | SendGrid/SMTP username | SendGrid Dashboard |
| `EMAIL_PASSWORD` | SendGrid/SMTP password | SendGrid API Key |

### **Production Environment**

Same as Staging but with `_PRODUCTION` suffix:

- `DB_CONNECTION_PRODUCTION`
- `HANGFIRE_CONNECTION_PRODUCTION`
- `REDIS_CONNECTION_PRODUCTION`
- `JWT_SECRET_PRODUCTION`
- `SEQ_API_KEY_PRODUCTION`
- `ENCRYPTION_KEY_PRODUCTION`
- `ENCRYPTION_IV_PRODUCTION`
- `APP_INSIGHTS_CONNECTION_STRING` (no suffix)
- `EMAIL_USERNAME` (shared)
- `EMAIL_PASSWORD` (shared)

---

## ?? Generating Secrets

### **1. JWT Secret (minimum 32 characters)**

```powershell
# Windows/Linux/Mac
openssl rand -base64 32

# Output example:
# kR3mT9yB4vN2xD8wQ1pL7jF6sA5gH0cM9zX4eW2qV1u=
```

### **2. Encryption Key (32 bytes)**

```powershell
openssl rand -base64 32

# Output example:
# nK8mJ3yT4vB2xN7wP1dL5jR6sG9hF0cQ8zE4eM2qA1i=
```

### **3. Encryption IV (16 bytes)**

```powershell
openssl rand -base64 16

# Output example:
# pL3mK8yT4vN2xD1w==
```

### **4. Database Connection String**

```
Host=your-db-server.postgres.database.azure.com;
Database=BankingSystemStaging;
Username=admin@your-db-server;
Password=YOUR_STRONG_PASSWORD;
SSL Mode=Require;
Trust Server Certificate=true
```

### **5. Redis Connection String**

```
your-redis.redis.cache.windows.net:6380,
ssl=true,
password=YOUR_REDIS_KEY,
abortConnect=false
```

### **6. Application Insights Connection String**

Get from Azure Portal:
1. Navigate to Application Insights resource
2. Go to **Properties**
3. Copy **Connection String**

Format:
```
InstrumentationKey=12345678-1234-1234-1234-123456789012;
IngestionEndpoint=https://region.applicationinsights.azure.com/;
LiveEndpoint=https://region.livediagnostics.monitor.azure.com/
```

---

## ?? Setting Secrets in GitHub

### **Method 1: GitHub Web UI**

1. **Navigate to repository settings:**
   ```
   https://github.com/YOUR_USERNAME/YOUR_REPO/settings/secrets/actions
   ```

2. **Click "New repository secret"**

3. **Enter secret details:**
   - **Name:** `DB_CONNECTION_STAGING`
   - **Value:** Your connection string
   - Click **Add secret**

4. **Repeat for all secrets**

### **Method 2: GitHub CLI**

```powershell
# Install GitHub CLI if not already installed
# Download from: https://cli.github.com/

# Login to GitHub
gh auth login

# Set secrets one by one
gh secret set DB_CONNECTION_STAGING
# Paste value when prompted and press Ctrl+D (Linux/Mac) or Ctrl+Z (Windows)

# Or set from file
echo "your-secret-value" | gh secret set JWT_SECRET_STAGING

# Or set multiple secrets from .env file
gh secret set -f .env.staging
```

### **Method 3: Bulk Import (PowerShell)**

```powershell
# Create a secrets file (DO NOT COMMIT!)
$secrets = @{
    "DB_CONNECTION_STAGING" = "Host=..."
    "JWT_SECRET_STAGING" = "kR3mT9yB4vN2..."
    "ENCRYPTION_KEY_STAGING" = "nK8mJ3yT4vB2..."
}

# Set all secrets
foreach ($secret in $secrets.GetEnumerator()) {
    Write-Host "Setting $($secret.Key)..."
    echo $secret.Value | gh secret set $secret.Key
}

# Verify
gh secret list
```

---

## ?? Azure Key Vault Integration

For production, use Azure Key Vault for additional security:

### **1. Create Key Vault**

```powershell
# Azure CLI
az keyvault create `
  --name kv-banking-prod `
  --resource-group rg-banking-prod `
  --location eastus
```

### **2. Add Secrets to Key Vault**

```powershell
# Add secrets
az keyvault secret set `
  --vault-name kv-banking-prod `
  --name jwt-secret `
  --value "YOUR_JWT_SECRET"

az keyvault secret set `
  --vault-name kv-banking-prod `
  --name db-connection `
  --value "Host=..."
```

### **3. Reference in appsettings.Production.json**

```json
{
  "JwtSettings": {
    "Secret": "@Microsoft.KeyVault(SecretUri=https://kv-banking-prod.vault.azure.net/secrets/jwt-secret/)"
  }
}
```

### **4. Grant Access to App**

```powershell
# Get Container App identity
$principalId = az containerapp identity show `
  --name banking-api `
  --resource-group rg-banking-prod `
  --query principalId -o tsv

# Grant access
az keyvault set-policy `
  --name kv-banking-prod `
  --object-id $principalId `
  --secret-permissions get list
```

---

## ? Best Practices

### **1. Secret Generation**

- ? Use cryptographically secure random generators
- ? Minimum 32 characters for secrets
- ? Different secrets for each environment
- ? Never use default/example values
- ? Never use predictable patterns

### **2. Secret Storage**

- ? Use GitHub Secrets for CI/CD
- ? Use Azure Key Vault for production
- ? Store backup in password manager (1Password, LastPass)
- ? Never commit to Git
- ? Never store in plain text files
- ? Never share via email/chat

### **3. Secret Access**

- ? Grant minimum required permissions
- ? Use different secrets per environment
- ? Enable audit logging
- ? Don't share secrets between environments
- ? Don't log secrets in application logs

### **4. Secret Rotation**

- ? Rotate secrets every 90 days
- ? Rotate immediately if compromised
- ? Use versioned secrets (Key Vault)
- ? Test rotation in staging first

---

## ?? Rotation Strategy

### **Quarterly Rotation (Every 90 Days)**

```powershell
# 1. Generate new secrets
$newJwtSecret = openssl rand -base64 32
$newEncryptionKey = openssl rand -base64 32

# 2. Update in GitHub Secrets
gh secret set JWT_SECRET_PRODUCTION --body $newJwtSecret

# 3. Update in Azure Key Vault
az keyvault secret set `
  --vault-name kv-banking-prod `
  --name jwt-secret `
  --value $newJwtSecret

# 4. Restart application (zero-downtime)
kubectl rollout restart deployment/banking-api

# 5. Verify
kubectl logs deployment/banking-api | grep "JWT"
```

### **Emergency Rotation (Suspected Compromise)**

```powershell
# 1. Immediately rotate all secrets
# 2. Revoke old secrets/keys
# 3. Audit access logs
# 4. Deploy updated secrets
# 5. Monitor for anomalies
# 6. Document incident
```

---

## ??? Security Checklist

Before deploying to production:

- [ ] All secrets generated with strong randomness
- [ ] Different secrets for Staging and Production
- [ ] Secrets stored in GitHub Secrets
- [ ] Production secrets also in Azure Key Vault
- [ ] `.env` file in `.gitignore`
- [ ] No secrets in source code
- [ ] No secrets in Docker images
- [ ] Secrets rotation schedule defined
- [ ] Backup of secrets in secure location
- [ ] Access audit logging enabled
- [ ] Team members know rotation procedure

---

## ?? Verification

### **Verify GitHub Secrets**

```powershell
# List all secrets (won't show values)
gh secret list

# Expected output:
# DB_CONNECTION_STAGING           Updated 2025-12-03
# DB_CONNECTION_PRODUCTION         Updated 2025-12-03
# JWT_SECRET_STAGING               Updated 2025-12-03
# ...
```

### **Verify Azure Key Vault**

```powershell
# List secrets in Key Vault
az keyvault secret list --vault-name kv-banking-prod --query "[].name"

# Get secret version (not value)
az keyvault secret show `
  --vault-name kv-banking-prod `
  --name jwt-secret `
  --query "id"
```

### **Verify in Application**

```powershell
# Check if secrets are loaded (without showing values)
kubectl exec deployment/banking-api -- \
  printenv | grep -E "(JWT_SECRET|DB_CONNECTION)" | sed 's/=.*/=***/'

# Expected:
# JWT_SECRET=***
# DB_CONNECTION=***
```

---

## ?? Support

If secrets are compromised:

1. **Immediately rotate all secrets**
2. **Check audit logs for unauthorized access**
3. **Notify security team**
4. **Document incident**
5. **Review and improve security**

---

## ?? Additional Resources

- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [OWASP Secrets Management](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)
- [Password Generation Best Practices](https://www.nist.gov/publications/digital-identity-guidelines)

---

**Last Updated:** December 2025
**Review Schedule:** Quarterly
