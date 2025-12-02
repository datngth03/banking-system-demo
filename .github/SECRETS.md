# GitHub Actions Secrets Configuration

This document lists all required secrets for CI/CD pipelines.

## Required Secrets

### Code Quality & Security

| Secret Name | Description | Required For | Example |
|-------------|-------------|--------------|---------|
| `SONAR_TOKEN` | SonarCloud authentication token | Code Quality workflow | `sqp_xxxxxxxxxxxxx` |
| `CODECOV_TOKEN` | Codecov.io upload token | CI workflow (optional) | `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx` |

### Container Registry

| Secret Name | Description | Required For | Example |
|-------------|-------------|--------------|---------|
| `GITHUB_TOKEN` | Automatically provided by GitHub | CD workflow | Auto-generated |

### Deployment - Staging

| Secret Name | Description | Required For | Example |
|-------------|-------------|--------------|---------|
| `DB_CONNECTION_STAGING` | PostgreSQL connection string | Staging deployment | `Host=postgres;Database=BankingStaging;...` |
| `REDIS_CONNECTION_STAGING` | Redis connection string | Staging deployment | `redis:6379,abortConnect=false` |
| `JWT_SECRET_STAGING` | JWT signing key | Staging deployment | `your-super-secret-key-min-32-chars` |
| `POSTGRES_PASSWORD_STAGING` | PostgreSQL password | Staging deployment | `SecurePassword123!` |

### Deployment - Production

| Secret Name | Description | Required For | Example |
|-------------|-------------|--------------|---------|
| `DB_CONNECTION_PRODUCTION` | PostgreSQL connection string | Production deployment | `Host=prod-db;Database=Banking;...` |
| `REDIS_CONNECTION_PRODUCTION` | Redis connection string | Production deployment | `prod-redis:6379,ssl=true` |
| `JWT_SECRET_PRODUCTION` | JWT signing key (production) | Production deployment | `highly-secure-production-key-min-32-chars` |
| `POSTGRES_PASSWORD_PRODUCTION` | PostgreSQL password (prod) | Production deployment | `VerySecureProductionPassword456!` |
| `ENCRYPTION_KEY_PRODUCTION` | AES encryption key | Production deployment | `32-byte-base64-encoded-key` |

### Cloud Deployment (Azure - Optional)

| Secret Name | Description | Required For | Example |
|-------------|-------------|--------------|---------|
| `AZURE_CREDENTIALS` | Azure service principal | Azure deployment | JSON credentials |
| `ACR_USERNAME` | Azure Container Registry user | Docker push to ACR | `bankingsystem` |
| `ACR_PASSWORD` | Azure Container Registry password | Docker push to ACR | `password` |
| `AZURE_CONTAINER_APP_NAME` | Azure Container App name | Container Apps deployment | `banking-api-prod` |
| `AZURE_RESOURCE_GROUP` | Azure resource group | Azure deployment | `rg-banking-prod` |

### Notifications (Optional)

| Secret Name | Description | Required For | Example |
|-------------|-------------|--------------|---------|
| `SLACK_WEBHOOK_URL` | Slack webhook for notifications | Deployment notifications | `https://hooks.slack.com/...` |
| `TEAMS_WEBHOOK_URL` | Microsoft Teams webhook | Deployment notifications | `https://outlook.office.com/webhook/...` |

## How to Add Secrets

### GitHub Repository Settings

1. Go to your repository on GitHub
2. Click **Settings** ? **Secrets and variables** ? **Actions**
3. Click **New repository secret**
4. Add the secret name and value
5. Click **Add secret**

### Environment-Specific Secrets

For staging and production environments:

1. Go to **Settings** ? **Environments**
2. Create environments: `staging` and `production`
3. Add environment-specific secrets
4. Configure protection rules (required reviewers, wait timer, etc.)

## Security Best Practices

### Secret Management

- ? **Never commit secrets** to version control
- ? **Use different secrets** for each environment
- ? **Rotate secrets regularly** (every 90 days recommended)
- ? **Use strong, randomly generated** secrets
- ? **Limit secret access** to necessary workflows only
- ? **Enable audit logging** for secret access

### Secret Generation

#### JWT Secret
```bash
openssl rand -base64 32
```

#### Encryption Key
```bash
openssl rand -base64 32
```

#### Database Password
```bash
openssl rand -base64 24 | tr -d "=+/" | cut -c1-20
```

## Validation

### Test Secrets Locally (Development Only)

Create `.env.local` file (DO NOT COMMIT):

```bash
# Staging
export DB_CONNECTION_STAGING="Host=localhost;Database=BankingStaging;..."
export REDIS_CONNECTION_STAGING="localhost:6379"
export JWT_SECRET_STAGING="your-staging-secret-min-32-chars"

# Production
export DB_CONNECTION_PRODUCTION="Host=prod-host;Database=Banking;..."
export REDIS_CONNECTION_PRODUCTION="prod-redis:6379"
export JWT_SECRET_PRODUCTION="your-production-secret-min-32-chars"
```

### Verify Secrets in GitHub

```bash
# List all secrets (requires GitHub CLI)
gh secret list

# Set a secret
gh secret set SONAR_TOKEN < sonar_token.txt
```

## Troubleshooting

### Common Issues

**Secret not found:**
- Verify secret name matches exactly (case-sensitive)
- Check if secret is set at repository or environment level
- Ensure workflow has permission to access the secret

**Secret rotation:**
- Update secret in GitHub Settings
- Re-run failed workflows
- Verify new secret works before removing old one

**Environment protection:**
- Check if environment requires approval
- Verify deployment branch patterns
- Review environment protection rules

## Monitoring

### Audit Secret Access

1. Go to **Settings** ? **Security** ? **Audit log**
2. Filter by secret access events
3. Review who accessed which secrets

### Alert on Secret Changes

Consider setting up notifications for:
- New secrets added
- Secrets modified
- Secrets deleted
- Failed secret access attempts

---

**?? SECURITY WARNING**

Never share secrets via:
- Email
- Slack/Teams messages
- Screenshots
- Log files
- Error messages

Always use secure channels like:
- Password managers
- Encrypted messaging
- GitHub Secrets
- Azure Key Vault
