# ?? Performance Testing

Load and performance tests for Banking System API using [k6](https://k6.io/).

---

## ?? Prerequisites

### Install k6

**Windows (Chocolatey):**
```powershell
choco install k6
```

**Windows (Scoop):**
```powershell
scoop install k6
```

**Linux/Mac:**
```bash
# macOS
brew install k6

# Linux
sudo gpg -k
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69
echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | sudo tee /etc/apt/sources.list.d/k6.list
sudo apt-get update
sudo apt-get install k6
```

**Or download from:** https://k6.io/docs/get-started/installation/

---

## ?? Test Files

| File | Type | Purpose | Prerequisites |
|------|------|---------|---------------|
| `load-test.js` | Load Test | Test public endpoints (health, swagger) | API running |
| `auth-load-test.js` | Auth Test | Test authenticated endpoints | API + test users seeded |
| `stress-test.js` | Stress Test | *(TODO)* Find breaking point | API running |
| `spike-test.js` | Spike Test | *(TODO)* Test sudden traffic increase | API running |

---

## ?? Running Tests

### **Prerequisites:**
```powershell
# 1. Start the API
.\start-dev.ps1

# 2. Verify API is running
curl http://localhost:5000/health
```

### **Run Load Test:**
```powershell
# Basic run
k6 run performance-tests/load-test.js

# With custom settings
k6 run --vus 50 --duration 2m performance-tests/load-test.js

# Save results
k6 run --out json=results.json performance-tests/load-test.js

# With HTML report
k6 run --out web-dashboard performance-tests/load-test.js
```

---

## ?? Test Scenarios

### **Load Test (load-test.js)**

**Profile:**
- **Ramp-up:** 10 users over 30s
- **Sustain:** 10 users for 1 minute
- **Peak:** 50 users over 30s
- **Sustain:** 50 users for 1 minute
- **Ramp-down:** 0 users over 30s

**Thresholds:**
- 95% of requests < 500ms
- Error rate < 10%

**Tests:**
1. Login (`POST /api/auth/login`)
2. Get user cards (`GET /api/cards/my-cards`)
3. Get user accounts (`GET /api/accounts/my-accounts`)

---

## ?? Understanding Results

### **Metrics:**

| Metric | Description | Good Target |
|--------|-------------|-------------|
| `http_req_duration` | Request latency | p95 < 500ms |
| `http_req_failed` | Error rate | < 10% |
| `http_reqs` | Requests per second | > 100 req/s |
| `iterations` | Complete test cycles | Depends on scenario |

### **Example Output:**
```
     ? login status is 200
     ? login response time < 500ms
     ? cards status is 200
     ? cards response time < 500ms

     checks.........................: 100.00% ? 2000      ? 0   
     data_received..................: 1.5 MB  15 kB/s
     data_sent......................: 450 kB  4.5 kB/s
     http_req_blocked...............: avg=1.2ms    min=0s     med=0s      max=50ms  
     http_req_duration..............: avg=150ms    min=50ms   med=120ms   max=800ms 
       { expected_response:true }...: avg=150ms    min=50ms   med=120ms   max=800ms 
     http_req_failed................: 0.00%   ? 0         ? 2000
     http_req_receiving.............: avg=1ms      min=0s     med=0.5ms   max=10ms  
     http_reqs......................: 2000    20/s
     iteration_duration.............: avg=1.2s     min=1s     med=1.15s   max=2s    
     iterations.....................: 500     5/s
     vus............................: 1       min=1       max=50
```

---

## ?? Test Configuration

### **Modify Load Test:**

Edit `load-test.js`:

```javascript
export let options = {
  stages: [
    { duration: '1m', target: 20 },  // Increase to 20 users
    { duration: '3m', target: 50 },  // Stay at 50 for 3 minutes
  ],
  thresholds: {
    http_req_duration: ['p(95)<300'], // Stricter: 300ms
    http_req_failed: ['rate<0.05'],   // Stricter: 5% error rate
  },
};
```

### **Environment Variables:**

```javascript
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
```

Run with:
```powershell
k6 run -e BASE_URL=https://staging-api.com performance-tests/load-test.js
```

---

## ?? Best Practices

### **Before Running Tests:**

1. ? **Start fresh:** Restart API and database
2. ? **Warm-up:** Run a small test first
3. ? **Monitor:** Watch Grafana/Prometheus during test
4. ? **Baseline:** Record results for comparison

### **During Tests:**

1. ?? Monitor CPU/Memory usage
2. ?? Watch database connections
3. ?? Check logs for errors
4. ?? Observe response times

### **After Tests:**

1. ?? Document results
2. ?? Compare with previous runs
3. ?? Identify bottlenecks
4. ?? Create improvement tasks

---

## ?? Additional Resources

- [k6 Documentation](https://k6.io/docs/)
- [k6 Examples](https://k6.io/docs/examples/)
- [HTTP Authentication](https://k6.io/docs/examples/http-authentication/)
- [Thresholds](https://k6.io/docs/using-k6/thresholds/)
- [Metrics](https://k6.io/docs/using-k6/metrics/)

---

## ?? Troubleshooting

### **k6 not found:**
```powershell
# Install k6
choco install k6
# Or download from https://k6.io/
```

### **Connection refused:**
```powershell
# Make sure API is running
docker-compose ps
curl http://localhost:5000/health
```

### **401 Unauthorized:**
```javascript
// Check login credentials in load-test.js
// Default: test@example.com / TestPassword123!
```

### **High error rate:**
```powershell
# Check API logs
docker-compose logs -f banking-api

# Check database connections
docker stats
```

---

## ?? Next Steps

1. **Create test data:** Seed database with test users
2. **Add more tests:** Create stress-test.js, spike-test.js
3. **CI/CD integration:** Add performance tests to pipeline
4. **Set benchmarks:** Document acceptable performance metrics
5. **Regular testing:** Schedule weekly performance tests

---

**Happy Load Testing!** ??
