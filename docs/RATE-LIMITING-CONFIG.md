# ?? RATE LIMITING CONFIGURATION

## ?? **Current Configuration (Development/Testing)**

The current rate limits are **INTENTIONALLY RELAXED** for development and load testing.

### **Active Limits:**

| Limiter | Requests/Minute | Queue Limit | Use Case |
|---------|----------------|-------------|----------|
| **auth** | 100 | 20 | Authentication endpoints (register, login) |
| **api** | 5000 (from config) | 20 | General API endpoints |
| **sensitive** | 100 | 20 | Money operations (transfer, withdraw) |
| **admin** | 200 | 50 | Admin operations |
| **global** | 1000 per IP | 50 | Fallback for all endpoints |

---

## ?? **PRODUCTION RECOMMENDATIONS**

**?? MUST REDUCE THESE BEFORE PRODUCTION DEPLOYMENT!**

### **Recommended Production Values:**

| Limiter | Development | Production | Reason |
|---------|------------|------------|--------|
| **auth** | 100/min | **10/min** | Prevent brute force attacks |
| **api** | 5000/min | **1000/min** | Normal user activity |
| **sensitive** | 100/min | **20/min** | Limit money operations |
| **admin** | 200/min | **30/min** | Admin operations are rare |
| **global** | 1000/min | **200/min** | Per-IP protection |

---

## ?? **How to Configure**

### **Method 1: Update RateLimitExtensions.cs (Code Change)**

**File:** `src/BankingSystem.API/Extensions/RateLimitExtensions.cs`

```csharp
// Auth endpoints
opt.PermitLimit = 10;  // Change from 100 ? 10
opt.QueueLimit = 2;    // Change from 20 ? 2

// Sensitive operations
opt.PermitLimit = 20;  // Change from 100 ? 20
opt.QueueLimit = 3;    // Change from 20 ? 3

// Admin operations
opt.PermitLimit = 30;  // Change from 200 ? 30
opt.QueueLimit = 5;    // Change from 50 ? 5

// Global limiter
PermitLimit = 200,     // Change from 1000 ? 200
QueueLimit = 10        // Change from 50 ? 10
```

### **Method 2: Environment-Based Configuration (Better)**

**Recommended approach:** Use different configs per environment.

**appsettings.Development.json:**
```json
{
  "RateLimitSettings": {
    "PermitLimit": 5000,
    "WindowInSeconds": 60,
    "QueueLimit": 20
  }
}
```

**appsettings.Production.json:**
```json
{
  "RateLimitSettings": {
    "PermitLimit": 1000,
    "WindowInSeconds": 60,
    "QueueLimit": 5
  }
}
```

**Then modify code to use environment:**

```csharp
var isProduction = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Production";

options.AddFixedWindowLimiter("auth", opt =>
{
    opt.PermitLimit = isProduction ? 10 : 100;
    opt.QueueLimit = isProduction ? 2 : 20;
    // ...
});
```

---

## ?? **Why These Limits?**

### **Auth (10 req/min in prod):**
- Normal user: Login once, maybe refresh token
- 10/min allows some retries but prevents brute force
- Attacker needs 6 minutes to try 60 passwords

### **Sensitive (20 req/min in prod):**
- Money operations are infrequent
- Normal user: 1-5 transactions per session
- 20/min allows batch operations but limits abuse

### **Admin (30 req/min in prod):**
- Admin operations are rare
- 30/min allows bulk operations
- High enough for legitimate use

### **Global (200 req/min in prod):**
- Covers all endpoints from single IP
- ~3.3 requests/second
- Prevents DDoS from single source

---

## ?? **Load Test Impact**

### **Development (Current):**
```
? Load tests pass with minimal rate limiting
? Can test high concurrency scenarios
? Easy to reproduce issues
```

### **Production (Recommended):**
```
??  Load tests will show ~50% rate limit errors
? This is EXPECTED and CORRECT behavior
? Protects against real attacks
```

---

## ?? **Migration Path**

### **Before Production:**

1. **Create appsettings.Production.json**
```json
{
  "RateLimitSettings": {
    "PermitLimit": 1000,
    "WindowInSeconds": 60,
    "QueueLimit": 5
  }
}
```

2. **Update RateLimitExtensions.cs**
```csharp
// Add environment check
var env = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
var isProduction = env == "Production";

// Auth limits
opt.PermitLimit = isProduction ? 10 : 100;
opt.QueueLimit = isProduction ? 2 : 20;
```

3. **Test in Staging**
```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run
# Test with production limits
```

4. **Deploy to Production**
```powershell
# Ensure environment is set
az webapp config appsettings set \
  --name banking-api \
  --settings ASPNETCORE_ENVIRONMENT=Production
```

---

## ?? **Testing Production Limits**

```powershell
# Set production mode
$env:ASPNETCORE_ENVIRONMENT="Production"

# Rebuild API
docker-compose build banking-api

# Restart
docker-compose up -d banking-api

# Run load test (expect ~50% rate limit errors)
k6 run performance-tests/load-test.js

# This is CORRECT behavior!
```

---

## ?? **Security Warning**

**Current development limits (100-1000 req/min) are TOO PERMISSIVE for production!**

**Risks with high limits:**
- Brute force attacks on auth endpoints
- DDoS attacks
- API abuse
- Excessive costs (database, bandwidth)

**Before going live:**
- [ ] Review all rate limits
- [ ] Test with production values
- [ ] Set up monitoring alerts
- [ ] Document for operations team

---

## ?? **Monitoring**

### **Metrics to Track:**

```
- Rate limit hits (429 responses)
- Auth endpoint failures
- Per-IP request rates
- Queue depths
```

### **Alerts to Configure:**

```
- Auth limit hit >5 times/hour ? Possible attack
- Global limit hit >100 times/hour ? Check for DDoS
- Single IP >80% of global limit ? Investigate
```

---

## ?? **References**

- **Code:** `src/BankingSystem.API/Extensions/RateLimitExtensions.cs`
- **Config:** `src/BankingSystem.API/appsettings.*.json`
- **Tests:** `performance-tests/load-test.js`
- **Docs:** `docs/DEPLOYMENT-GUIDE.md`

---

*Last Updated: 2024-12-08*  
*Status: Development configuration active*  
*Action Required: Update before production deployment*
