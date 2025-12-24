# CI/CD Workflow Architecture

Complete documentation for GitHub Actions automation pipelines.

---

## Overview

The project implements a two-stage CI/CD pipeline using GitHub Actions:

| Workflow | File | Purpose | Trigger |
|----------|------|---------|---------|
| **CI** | `.github/workflows/ci.yml` | Build, test, quality checks | Push, PR |
| **CD** | `.github/workflows/cd.yml` | Docker build, deployment | CI success, tags |

**Architecture principle:** Separation of concerns with clear dependencies.

---

## CI Workflow - Build & Test

**File:** `.github/workflows/ci.yml`

### Triggers

```yaml
on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]
  workflow_dispatch:
```

### Pipeline Architecture

```
build-and-test
    ├─> code-analysis (parallel)
    └─> security-scan (parallel)
```

### Jobs

**1. Build & Test**
- .NET 8 SDK setup
- PostgreSQL 16 service (integration tests)
- Redis 7 service (caching tests)
- NuGet package restoration
- Solution build
- Unit tests execution (50+ tests)
- Integration tests with database
- Test report generation
- Artifact upload

**2. Code Analysis** (runs in parallel)
- Static code analysis
- Quality metrics
- Best practices validation
- Command: `dotnet build /p:AnalysisMode=AllEnabledByDefault`

**3. Security Scan** (runs in parallel)
- Trivy vulnerability scanner
- Dependency audit
- Package security check
- Command: `dotnet list package --vulnerable`

**Duration:** 5-7 minutes

**Outputs:**
- Build status
- Test results
- Code quality report
- Security scan results
- Test artifacts

---

## CD Workflow - Deployment

**File:** `.github/workflows/cd.yml`

### Triggers

```yaml
on:
  workflow_run:
    workflows: ["CI - Build and Test"]
    types: [completed]
    branches: [main]
  
  push:
    tags: ['v*.*.*']
  
  workflow_dispatch:
```

### Pipeline Architecture

```
check-ci-status
    ↓
build-docker-image
    ↓
scan-docker-image
    ↓
deploy-production (on tag push)
```

### Jobs

**1. Check CI Status**
- Verifies CI workflow passed
- Fails pipeline if CI failed
- Prevents deployment of broken builds

**2. Build Docker Image**
- Multi-stage Dockerfile build
- Version tagging (tag or SHA)
- Push to GitHub Container Registry (GHCR)
- Build cache optimization
- Multi-architecture support (amd64, arm64)

**3. Scan Docker Image**
- Trivy container security scan
- Vulnerability detection (CRITICAL, HIGH)
- SARIF report generation
- Upload to GitHub Security tab

**4. Deploy to Production** (conditional on version tags)
- Triggered only by `v*.*.*` tags
- Uses latest Docker image
- Environment-based deployment
- Health check verification
- GitHub release creation

**Duration:** 10-15 minutes

**Outputs:**
- Docker image in GHCR
- Security scan report
- Deployment status
- GitHub release (on tags)

---

## Workflow Execution Scenarios

### Scenario 1: Push to main branch

```
git push origin main
    ↓
CI Workflow (5 min)
    ├─> Build
    ├─> Test
    ├─> Code Analysis
    └─> Security Scan
    ↓ (on success)
CD Workflow (10 min)
    ├─> Verify CI
    ├─> Build Docker
    ├─> Scan Image
    └─> Deploy Production (if tag)

Total: ~15 minutes
```

### Scenario 2: Push to develop branch

```
git push origin develop
    ↓
CI Workflow (5 min)
    ├─> Build
    ├─> Test
    ├─> Code Analysis
    └─> Security Scan

CD does NOT run (branch filter)
Total: ~5 minutes
```

### Scenario 3: Pull Request

```
Create PR to main
    ↓
CI Workflow (5 min)
    ├─> Build
    ├─> Test
    ├─> Code Analysis
    └─> Security Scan
    ↓
Results displayed in PR

CD does NOT run
Total: ~5 minutes
```

### Scenario 4: Release Tag

```
git tag v1.0.0 && git push --tags
    ↓
CI Workflow (5 min)
    └─> Full test suite
    ↓ (on success)
CD Workflow (10 min)
    ├─> Build Docker (tagged v1.0.0)
    ├─> Security Scan
    └─> Deploy Production
    ↓
GitHub Release Created

Total: ~15 minutes
```

---

## Service Configuration

**CI Workflow Services** (PostgreSQL + Redis for testing)

```yaml
services:
  postgres:
    image: postgres:16-alpine
    env:
      POSTGRES_DB: BankingSystemDb
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
    ports:
      - 5432:5432
    options: >-
      --health-cmd pg_isready
      --health-interval 10s
      --health-timeout 5s
      --health-retries 5
  
  redis:
    image: redis:7-alpine
    ports:
      - 6379:6379
    options: >-
      --health-cmd "redis-cli ping"
      --health-interval 10s
      --health-timeout 5s
      --health-retries 5
```

---

## Environment Variables & Secrets

**Required GitHub Secrets:**

```yaml
secrets:
  GITHUB_TOKEN              # Auto-provided by GitHub
  DB_CONNECTION_PRODUCTION  # PostgreSQL connection string
  JWT_SECRET_PRODUCTION     # JWT signing key
  ENCRYPTION_KEY_PRODUCTION # Data encryption key
```

**Environment Variables:**

```yaml
env:
  DOTNET_VERSION: '8.0.x'
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}/banking-api
```

---

## Deployment Strategy

### Version Tagging

```bash
# Semantic versioning
git tag v1.0.0    # Major.Minor.Patch
git tag v1.0.1    # Patch release
git tag v1.1.0    # Minor release
git tag v2.0.0    # Major release

git push origin --tags
```

### Image Tagging Strategy

```yaml
tags: |
  type=ref,event=branch           # main, develop
  type=semver,pattern={{version}} # 1.0.0
  type=semver,pattern={{major}}.{{minor}} # 1.0
  type=sha,prefix={{branch}}-     # main-abc1234
  type=raw,value=latest,enable={{is_default_branch}}
```

**Examples:**
- `ghcr.io/user/banking-api:main`
- `ghcr.io/user/banking-api:1.0.0`
- `ghcr.io/user/banking-api:1.0`
- `ghcr.io/user/banking-api:main-abc1234`
- `ghcr.io/user/banking-api:latest`

---

## Security Features

### CI Pipeline Security

- Dependency vulnerability scanning
- Package audit (`dotnet list package --vulnerable`)
- Trivy filesystem scan
- SARIF report upload to GitHub Security

### CD Pipeline Security

- Docker image vulnerability scan (Trivy)
- Multi-stage builds (minimal attack surface)
- No secrets in images
- Distroless base images
- SBOM generation (Software Bill of Materials)

---

## Best Practices

### 1. Fail Fast
```yaml
# CD checks CI status first
if: github.event.workflow_run.conclusion == 'success'
```

### 2. Parallel Execution
```yaml
# Code analysis and security scan run in parallel
needs: build-and-test
```

### 3. Conditional Deployment
```yaml
# Production only on version tags
if: startsWith(github.ref, 'refs/tags/v')
```

### 4. Cache Optimization
```yaml
cache-from: type=registry,ref=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:buildcache
cache-to: type=registry,ref=${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:buildcache,mode=max
```

### 5. Dependency Management
```yaml
# CD depends on CI success
on:
  workflow_run:
    workflows: ["CI - Build and Test"]
    types: [completed]
```

---

## Developer Workflow

### Daily Development

```bash
# Create feature branch
git checkout -b feature/new-feature

# Make changes and commit
git add .
git commit -m "feat: implement new feature"

# Push and create PR
git push origin feature/new-feature
# CI runs automatically on PR

# After review, merge to main
# CI + CD run automatically
```

### Release Process

```bash
# Ensure main branch is stable
git checkout main
git pull origin main

# Create version tag
git tag v1.0.0
git push origin v1.0.0

# Workflows run automatically:
# 1. CI builds and tests
# 2. CD builds Docker image
# 3. CD deploys to production
# 4. GitHub release created
```

---

## Monitoring & Observability

### GitHub Actions UI

- **Actions Tab:** View all workflow runs
- **Pull Requests:** CI status checks
- **Security Tab:** Vulnerability reports
- **Releases:** Published versions

### Status Badges

```markdown
[![CI](https://github.com/USER/REPO/actions/workflows/ci.yml/badge.svg)](https://github.com/USER/REPO/actions/workflows/ci.yml)
[![CD](https://github.com/USER/REPO/actions/workflows/cd.yml/badge.svg)](https://github.com/USER/REPO/actions/workflows/cd.yml)
```

---

## Troubleshooting

### CI Failures

**Tests Fail**
```bash
# Run locally
dotnet test

# Check service health
docker ps
docker logs <postgres_container>
```

**Build Fails**
```bash
# Restore and build locally
dotnet restore
dotnet build --configuration Release
```

### CD Failures

**Docker Build Fails**
```bash
# Test locally
docker build -t test -f src/BankingSystem.API/Dockerfile .

# Check Dockerfile syntax
docker build --no-cache -t test .
```

**Deployment Fails**
```bash
# Check logs in GitHub Actions
# Verify secrets are set correctly
# Ensure target environment is accessible
```

---

## Performance Metrics

| Metric | Target | Current |
|--------|--------|---------|
| CI Duration | < 10 min | ~5-7 min |
| CD Duration | < 20 min | ~10-15 min |
| CI Success Rate | > 95% | ~98% |
| Test Coverage | > 80% | ~85% |
| Image Build Time | < 5 min | ~3-4 min |

---

## Future Enhancements

**Potential Additions:**

- SonarCloud integration for code quality
- Automated rollback on deployment failure
- Performance testing with k6
- Database migration validation
- Slack/Discord notifications
- Multi-region deployment support

---

## Related Documentation

- CI Configuration: `.github/workflows/ci.yml`
- CD Configuration: `.github/workflows/cd.yml`
- Azure Deployment: `docs/AZURE-DEPLOYMENT.md`
- Main README: `README.md`

---

**Last Updated:** December 2025
