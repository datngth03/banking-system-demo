# ?? WORKFLOW STRUCTURE - EXPLAINED

## ?? **FINAL WORKFLOW ARCHITECTURE**

Sau khi cleanup, b?n có **2 workflows chính**:

---

## 1?? **CI Workflow (ci.yml)**

### **File:** `.github/workflows/ci.yml`

### **Trigger:**
```yaml
on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]
  workflow_dispatch:
```

### **Jobs:**
```
build-and-test (with PostgreSQL & Redis services)
    ?
    ?? code-analysis (parallel)
    ?? security-scan (parallel)
```

### **Khi ch?y:**
- ? Push vào `main` ho?c `develop`
- ? T?o Pull Request vào `main` ho?c `develop`
- ? Manual trigger

### **M?c ?ích:**
- Build solution
- Run unit tests
- Run integration tests
- Code quality analysis
- Security scanning (Trivy)
- Generate test reports
- Upload artifacts

---

## 2?? **CD Workflow (cd.yml)**

### **File:** `.github/workflows/cd.yml`

### **Trigger:**
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

### **Jobs:**
```
check-ci-status (verify CI passed)
    ?
build-docker-image
    ?
scan-docker-image
    ?
deploy-staging
    ?
deploy-production (if tag)
```

### **Khi ch?y:**
- ? Sau khi CI workflow complete (ch? n?u success)
- ? Push tag `v*.*.*`
- ? Manual trigger

### **M?c ?ích:**
- Build Docker image
- Security scan Docker image
- Deploy to staging
- Deploy to production

---

## ?? **WORKFLOW DEPENDENCY:**

```
Push to main
    ?
???????????????????????????
?  CI Workflow (ci.yml)   ?
?  - Build & Test         ?
?  - Code Analysis        ?
?  - Security Scan        ?
???????????????????????????
    ? (on success)
???????????????????????????
?  CD Workflow (cd.yml)   ?
?  - Check CI Status      ?
?  - Build Docker         ?
?  - Scan Docker          ?
?  - Deploy Staging       ?
?  - Deploy Production    ?
???????????????????????????
```

---

## ?? **LU?NG CH?Y:**

### **Scenario 1: Push to main**

```
t=0m    Push to main
t=0m    CI starts
t=5m    CI completes ?
        ?? Build: ?
        ?? Tests: ?
        ?? Code Quality: ?
        ?? Security: ?
t=5m    CD triggers (workflow_run)
t=5m    Check CI status: ?
t=6m    Build Docker starts
t=15m   CD completes ?
        ?? Docker: ?
        ?? Security: ?
        ?? Deploy Staging: ?

Total: ~15 minutes
```

### **Scenario 2: Push to develop**

```
t=0m    Push to develop
t=0m    CI starts
t=5m    CI completes ?

CD DOES NOT run (branch not match)

Total: ~5 minutes
```

### **Scenario 3: Pull Request**

```
t=0m    Create PR to main
t=0m    CI starts
t=5m    CI completes ?
        Shows results in PR

CD DOES NOT run (only on push)

Total: ~5 minutes
```

### **Scenario 4: Tag push (v1.0.0)**

```
t=0m    Push tag v1.0.0
t=0m    CI starts
t=5m    CI completes ?
t=5m    CD triggers (workflow_run + tag)
t=6m    Build Docker (version=1.0.0)
t=15m   Deploy Staging ?
t=15m   Wait for approval ??
t=??    Admin approves ?
t=20m   Deploy Production ?
t=20m   Create GitHub Release ?

Total: ~20 minutes + approval time
```

---

## ? **REMOVED WORKFLOWS:**

### **1. ci-cd.yml (REMOVED)**

**Why removed?**
- No PostgreSQL/Redis services
- Less comprehensive testing
- Duplicated effort
- Conflicts with ci.yml and cd.yml

**Better approach:** Separate CI and CD workflows with proper dependency.

---

### **2. code-quality.yml (REMOVED)**

**Why removed?**
- Duplicated code analysis (already in ci.yml)
- Duplicated build & test
- Required SonarCloud token (not setup)
- CodeQL can be added to ci.yml if needed

**What was lost:**
- ? SonarCloud integration (can add back if needed)
- ? Weekly scheduled runs (can add to ci.yml)
- ? Dependency license review (can add to ci.yml)
- ? Basic code analysis: **Still available in ci.yml**
- ? Security scanning: **Still available in ci.yml (Trivy)**

**What we kept:**
- ? Code analysis via `dotnet build /p:AnalysisMode=AllEnabledByDefault`
- ? Security scanning via Trivy
- ? Dependency vulnerability check via `dotnet list package --vulnerable`

---

## ?? **COMPARISON TABLE:**

| Workflow | Trigger | Purpose | Services | Dependency | Status |
|----------|---------|---------|----------|------------|--------|
| **ci.yml** | Push, PR | Build, Test, Quality | PostgreSQL, Redis | None | ? **ACTIVE** |
| **cd.yml** | CI success, Tags | Docker, Deploy | None | CI must pass | ? **ACTIVE** |
| ~~ci-cd.yml~~ | ~~Push, PR~~ | ~~Everything~~ | ~~None~~ | ~~None~~ | ? **REMOVED** |
| ~~code-quality.yml~~ | ~~Push, PR, Schedule~~ | ~~Code Quality~~ | ~~None~~ | ~~None~~ | ? **REMOVED** |

---

## ?? **BEST PRACTICES:**

### **1. Separation of Concerns:**
- ? CI: Build, test, quality checks
- ? CD: Docker build, deployment
- ? Don't mix CI and CD in one workflow

### **2. Clear Dependencies:**
```yaml
# CD depends on CI
on:
  workflow_run:
    workflows: ["CI - Build and Test"]
    types: [completed]
```

### **3. Conditional Execution:**
```yaml
# Only deploy from main branch
if: github.ref == 'refs/heads/main'
```

### **4. Avoid Duplication:**
- ? Multiple workflows doing same thing
- ? Consolidate related checks in one workflow
- ? Use job dependencies (`needs:`)

---

## ?? **DAILY WORKFLOW:**

### **Developer:**
```powershell
# 1. Create feature branch
git checkout -b feature/my-feature

# 2. Make changes
# ... code ...

# 3. Push and create PR
git push origin feature/my-feature
# Create PR on GitHub

# 4. CI runs automatically
# - Build ?
# - Tests ?
# - Code Quality ?
# - Security Scan ?

# 5. Merge PR after review
# GitHub merges to main

# 6. CI runs again on main
# CI passes ?

# 7. CD runs automatically
# - Build Docker ?
# - Deploy Staging ?
```

### **Release Manager:**
```powershell
# 1. Ensure main is stable
# Check CI/CD passing

# 2. Create release tag
git checkout main
git pull
git tag v1.0.0
git push origin v1.0.0

# 3. CD runs with production deployment
# - Build Docker (v1.0.0) ?
# - Deploy Staging ?
# - Wait for approval ??

# 4. Approve production deployment
# Click approve in GitHub

# 5. Production deployed ?
# GitHub Release created ?
```

---

## ? **SUMMARY:**

### **Current Structure (After Cleanup):**

1. **ci.yml** - Comprehensive CI pipeline
   - Builds, tests, quality checks
   - Security scanning (Trivy)
   - Runs on push & PR
   - Independent

2. **cd.yml** - Deployment pipeline
   - Waits for CI to pass
   - Builds Docker, deploys
   - Runs after CI success

3. ~~**ci-cd.yml**~~ - **REMOVED** (duplicate)

4. ~~**code-quality.yml**~~ - **REMOVED** (duplicate + requires SonarCloud token)

### **Benefits:**

? **Clear separation:** CI vs CD
? **No duplication:** Each workflow has specific role
? **Safe deployment:** CD only runs if CI passes
? **Flexible:** Manual override available
? **Efficient:** No wasted resources
? **Simple:** Only 2 workflows to maintain

---

## ?? **CONCLUSION:**

**B?n bây gi? có 2 workflows:**

1. **CI** (ci.yml) - Test everything
2. **CD** (cd.yml) - Deploy if CI passes

**Lu?ng:** Push ? CI ? (if success) ? CD ? Deploy

**Clean, simple, efficient!** ??

---

## ?? **OPTIONAL: Add SonarCloud Later**

If you need SonarCloud in the future:

```yaml
# Add to ci.yml security-scan job
- name: SonarCloud Scan
  uses: SonarSource/sonarcloud-github-action@master
  env:
    GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
    SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
```

**Requirements:**
1. Create SonarCloud account
2. Add `SONAR_TOKEN` to GitHub secrets
3. Configure sonar-project.properties
