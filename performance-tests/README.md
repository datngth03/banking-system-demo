# Performance Testing

Load and performance tests for Banking System API using k6.

---

## Prerequisites

### Install k6

**Windows:**
```powershell
# Chocolatey
choco install k6

# Scoop
scoop install k6
```

**Linux/macOS:**
```bash
# macOS
brew install k6

# Ubuntu/Debian
sudo gpg --no-default-keyring --keyring /usr/share/keyrings/k6-archive-keyring.gpg \
  --keyserver hkp://keyserver.ubuntu.com:80 \
  --recv-keys C5AD17C747E3415A3642D57D77C6C491D6AC1D69

echo "deb [signed-by=/usr/share/keyrings/k6-archive-keyring.gpg] https://dl.k6.io/deb stable main" | \
  sudo tee /etc/apt/sources.list.d/k6.list

sudo apt-get update
sudo apt-get install k6
```

**Download:** https://k6.io/docs/get-started/installation/

---

## Test Files

| File | Type | Purpose |
|------|------|---------|
| `load-test.js` | Basic Load | Public endpoints (health, swagger) |
| `auth-load-test.js` | Authenticated Load | Protected endpoints (accounts, cards) |
| `test-workflow.ps1` | Automation | Complete test workflow with user seeding |

---

## Quick Start

### 1. Start API

```powershell
docker-compose up -d
```

### 2. Verify API

```bash
curl http://localhost:5000/health
```

### 3. Run Tests

**Basic load test:**
```bash
k6 run performance-tests/load-test.js
```

**Authenticated test (requires user seeding):**
```bash
# Run complete workflow (seeds users automatically)
.\performance-tests\test-workflow.ps1

# Or run k6 directly
k6 run performance-tests/auth-load-test.js
```

---

## Test Scenarios

### load-test.js

**Profile:**
- Ramp-up: 10 users over 30s
- Sustain: 10 users for 1 minute
- Peak: 50 users over 30s
- Sustain: 50 users for 1 minute
- Ramp-down: 0 users over 30s

**Thresholds:**
- 95% of requests < 500ms
- Error rate < 10%

**Endpoints:**
- `GET /health`
- `GET /swagger/index.html`

### auth-load-test.js

**Profile:**
- 10 concurrent users
- 1 minute duration

**Thresholds:**
- 95% of requests < 500ms
- Error rate < 30% (includes rate limiting)

**Endpoints:**
- `POST /api/v1/auth/login`
- `GET /api/cards/my-cards`
- `GET /api/accounts/my-accounts`

---

## Understanding Results

### Key Metrics

| Metric | Description | Target |
|--------|-------------|--------|
| `http_req_duration` | Request latency | p95 < 500ms |
| `http_req_failed` | Error rate | < 10% |
| `http_reqs` | Requests/second | > 100 req/s |
| `iterations` | Complete cycles | Varies |

### Example Output

```
checks.........................: 100.00% ? 2000   ? 0   
http_req_duration..............: avg=150ms  p95=400ms
http_req_failed................: 0.00%   ? 0      ? 2000
http_reqs......................: 2000    20/s
iterations.....................: 500     5/s
```

---

## Configuration

### Custom Load Profile

Edit `load-test.js`:

```javascript
export let options = {
  stages: [
    { duration: '1m', target: 20 },
    { duration: '3m', target: 50 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<300'],
    http_req_failed: ['rate<0.05'],
  },
};
```

### Environment Variables

```javascript
const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
```

Run with custom URL:
```bash
k6 run -e BASE_URL=https://api.example.com performance-tests/load-test.js
```

---

## Best Practices

**Before testing:**
1. Start with fresh API instance
2. Run warm-up test
3. Monitor system resources
4. Document baseline metrics

**During testing:**
- Monitor CPU/Memory usage
- Watch database connections
- Check application logs
- Observe response times

**After testing:**
- Document results
- Compare with baseline
- Identify bottlenecks
- Create improvement tasks

---

## Troubleshooting

### k6 not found

```bash
# Install k6 first
choco install k6
```

### Connection refused

```bash
# Verify API is running
docker-compose ps
curl http://localhost:5000/health
```

### High error rate

```bash
# Check API logs
docker-compose logs -f banking-api

# Check rate limiting (expected in tests)
# Error rate ~30% indicates rate limiting is working
```

### 401 Unauthorized

```bash
# Run test-workflow.ps1 to seed users first
.\performance-tests\test-workflow.ps1
```

---

## Resources

- [k6 Documentation](https://k6.io/docs/)
- [k6 Examples](https://k6.io/docs/examples/)
- [HTTP Authentication](https://k6.io/docs/examples/http-authentication/)
- [Thresholds](https://k6.io/docs/using-k6/thresholds/)
- [Metrics](https://k6.io/docs/using-k6/metrics/)

---

**Last updated:** December 2025
