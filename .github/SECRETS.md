# GitHub Actions Secrets Configuration

This document lists all required secrets for CI/CD pipelines.

## 📋 **Current Workflow Status**

**Active Workflows:**
- ✅ `ci.yml` - Build & Test (no secrets required)
- ✅ `cd.yml` - Deploy (uses GITHUB_TOKEN)

**Secret Requirements:**
- **CI Workflow:** No external secrets needed (PostgreSQL & Redis run as services)
- **CD Workflow:** Uses auto-provided `GITHUB_TOKEN` only

---

## 🔑 **Required Secrets (Minimal Setup)**

### **For CI/CD Workflows**

| Secret Name | Description | Required For | Auto-Provided |
|-------------|-------------|--------------|---------------|
| `GITHUB_TOKEN` | GitHub Actions authentication | CD workflow | ✅ Yes (automatic) |

**Note:** The current setup uses Docker services for PostgreSQL and Redis in CI, so no external database secrets are needed!

---

## 🚀 **Optional Secrets (Production Deployment)**

If you deploy to production environments, you may need:

### **Container Registry (Optional)**

| Secret Name | Description | When Needed | Example |
|-------------|-------------|-------------|---------|
| `DOCKER_USERNAME` | Docker Hub username | If pushing to Docker Hub | `yourusername` |
| `DOCKER_PASSWORD` | Docker Hub password/token | If pushing to Docker Hub | `dckr_pat_xxxxx` |

### **Cloud Deployment - Azure (Optional)**

| Secret Name | Description | When Needed | How to Get |
|-------------|-------------|-------------|------------|
| `AZURE_CREDENTIALS` | Azure service principal | Azure deployment | `az ad sp create-for-rbac` |
| `ACR_USERNAME` | Azure Container Registry | Push to ACR | Azure Portal → ACR → Access keys |
| `ACR_PASSWORD` | ACR password | Push to ACR | Azure Portal → ACR → Access keys |

### **Database & Infrastructure (Production)**

| Secret Name | Description | When Needed | Example |
|-------------|-------------|-------------|---------|
| `DB_CONNECTION_PRODUCTION` | PostgreSQL connection | Production deploy | `Host=prod.postgres.azure.com;...` |
| `REDIS_CONNECTION_PRODUCTION` | Redis connection | Production deploy | `prod-redis:6379,ssl=true` |
| `JWT_SECRET_PRODUCTION` | JWT signing key | Production deploy | `openssl rand -base64 32` |
| `ENCRYPTION_KEY_PRODUCTION` | AES encryption key | Production deploy | `openssl rand -base64 32` |

---

## 🛠️ **How to Add Secrets**

### **Method 1: GitHub Web UI**

1. Go to your repository on GitHub
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter secret name and value
5. Click **Add secret**

### **Method 2: GitHub CLI**

```bash
# Install GitHub CLI
# Download from: https://cli.github.com/

# Login
gh auth login

# Set a secret
gh secret set SECRET_NAME
# Paste value and press Ctrl+D (Linux/Mac) or Ctrl+Z (Windows)

# Or from file
echo "your-secret-value" | gh secret set SECRET_NAME

# List secrets
gh secret list
```

---

## 🔐 **Security Best Practices**

### **Secret Management**

- ✅ **Never commit secrets** to Git
- ✅ **Use different secrets** for each environment (dev/staging/prod)
- ✅ **Rotate secrets regularly** (every 90 days)
- ✅ **Use strong, random** secrets (minimum 32 characters)
- ✅ **Limit access** to necessary people only
- ✅ **Enable audit logging**

### **Secret Generation**

#### **JWT Secret (32+ characters)**
```bash
openssl rand -base64 32
# Output: kR3mT9yB4vN2xD8wQ1pL7jF6sA5gH0cM9zX4eW2qV1u=
```

#### **Encryption Key (32 bytes)**
```bash
openssl rand -base64 32
# Output: nK8mJ3yT4vB2xN7wP1dL5jR6sG9hF0cQ8zE4eM2qA1i=
```

#### **Strong Password**
```bash
openssl rand -base64 24 | tr -d "=+/" | cut -c1-20
# Output: mK3nT8yB4vN2xD1wP5j
```

---

## ✅ **Current Setup Validation**

### **What's Currently Working:**

```powershell
# Check CI workflow status
gh workflow view ci.yml

# Check CD workflow status  
gh workflow view cd.yml

# List current secrets (won't show values)
gh secret list
```

### **Minimal Setup (Current State):**

**No secrets needed for local development:**
```powershell
# Just run docker-compose
docker-compose up -d

# CI/CD works with GITHUB_TOKEN only
# GitHub provides this automatically
```

---

## 📚 **When You'll Need Secrets**

### **Scenario 1: Deploy to Docker Hub**
Add `DOCKER_USERNAME` and `DOCKER_PASSWORD`

### **Scenario 2: Deploy to Azure**
Add `AZURE_CREDENTIALS`, `ACR_USERNAME`, `ACR_PASSWORD`

### **Scenario 3: Production Database**
Add `DB_CONNECTION_PRODUCTION`, `REDIS_CONNECTION_PRODUCTION`

### **Scenario 4: Advanced Monitoring**
Add Application Insights connection string

---

## 🔍 **Verify Secrets**

### **List Secrets (GitHub CLI)**
```bash
gh secret list

# Expected output (if no secrets set):
# No secrets for datngth03/banking-system-demo
```

### **Test Workflow Without Secrets**
```bash
# CI workflow should pass without any secrets
git push origin main

# Check workflow run
gh run list --workflow=ci.yml
```

---

## ⚠️ **Security Warnings**

### **Never Share Secrets Via:**
- ❌ Email
- ❌ Chat (Slack, Teams, Discord)
- ❌ Screenshots
- ❌ Log files
- ❌ Error messages
- ❌ Public GitHub issues

### **Always Use Secure Channels:**
- ✅ GitHub Secrets
- ✅ Azure Key Vault
- ✅ Password managers (1Password, LastPass)
- ✅ Encrypted messaging

---

## 🆘 **Troubleshooting**

### **Secret Not Found**
```yaml
# In workflow file, check:
${{ secrets.SECRET_NAME }}  # ← Name must match exactly (case-sensitive)
```

### **Workflow Can't Access Secret**
- Verify secret is set at repository level (not environment level)
- Check workflow permissions in Settings → Actions → General

### **Need to Rotate Secret**
1. Generate new secret value
2. Update in GitHub Secrets (keeps old value until saved)
3. Click "Update secret"
4. Re-run failed workflows

---

## 📊 **Audit & Monitoring**

### **View Secret Access**
1. Go to **Settings** → **Security** → **Audit log**
2. Filter by: `action:repo.update_secret`
3. Review who changed what

### **Best Practice Checklist**
- [ ] Secrets rotated every 90 days
- [ ] Different secrets per environment
- [ ] Minimum 32 characters for all secrets
- [ ] No secrets in code or logs
- [ ] Audit log reviewed quarterly
- [ ] Only necessary people have access

---

**See also:**
- `docs/SECRETS-MANAGEMENT.md` - Complete secrets guide
- `docs/DEPLOYMENT-GUIDE.md` - Production deployment
- `.github/workflows/ci.yml` - CI configuration
- `.github/workflows/cd.yml` - CD configuration

---

*Last Updated: December 2025*  
*Current Status: Minimal secrets setup (GITHUB_TOKEN only)*
