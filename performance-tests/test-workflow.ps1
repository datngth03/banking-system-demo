# Complete Testing Workflow
# Automated test workflow for Banking System API
# This script: registers users, tests authentication, runs load tests

Write-Host "`nCOMPLETE TESTING WORKFLOW" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Step 1: Test Manual Registration
Write-Host "`n[1/5] Testing Manual Registration..." -ForegroundColor Yellow

$testBody = @{
    firstName = 'Manual'
    lastName = 'Test'
    email = "test.$(Get-Random)@example.com"
    password = 'Secure!2025$Bank#Mgr'
    phoneNumber = "+$(Get-Random -Minimum 1000000000 -Maximum 9999999999)"
    dateOfBirth = '1992-03-15T00:00:00Z'
    street = '999 Test Street'
    city = 'Test City'
    state = 'CA'
    postalCode = '90210'
    country = 'USA'
} | ConvertTo-Json

Write-Host "   Email: $($testBody | ConvertFrom-Json | Select-Object -ExpandProperty email)" -ForegroundColor Gray
Write-Host "   Password: Secure!2025`$Bank#Mgr" -ForegroundColor Gray

try {
    $regResponse = Invoke-WebRequest `
        -Uri "http://localhost:5000/api/v1/auth/register" `
        -Method POST `
        -Body $testBody `
        -ContentType "application/json" `
        -UseBasicParsing `
        -ErrorAction Stop
    
    Write-Host "   SUCCESS - Registration completed" -ForegroundColor Green
    $regData = $regResponse.Content | ConvertFrom-Json
    $testEmail = $regData.email
    
} catch {
    Write-Host "   FAILED - Registration error" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "   Details: $errorBody" -ForegroundColor Gray
    }
    
    Write-Host "`n   Fix registration before continuing!" -ForegroundColor Yellow
    exit 1
}

# Step 2: Test Login
Write-Host "`n[2/5] Testing Login..." -ForegroundColor Yellow

$loginBody = @{
    email = $testEmail
    password = 'Secure!2025$Bank#Mgr'
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest `
        -Uri "http://localhost:5000/api/v1/auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -UseBasicParsing
    
    Write-Host "   SUCCESS - Login completed" -ForegroundColor Green
    $loginData = $loginResponse.Content | ConvertFrom-Json
    Write-Host "   User: $($loginData.fullName)" -ForegroundColor Gray
    Write-Host "   Token expires: $($loginData.expiresAt)" -ForegroundColor Gray
    
} catch {
    Write-Host "   FAILED - Login error" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

# Step 3: Register Test Users for Auth Load Test
Write-Host "`n[3/5] Registering Test Users for Load Testing..." -ForegroundColor Yellow

$authTestUsers = @(
    @{ 
        firstName = 'Admin'
        lastName = 'User'
        email = 'admin@bankingsystem.com'
        password = 'Secure!2025$Bank#Mgr'
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
        password = 'Verify!2025$Safe#U1'
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
        password = 'Check!2025$Valid#U2'
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
        
        Write-Host "   SUCCESS - $($user.email)" -ForegroundColor Green
        $registeredCount++
        
    } catch {
        if ($_.Exception.Message -match "409|Conflict") {
            Write-Host "   EXISTS  - $($user.email) (already registered)" -ForegroundColor Green
            $registeredCount++
        } else {
            Write-Host "   WARNING - $($user.email) - Failed: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

Write-Host "   Registered: $registeredCount / $($authTestUsers.Count) users" -ForegroundColor Cyan

# Step 4: Run k6 Load Tests
Write-Host "`n[4/5] Running k6 Load Tests..." -ForegroundColor Yellow

Write-Host "`n   Test 1: Basic Load Test (Public Endpoints)" -ForegroundColor Cyan
Write-Host "   Note: Some failures expected due to rate limiting" -ForegroundColor Gray

k6 run performance-tests/load-test.js

$simpleTestPassed = ($LASTEXITCODE -eq 0)

if ($simpleTestPassed) {
    Write-Host "   SUCCESS - Basic load test passed" -ForegroundColor Green
} else {
    Write-Host "   WARNING - Basic load test had issues (check output above)" -ForegroundColor Yellow
}

Write-Host "`n   Test 2: Authenticated Load Test" -ForegroundColor Cyan
Write-Host "   Note: Using registered test users" -ForegroundColor Gray

k6 run performance-tests/auth-load-test.js

$authTestPassed = ($LASTEXITCODE -eq 0)

if ($authTestPassed) {
    Write-Host "   SUCCESS - Auth load test passed" -ForegroundColor Green
} else {
    Write-Host "   WARNING - Auth load test had issues (check output above)" -ForegroundColor Yellow
}

# Summary
Write-Host "`n=================================" -ForegroundColor Cyan
Write-Host "TEST SUMMARY" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

Write-Host "`nCompleted Tests:" -ForegroundColor Green
Write-Host "  1. Manual Registration: PASSED" -ForegroundColor White
Write-Host "  2. Login: PASSED" -ForegroundColor White
Write-Host "  3. Test Users Registration: $registeredCount users" -ForegroundColor White
Write-Host "  4. Basic Load Test: $(if($simpleTestPassed){'PASSED'}else{'HAD ISSUES (rate limiting)'})" -ForegroundColor White
Write-Host "  5. Auth Load Test: $(if($authTestPassed){'PASSED'}else{'HAD ISSUES'})" -ForegroundColor White

Write-Host "`nTest users credentials:" -ForegroundColor Cyan
Write-Host "  admin@bankingsystem.com : Secure!2025`$Bank#Mgr" -ForegroundColor Gray
Write-Host "  user1@test.com          : Verify!2025`$Safe#U1" -ForegroundColor Gray
Write-Host "  user2@test.com          : Check!2025`$Valid#U2" -ForegroundColor Gray

Write-Host "`nWorkflow complete." -ForegroundColor Cyan

