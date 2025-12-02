# ?? CI/CD Workflow Dependency - Explained

## ?? **TR??C KHI FIX:**

### **Problem: Workflows ??c l?p**

```yaml
# CI Workflow
on:
  push:
    branches: [ main ]  # ? Trigger 1

# CD Workflow  
on:
  push:
    branches: [ main ]  # ? Trigger 2 (??c l?p!)
```

**K?t qu?:**

```
Push to main
    ?
    ?? CI ch?y ? ho?c ?
    ?? CD ch?y ? (không quan tâm CI)
    
?? CD có th? deploy code b? l?i!
```

---

## ? **SAU KHI FIX:**

### **Solution: workflow_run dependency**

```yaml
# CD Workflow (FIXED)
on:
  workflow_run:
    workflows: ["CI - Build and Test"]  # ? ??i CI workflow
    types:
      - completed  # ? Ch? ch?y khi CI completed
    branches:
      - main
```

**K?t qu?:**

```
Push to main
    ?
CI ch?y
    ?? Success ? ? CD ch?y ?
    ?? Failed ? ? CD KHÔNG ch?y ?
    
? Ch? deploy khi CI pass!
```

---

## ?? **CHI TI?T IMPLEMENTATION:**

### **Step 1: CD workflow trigger**

```yaml
on:
  workflow_run:
    workflows: ["CI - Build and Test"]  # ? Tên chính xác c?a CI workflow
    types:
      - completed  # ? Khi CI completed (success/failure/cancelled)
    branches:
      - main  # ? Ch? cho main branch
```

### **Step 2: Check CI status**

```yaml
jobs:
  check-ci-status:
    runs-on: ubuntu-latest
    if: github.event_name == 'workflow_run'  # ? Ch? khi trigger t? workflow_run
    steps:
      - name: Check CI workflow result
        if: github.event.workflow_run.conclusion != 'success'
        run: |
          echo "CI workflow failed!"
          echo "Conclusion: ${{ github.event.workflow_run.conclusion }}"
          exit 1  # ? Fail job n?u CI không success
```

### **Step 3: Build Docker (ch? khi CI pass)**

```yaml
  build-docker-image:
    needs: [check-ci-status]  # ? Ph? thu?c vào check-ci-status
    if: |
      always() && 
      (needs.check-ci-status.result == 'success' ||  # ? CI passed
       github.event_name == 'workflow_dispatch' ||    # ? Manual trigger
       github.event_name == 'push')                   # ? Tag push
```

---

## ?? **WORKFLOW EXECUTION:**

### **Scenario 1: CI Success**

```
t=0m    Push to main
t=0m    CI workflow starts
t=5m    CI completes: ? Success
        ?
t=5m    CD workflow triggered (workflow_run)
t=5m    check-ci-status: ? Pass
t=5m    build-docker-image starts
t=15m   CD completes: ? Success

Result: ? Code deployed
```

### **Scenario 2: CI Failed**

```
t=0m    Push to main
t=0m    CI workflow starts
t=3m    CI completes: ? Failed (build error)
        ?
t=3m    CD workflow triggered (workflow_run)
t=3m    check-ci-status: ? Fail
        ?? Exit 1 (job failed)
t=3m    build-docker-image: ?? Skipped

Result: ? No deployment (safe!)
```

### **Scenario 3: Manual Trigger**

```
User clicks "Run workflow" button
    ?
CD workflow starts (workflow_dispatch)
    ?
check-ci-status: ?? Skipped (not workflow_run)
    ?
build-docker-image: ? Run
    ?
Result: ? Deploy (manual override)
```

### **Scenario 4: Tag Push**

```
git tag v1.0.0
git push origin v1.0.0
    ?
CD workflow starts (push tag)
    ?
check-ci-status: ?? Skipped (not workflow_run)
    ?
build-docker-image: ? Run
    ?
Result: ? Production deployment
```

---

## ?? **WORKFLOW DEPENDENCY DIAGRAM:**

```
???????????????????????????????????????
?      Push to main branch            ?
???????????????????????????????????????
                ?
???????????????????????????????????????
?      CI Workflow Starts             ?
???????????????????????????????????????
?  1. Build & Test                    ?
?  2. Code Analysis                   ?
?  3. Security Scan                   ?
???????????????????????????????????????
                ?
        ??????????????????
        ?                ?
    ? Success       ? Failed
        ?                ?
?????????????????   ????????????????
? CD Triggered  ?   ? CD BLOCKED   ?
? (workflow_run)?   ? (no trigger) ?
?????????????????   ????????????????
        ?                ?
?????????????????   ????????????????
? Check Status  ?   ?   STOP ?   ?
? ? Pass       ?   ?              ?
?????????????????   ????????????????
        ?
?????????????????
? Build Docker  ?
? Deploy        ?
?????????????????
```

---

## ?? **KEY POINTS:**

### **1. workflow_run Event:**
```yaml
github.event_name == 'workflow_run'
github.event.workflow_run.conclusion  # success, failure, cancelled
github.event.workflow_run.name        # "CI - Build and Test"
```

### **2. Multiple Triggers:**
CD có 3 triggers:
1. **workflow_run** - Sau khi CI complete (ch? deploy n?u success)
2. **workflow_dispatch** - Manual (bypass CI check)
3. **push tags** - Tag push (bypass CI check)

### **3. Conditional Logic:**
```yaml
if: |
  always() &&  # ? Luôn ch?y job này (k? c? khi previous job skipped)
  (needs.check-ci-status.result == 'success' ||  # ? CI passed
   github.event_name == 'workflow_dispatch' ||    # ? Manual
   github.event_name == 'push')                   # ? Tag
```

---

## ?? **TESTING:**

### **Test CI Success ? CD Runs:**
```powershell
# Make a change
echo "test" >> README.md
git add .
git commit -m "Test CI/CD dependency"
git push origin main

# Watch:
# 1. CI runs and passes ?
# 2. CD triggers automatically ?
# 3. CD checks CI status ?
# 4. CD deploys ?
```

### **Test CI Failure ? CD Blocked:**
```powershell
# Introduce a build error
# (e.g., syntax error in code)
git push origin main

# Watch:
# 1. CI runs and fails ?
# 2. CD triggers ?
# 3. CD checks CI status ?
# 4. CD stops (no deployment) ?
```

### **Test Manual Override:**
```powershell
# Go to GitHub Actions tab
# Click "CD - Build and Deploy"
# Click "Run workflow"
# Select environment
# Click "Run workflow" button

# Watch:
# - CD runs immediately (no CI check)
# - Deploys regardless of CI status
```

---

## ? **BENEFITS:**

1. **Safety:** Không deploy code b? l?i
2. **Flexibility:** Manual override khi c?n
3. **Automation:** T? ??ng deploy khi CI pass
4. **Transparency:** Rõ ràng why CD ch?y hay không

---

## ?? **IMPORTANT NOTES:**

### **Workflow Name Must Match:**
```yaml
# CI Workflow
name: CI - Build and Test  # ? Tên này

# CD Workflow
on:
  workflow_run:
    workflows: ["CI - Build and Test"]  # ? Ph?i gi?ng tên trên!
```

### **Branch Must Match:**
```yaml
on:
  workflow_run:
    branches:
      - main  # ? Ch? main branch
```

N?u push vào `develop`:
- CI ch?y ?
- CD KHÔNG ch?y ? (branch không match)

---

## ?? **COMMIT MESSAGE:**

```powershell
git add .github/workflows/cd.yml
git commit -m "Add CI/CD workflow dependency

- CD now waits for CI to complete successfully
- Uses workflow_run trigger
- Checks CI conclusion before deployment
- Manual and tag triggers still work
- Prevents deploying broken code

This ensures CD only runs after CI passes!"

git push origin main
```

---

## ?? **RESULT:**

```
TR??C:
- CI và CD ??c l?p
- CD có th? deploy code l?i ?

SAU:
- CD ph? thu?c vào CI
- CD ch? ch?y khi CI success ?
- Safe deployment! ??
```

---

**GitHub H? TR? qua `workflow_run` event!** ??
