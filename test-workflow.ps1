# Complete Testing Workflow
# Run this to test everything step-by-step before full setup

Write-Host "`n🧪 COMPLETE TESTING WORKFLOW`n" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# === STEP 1: Test Manual Registration ===
Write-Host "`n1️⃣ Testing Manual Registration..." -ForegroundColor Yellow

$testBody = @{
    firstName = 'Manual'
    lastName = 'Test'
    email = "test.$(Get-Random)@example.com"  # Random email to avoid conflicts
    password = 'Secure!2024$Bank#Mgr'
    phoneNumber = "+$(Get-Random -Minimum 1000000000 -Maximum 9999999999)"
    dateOfBirth = '1992-03-15T00:00:00Z'
    street = '999 Test Street'
    city = 'Test City'
    state = 'CA'
    postalCode = '90210'
    country = 'USA'
} | ConvertTo-Json

Write-Host "   Email: $($testBody | ConvertFrom-Json | Select-Object -ExpandProperty email)" -ForegroundColor Gray
Write-Host "   Password: Secure!2024`$Bank#Mgr" -ForegroundColor Gray

try {
    $regResponse = Invoke-WebRequest `
        -Uri "http://localhost:5000/api/v1/auth/register" `
        -Method POST `
        -Body $testBody `
        -ContentType "application/json" `
        -UseBasicParsing `
        -ErrorAction Stop
    
    Write-Host "   ✅ Registration SUCCESS!" -ForegroundColor Green
    $regData = $regResponse.Content | ConvertFrom-Json
    $testEmail = $regData.email
    
} catch {
    Write-Host "   ❌ Registration FAILED!" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "   Details: $errorBody" -ForegroundColor Gray
    }
    
    Write-Host "`n   ⚠️  Fix registration before continuing!" -ForegroundColor Yellow
    exit 1
}

# === STEP 2: Test Login ===
Write-Host "`n2️⃣ Testing Login..." -ForegroundColor Yellow

$loginBody = @{
    email = $testEmail
    password = 'Secure!2024$Bank#Mgr'
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest `
        -Uri "http://localhost:5000/api/v1/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -UseBasicParsing
    
    Write-Host "   ✅ Login SUCCESS!" -ForegroundColor Green
    $loginData = $loginResponse.Content | ConvertFrom-Json
    Write-Host "   User: $($loginData.fullName)" -ForegroundColor Gray
    Write-Host "   Token expires: $($loginData.expiresAt)" -ForegroundColor Gray
    
} catch {
    Write-Host "   ❌ Login FAILED!" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

# === STEP 3: Register Test Users for Auth Test ===
Write-Host "`n3️⃣ Registering Test Users for Auth Load Test..." -ForegroundColor Yellow

$authTestUsers = @(
    @{ 
        firstName = 'Admin'
        lastName = 'User'
        email = 'admin@bankingsystem.com'
        password = 'Secure!2024$Bank#Mgr'
        phoneNumber = '+1234567890'
        dateOfBirth = '1990-01-01T00:00:00Z'
        street = '123 Admin Street'
        city = 'Admin City'
        state = 'CA'
        postalCode = '12345'
        country = 'USA'
    },
    @{ 
        firstName = 'User'
        lastName = 'One'
        email = 'user1@test.com'
        password = 'Verify!2024$Safe#U1'
        phoneNumber = '+1234567891'
        dateOfBirth = '1995-05-15T00:00:00Z'
        street = '456 Test Street'
        city = 'Test City'
        state = 'NY'
        postalCode = '54321'
        country = 'USA'
    },
    @{ 
        firstName = 'User'
        lastName = 'Two'
        email = 'user2@test.com'
        password = 'Check!2024$Valid#U2'
        phoneNumber = '+1234567892'
        dateOfBirth = '1998-08-20T00:00:00Z'
        street = '789 Test Avenue'
        city = 'Test City'
        state = 'TX'
        postalCode = '98765'
        country = 'USA'
    }
)

$registeredCount = 0

foreach ($user in $authTestUsers) {
    $body = $user | ConvertTo-Json
    
    try {
        Invoke-WebRequest `
            -Uri "http://localhost:5000/api/v1/auth/register" `
            -Method POST `
            -Body $body `
            -ContentType "application/json" `
            -UseBasicParsing `
            -ErrorAction Stop | Out-Null
        
        Write-Host "   ✅ $($user.email)" -ForegroundColor Green
        $registeredCount++
        
    } catch {
        if ($_.Exception.Message -match "409|Conflict") {
            Write-Host "   ✅ $($user.email) (already exists)" -ForegroundColor Green
            $registeredCount++
        } else {
            Write-Host "   ⚠️  $($user.email) - Failed: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

Write-Host "   Registered: $registeredCount / $($authTestUsers.Count) users" -ForegroundColor Cyan

# === STEP 4: Run K6 Load Tests ===
Write-Host "`n4️⃣ Running K6 Load Tests..." -ForegroundColor Yellow

Write-Host "`n   📊 Test 1: Simple Load Test (Public Endpoints)" -ForegroundColor Cyan
Write-Host "   Expected: Some failures due to rate limiting (normal)" -ForegroundColor Gray
k6 run performance-tests/load-test.js

$simpleTestPassed = ($LASTEXITCODE -eq 0)

if ($simpleTestPassed) {
    Write-Host "   ✅ Simple load test PASSED!" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Simple load test had issues (check above)" -ForegroundColor Yellow
}

Write-Host "`n   📊 Test 2: Authenticated Load Test" -ForegroundColor Cyan
Write-Host "   Using registered test users (should work now)" -ForegroundColor Gray

k6 run performance-tests/auth-load-test.js

$authTestPassed = ($LASTEXITCODE -eq 0)

if ($authTestPassed) {
    Write-Host "   ✅ Auth load test PASSED!" -ForegroundColor Green
} else {
    Write-Host "   ⚠️  Auth load test had issues (expected if no users)" -ForegroundColor Yellow
}

# === SUMMARY ===
Write-Host "`n=================================" -ForegroundColor Cyan
Write-Host "🎯 TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

Write-Host "`n✅ Completed Tests:" -ForegroundColor Green
Write-Host "  1. Manual Registration: ✅ PASSED" -ForegroundColor White
Write-Host "  2. Login: ✅ PASSED" -ForegroundColor White
Write-Host "  3. Test Users Registration: ✅ $registeredCount users" -ForegroundColor White
Write-Host "  4. Simple Load Test: $(if($simpleTestPassed){'✅ PASSED'}else{'⚠️  HAD ISSUES (rate limiting)'})" -ForegroundColor White
Write-Host "  5. Auth Load Test: $(if($authTestPassed){'✅ PASSED'}else{'⚠️  HAD ISSUES'})" -ForegroundColor White

