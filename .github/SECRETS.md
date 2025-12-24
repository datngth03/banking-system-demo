# GitHub Actions Secrets Configuration

Documentation for required secrets in CI/CD pipelines.

---

## Current Workflow Status

**Active Workflows:**
- `ci.yml` - Build & Test (no secrets required)
- `cd.yml` - Deploy (uses GITHUB_TOKEN)

**Secret Requirements:**
- **CI Workflow:** No external secrets needed (PostgreSQL & Redis run as GitHub services)
- **CD Workflow:** Uses auto-provided `GITHUB_TOKEN` only

---

## Required Secrets

### CI/CD Workflows

| Secret Name | Description | Required For | Auto-Provided |
|-------------|-------------|--------------|---------------|
| `GITHUB_TOKEN` | GitHub Actions authentication | CD workflow | Yes (automatic) |

**Note:** Current setup uses GitHub-hosted services for PostgreSQL and Redis in CI, eliminating need for external database secrets.

---

## Optional Secrets for Production

### Container Registry

| Secret Name | Description | When Needed |
|-------------|-------------|-------------|
| `DOCKER_USERNAME` | Docker Hub username | If pushing to Docker Hub |
| `DOCKER_PASSWORD` | Docker Hub token | If pushing to Docker Hub |

### Azure Deployment

| Secret Name | Description | How to Get |
|-------------|-------------|------------|
| `AZURE_CREDENTIALS` | Azure service principal | `az ad sp create-for-rbac` |
| `ACR_USERNAME` | Azure Container Registry user | Azure Portal → ACR → Access keys |
| `ACR_PASSWORD` | ACR password | Azure Portal → ACR → Access keys |

### Production Infrastructure

| Secret Name | Description | Example Generation |
|-------------|-------------|-------------------|
| `DB_CONNECTION_PRODUCTION` | PostgreSQL connection | See docs/AZURE-DEPLOYMENT.md |
| `REDIS_CONNECTION_PRODUCTION` | Redis connection | See docs/AZURE-DEPLOYMENT.md |
| `JWT_SECRET_PRODUCTION` | JWT signing key | `openssl rand -base64 64` |
| `ENCRYPTION_KEY_PRODUCTION` | AES encryption key | `openssl rand -base64 32` |

---

## Adding Secrets

### GitHub Web UI

1. Navigate to repository Settings
2. Go to Secrets and variables → Actions
3. Click New repository secret
4. Enter name and value
5. Click Add secret

### GitHub CLI

```bash
# Install GitHub CLI from https://cli.github.com/

# Login
gh auth login

# Set secret
gh secret set SECRET_NAME
# Paste value and press Ctrl+D (Linux/Mac) or Ctrl+Z (Windows)

# From file or pipe
echo "your-secret-value" | gh secret set SECRET_NAME

# List secrets (names only)
gh secret list
```

---

## Security Best Practices

### Secret Management

- Never commit secrets to Git
- Use different secrets per environment (dev/staging/prod)
- Rotate secrets every 90 days
- Use strong, random secrets (minimum 32 characters)
- Limit access to necessary people only
- Enable audit logging

### Secret Generation

**JWT Secret (64 characters):**
```bash
openssl rand -base64 64
```

**Encryption Key (32 bytes):**
```bash
openssl rand -base64 32
```

**Strong Password (20 characters):**
```bash
openssl rand -base64 24 | tr -d "=+/" | cut -c1-20
```

---

## Validation

### Check Workflow Status

```bash
# View CI workflow
gh workflow view ci.yml

# View CD workflow  
gh workflow view cd.yml

# List configured secrets (names only)
gh secret list
```

### Minimal Setup (Current State)

No secrets needed for local development:

```bash
# Local development
docker-compose up -d

# CI/CD works with GITHUB_TOKEN only
# GitHub provides this automatically
```

---

## When Secrets Are Needed

**Deploy to Docker Hub:**
- Add `DOCKER_USERNAME` and `DOCKER_PASSWORD`

**Deploy to Azure:**
- Add `AZURE_CREDENTIALS`, `ACR_USERNAME`, `ACR_PASSWORD`

**Production Database:**
- Add `DB_CONNECTION_PRODUCTION`, `REDIS_CONNECTION_PRODUCTION`

**Advanced Monitoring:**
- Add Application Insights connection string

---

## Troubleshooting

### Secret Not Found

Verify secret name matches exactly (case-sensitive):
```yaml
${{ secrets.SECRET_NAME }}
```

### Workflow Cannot Access Secret

- Verify secret is set at repository level (not environment level)
- Check workflow permissions in Settings → Actions → General

### Rotating Secrets

1. Generate new secret value
2. Update in GitHub Secrets
3. Click Update secret
4. Re-run failed workflows

---

## Audit & Monitoring

**View Secret Access:**
1. Go to Settings → Security → Audit log
2. Filter by: `action:repo.update_secret`
3. Review access history

**Security Checklist:**
- Secrets rotated every 90 days
- Different secrets per environment
- Minimum 32 characters for all secrets
- No secrets in code or logs
- Audit log reviewed quarterly
- Access limited to necessary personnel

---

## Related Documentation

- [AZURE-DEPLOYMENT.md](../docs/AZURE-DEPLOYMENT.md) - Azure deployment guide
- [WORKFLOW-ARCHITECTURE.md](../docs/WORKFLOW-ARCHITECTURE.md) - CI/CD pipeline documentation
- [.github/workflows/ci.yml](./workflows/ci.yml) - CI configuration
- [.github/workflows/cd.yml](./workflows/cd.yml) - CD configuration

---

**Current Status:** Minimal secrets setup (GITHUB_TOKEN only)

*Last updated: December 2025*
