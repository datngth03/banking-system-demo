# ?? Monitoring & Observability Guide

Complete guide for monitoring Banking System API across all environments.

---

## ?? Overview

The Banking System implements a **comprehensive observability stack**:

- **Metrics**: Prometheus + Grafana
- **Logs**: Serilog + Seq + Application Insights
- **Traces**: OpenTelemetry
- **Health Checks**: Built-in ASP.NET Core health checks

---

## ?? Table of Contents

1. [Environment Configuration](#environment-configuration)
2. [Metrics (Prometheus + Grafana)](#metrics-prometheus--grafana)
3. [Logging (Serilog + Seq)](#logging-serilog--seq)
4. [Distributed Tracing](#distributed-tracing)
5. [Health Checks](#health-checks)
6. [Alerts & Notifications](#alerts--notifications)
7. [Dashboards](#dashboards)
8. [Troubleshooting](#troubleshooting)

---

## ?? Environment Configuration

### Configuration Files by Environment

| Environment | Config File | Purpose |
|-------------|------------|---------|
| **Development** | `appsettings.Development.json` | Local development (F5) |
| **Docker** | `appsettings.Docker.json` | Docker Compose local |
| **Test** | `appsettings.Test.json` | CI/CD unit tests |
| **Staging** | `appsettings.Staging.json` | Pre-production testing |
| **Production** | `appsettings.Production.json` | Production deployment |

### Environment Variable Hierarchy

```
appsettings.json (base)
    ?
appsettings.{Environment}.json (overrides)
    ?
Environment Variables (overrides all)
    ?
Azure Key Vault / Secrets (production)
```

### Quick Setup by Environment

#### **Local Development (F5)**

```powershell
# No setup needed - uses appsettings.Development.json
dotnet run --project src/BankingSystem.API
```

**Monitoring URLs:**
- Seq: `http://localhost:5341`
- No Prometheus/Grafana (optional)

#### **Docker Local**

```powershell
# Start full stack
docker-compose --profile monitoring up -d

# Generate test logs
.\testlog.ps1
```

**Monitoring URLs:**
- Seq: `http://localhost:5341`
- Prometheus: `http://localhost:9090`
- Grafana: `http://localhost:3000` (admin/admin)
- API Metrics: `http://localhost:5000/metrics`

#### **CI/CD (GitHub Actions)**

Environment: `Test`

```yaml
# .github/workflows/ci.yml
env:
  ASPNETCORE_ENVIRONMENT: Test
  # No Seq/Prometheus - tests only
```

#### **Staging Deployment**

```powershell
# Set environment
$env:ASPNETCORE_ENVIRONMENT = "Staging"

# Set secrets (from Azure Key Vault)
$env:ConnectionStrings__DefaultConnection = "@Microsoft.KeyVault(...)"
$env:JwtSettings__Secret = "@Microsoft.KeyVault(...)"
$env:Logging__Seq__ApiKey = "@Microsoft.KeyVault(...)"

# Deploy
docker-compose -f docker-compose.staging.yml up -d
```

#### **Production Deployment**

```powershell
# Deploy with production config
docker-compose -f docker-compose.production.yml up -d
```

---

## ?? Metrics (Prometheus + Grafana)

### Available Metrics

#### **Application Metrics**
- `banking_users_active` - Current active users
- `banking_accounts_total` - Total accounts
- `banking_transactions_pending` - Pending transactions
- `http_server_request_duration_seconds` - Request latency
- `http_server_active_requests` - Active requests

#### **.NET Runtime Metrics**
- `process_runtime_dotnet_gc_collections_count_total` - GC collections
- `process_runtime_dotnet_gc_heap_size_bytes` - Heap size
- `process_runtime_dotnet_thread_pool_threads_count` - Thread pool
- `process_runtime_dotnet_exceptions_count_total` - Exceptions

#### **Kestrel Metrics**
- `kestrel_active_connections` - Active connections
- `kestrel_queued_connections` - Queued connections

### Prometheus Setup

**Config:** `monitoring/prometheus.yml`

```yaml
scrape_configs:
  - job_name: 'banking-api'
    static_configs:
      - targets: ['banking-api:80']
    metrics_path: '/metrics'
    scrape_interval: 10s
```

**Query Examples:**

```promql
# Request rate
rate(http_server_request_duration_seconds_count[1m])

# Error rate
rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[1m])

# P95 latency
histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))

# Memory usage
process_runtime_dotnet_gc_heap_size_bytes

# Active users
banking_users_active
```

### Grafana Dashboards

**Pre-built dashboard:** `monitoring/grafana-dashboard.json`

**Panels:**
1. HTTP Request Rate
2. Active Connections
3. Active Users
4. GC Collections
5. Memory Usage
6. Request Duration (P95)

**Import:**
1. Open Grafana: `http://localhost:3000`
2. Login: `admin/admin`
3. **Dashboards** ? **Import**
4. Upload `monitoring/grafana-dashboard.json`

---

## ?? Logging (Serilog + Seq)

### Log Levels by Environment

| Environment | Console | File | Seq | App Insights |
|-------------|---------|------|-----|--------------|
| **Development** | Info | Warning | Info | - |
| **Docker** | Info | Warning | Info | - |
| **Test** | Info | - | - | - |
| **Staging** | Warning | Warning | Info | Warning |
| **Production** | Warning | Warning | Warning | Warning |

### Seq Configuration

#### **Development**
```json
{
  "Logging": {
    "Seq": {
      "Url": "http://localhost:5341"
    }
  }
}
```

#### **Docker**
```json
{
  "Logging": {
    "Seq": {
      "Url": "http://seq"
    }
  }
}
```

#### **Production**
```json
{
  "Logging": {
    "Seq": {
      "Url": "https://seq.yourcompany.com",
      "ApiKey": ""  # From Azure Key Vault
    }
  }
}
```

### Useful Seq Queries

```sql
-- All errors
@Level = 'Error'

-- Specific endpoint
@Message like '%/api/accounts%'

-- Slow requests
Duration > 1000 and @Message like '%Request finished%'

-- Failed logins
@Message like '%authentication%' and @Level = 'Warning'

-- By user
UserId = 'abc-123'

-- Last hour errors
@Level = 'Error' and @Timestamp > DateTime('Now').AddHours(-1)
```

### Log Enrichment

All logs include:
- `MachineName` - Server hostname
- `EnvironmentName` - Development/Staging/Production
- `ThreadId` - Thread ID
- `Application` - "BankingSystem.API"
- `Version` - Application version

---

## ?? Distributed Tracing

### OpenTelemetry Configuration

**Instrumented:**
- ? ASP.NET Core (HTTP requests)
- ? HTTP Client (outbound calls)
- ? Entity Framework Core (database queries)

**Export:**
- Console (Development)
- Future: Jaeger/Zipkin (Production)

### Trace Context Propagation

All HTTP requests include:
- `traceparent` header
- `tracestate` header
- Correlation ID

---

## ?? Health Checks

### Endpoints

| Endpoint | Purpose | Checks |
|----------|---------|--------|
| `/health` | Overall health | All subsystems |
| `/health/ready` | Readiness probe | Database, Redis, Hangfire |
| `/health/live` | Liveness probe | Process is alive |

### Health Check Components

- ? Database (PostgreSQL)
- ? Redis Cache
- ? Hangfire Background Jobs

### Response Format

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0234567",
  "entries": {
    "postgres": {
      "status": "Healthy",
      "duration": "00:00:00.0123456"
    },
    "redis": {
      "status": "Healthy",
      "duration": "00:00:00.0023456"
    }
  }
}
```

---

## ?? Alerts & Notifications

### Recommended Alerts

#### **Seq Alerts**

1. **High Error Rate**
   - Query: `@Level = 'Error'`
   - Threshold: > 10 in 5 minutes
   - Action: Send email/Slack

2. **Failed Logins**
   - Query: `@Message like '%Failed login%'`
   - Threshold: > 5 in 1 minute
   - Action: Security alert

3. **Slow Queries**
   - Query: `Duration > 5000 and @Message like '%database%'`
   - Threshold: > 3 in 5 minutes
   - Action: Performance alert

#### **Prometheus Alerts**

**Config:** `monitoring/prometheus-alerts.yml`

```yaml
groups:
  - name: banking_api
    interval: 30s
    rules:
      - alert: HighErrorRate
        expr: rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m]) > 0.1
        for: 2m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"

      - alert: HighMemoryUsage
        expr: process_runtime_dotnet_gc_heap_size_bytes > 500000000
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "Memory usage above 500MB"
```

---

## ?? Dashboards

### Grafana Dashboard Panels

1. **Overview**
   - Active users
   - Request rate
   - Error rate
   - P95 latency

2. **Performance**
   - Request duration histogram
   - Database query time
   - Cache hit rate

3. **Resources**
   - CPU usage
   - Memory usage
   - GC metrics
   - Thread pool

4. **Business Metrics**
   - Total accounts
   - Active users
   - Pending transactions
   - Transaction volume

### Seq Dashboard

Create signals for:
- Error trends
- Request volume
- Endpoint usage
- User activity

---

## ?? Troubleshooting

### No Logs in Seq

**Check:**
```powershell
# 1. Seq running?
docker ps --filter "name=seq"

# 2. API configured?
docker exec bankingsystem-api printenv | Select-String "Seq"

# 3. API can reach Seq?
docker exec bankingsystem-api wget -O- http://seq

# 4. Generate traffic
.\testlog.ps1
```

### Prometheus Not Scraping

**Check:**
```powershell
# 1. Metrics endpoint working?
Invoke-WebRequest http://localhost:5000/metrics

# 2. Prometheus config
docker exec bankingsystem-prometheus cat /etc/prometheus/prometheus.yml

# 3. Prometheus targets
start http://localhost:9090/targets
```

### Grafana No Data

**Check:**
```powershell
# 1. Datasource configured?
start http://localhost:3000/datasources

# 2. Test connection
# Click datasource ? Save & Test

# 3. Prometheus has data?
start http://localhost:9090/graph
```

---

## ?? Best Practices

### Development
- ? Use Seq for log viewing
- ? Debug level logging
- ? Console output enabled

### Staging
- ? Production-like monitoring
- ? Test alerts & dashboards
- ? Validate integrations

### Production
- ? Warning+ level logging only
- ? Centralized logging (Seq + App Insights)
- ? Alerts configured
- ? Dashboards monitored
- ? Secrets in Key Vault
- ? Log retention policies

---

## ?? Resources

- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Seq Documentation](https://docs.datalust.co/docs)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Serilog Best Practices](https://github.com/serilog/serilog/wiki/Configuration-Basics)

---

## ?? Quick Start Commands

```powershell
# Local development
dotnet run --project src/BankingSystem.API

# Docker full stack
docker-compose --profile monitoring up -d

# Generate logs
.\testlog.ps1

# Setup monitoring
.\setup-monitoring.ps1

# View logs
start http://localhost:5341

# View metrics
start http://localhost:9090

# View dashboards
start http://localhost:3000
```

---

**Last Updated:** December 2024
**Maintained by:** Banking System Team
