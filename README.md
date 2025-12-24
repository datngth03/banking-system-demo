# 🏦 Banking System - Enterprise-Grade .NET 8 Application

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?logo=docker)](https://www.docker.com/)
[![CI](https://github.com/datngth03/banking-system-demo/actions/workflows/ci.yml/badge.svg)](https://github.com/datngth03/banking-system-demo/actions/workflows/ci.yml)

A production-ready banking system built with .NET 8, implementing Clean Architecture, CQRS pattern, and modern DevOps practices.

---

## 🚀 Quick Start

```bash
# Clone repository
git clone https://github.com/datngth03/banking-system-demo
cd banking-system-demo

# Start core services only (API + databases)
docker-compose up -d

# OR start with full monitoring stack
docker-compose --profile full up -d

# Access application
# API: http://localhost:5000
# Swagger: http://localhost:5000/swagger
# Grafana: http://localhost:3000 (admin/admin)
# Prometheus: http://localhost:9090
# Seq: http://localhost:5341
# PgAdmin: http://localhost:5050 (admin@bankingsystem.com/admin)
```

**Note:** Database migrations run automatically on first startup.

**Monitoring Services:** Use `--profile full` to start Grafana, Prometheus, Seq, and PgAdmin along with core services.

---

## 📋 Overview

### Core Features

**Banking Operations:**
- User management with role-based access control (User, Admin, Manager, Support)
- Multi-currency account management
- Transaction processing (deposit, withdraw, transfer)
- Card management (Visa, Mastercard)
- Bill payments with scheduling
- Real-time notifications
- Complete audit logging

**Technical Implementation:**
- Clean Architecture with CQRS pattern
- JWT authentication with refresh tokens
- AES-256 encryption for sensitive data
- Redis caching with 75-90% hit rate
- Rate limiting (auth, API, sensitive operations)
- Background job processing (Hangfire)
- Structured logging (Serilog + Seq)
- Metrics collection (Prometheus + Grafana)

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────┐
│         Banking System API              │
│          (.NET 8 Web API)               │
└──────────────┬──────────────────────────┘
               │
     ┌─────────┴─────────┐
     │                   │
┌────▼──────┐   ┌────────▼─────────┐
│Application│   │Infrastructure    │
│  (CQRS)   │   │(EF Core, Redis)  │
│ Commands  │   │  - PostgreSQL    │
│ Queries   │   │  - Hangfire      │
│ Handlers  │   │  - Services      │
└────┬──────┘   └────────┬─────────┘
     │                   │
     └─────────┬─────────┘
               │
        ┌──────▼──────┐
        │   Domain    │
        │  Entities   │
        │ Value Obj   │
        │ Interfaces  │
        └─────────────┘
```

**Stack:**
- **Backend:** .NET 8, ASP.NET Core Web API
- **Database:** PostgreSQL 16 (Business + Hangfire)
- **Cache:** Redis 7
- **Monitoring:** Prometheus, Grafana, Seq, Application Insights
- **Background Jobs:** Hangfire
- **Patterns:** Clean Architecture, CQRS, MediatR, Repository

---

## 📁 Project Structure

```
banking-system-demo/
│
├── src/                                 # Source code
│   ├── BankingSystem.API/              # API layer (Controllers, Middleware)
│   ├── BankingSystem.Application/      # Business logic (CQRS, Handlers)
│   ├── BankingSystem.Domain/           # Domain models (Entities, Value Objects)
│   └── BankingSystem.Infrastructure/   # Infrastructure (Data Access, Services)
│
├── tests/                               # Test projects
│   ├── BankingSystem.Tests/            # Unit tests
│   └── BankingSystem.IntegrationTests/ # Integration tests
│
├── docs/                                # Documentation
│   ├── AZURE-DEPLOYMENT.md             # Azure deployment guide
│   └── WORKFLOW-ARCHITECTURE.md        # CI/CD pipeline documentation
│
├── azure/                               # Azure infrastructure
│   ├── bicep/                          # Bicep templates (IaC)
│   ├── scripts/                        # Deployment scripts
│   └── appsettings/                    # Azure-specific configs
│
├── .github/workflows/                   # CI/CD pipelines
│   ├── ci.yml                          # Build & test
│   └── cd.yml                          # Deploy
│
├── k8s/                                 # Kubernetes manifests
├── monitoring/                          # Monitoring configs
├── performance-tests/                   # k6 load tests & test workflow
│
├── docker-compose.yml                   # Local development
└── README.md                            # This file
```

---

## 🛠️ Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [k6](https://k6.io/) (optional, for load testing)

### Local Setup

**Option 1: Full Docker Stack (with monitoring)**
```bash
# Start all services including monitoring
docker-compose --profile full up -d
```

**Option 2: Core Services Only**
```bash
# Start only API + databases (faster startup)
docker-compose up -d
```

**Option 3: Local API + Docker Infrastructure**
```bash
# Start databases only
docker-compose up -d postgres-business postgres-hangfire redis

# Run API locally
cd src/BankingSystem.API
dotnet run
```

**Available Profiles:**
- `default` - Core services (API, PostgreSQL, Redis)
- `monitoring` - Adds Seq, Prometheus, Grafana
- `tools` - Adds PgAdmin
- `full` - All services (monitoring + tools)

### Configuration

**Development:**
- Connection strings in `appsettings.Development.json`
- Rate limits: 5000 req/min (relaxed for testing)
- Auto-migrations enabled

**Production:**
- Secrets in Azure Key Vault
- Rate limits: 1000 req/min
- HTTPS enforced
- Connection pooling optimized

---

## 📊 API Documentation

### Endpoints

| Category | Endpoint | Method | Auth |
|----------|----------|--------|------|
| **Auth** | `/api/v1/auth/register` | POST | No |
| | `/api/v1/auth/login` | POST | No |
| | `/api/v1/auth/refresh` | POST | No |
| **Accounts** | `/api/accounts/my-accounts` | GET | Yes |
| | `/api/accounts/{id}/deposit` | POST | Yes |
| | `/api/accounts/transfer` | POST | Yes |
| **Cards** | `/api/cards/my-cards` | GET | Yes |
| | `/api/cards/issue` | POST | Yes |
| **Other** |...|...|...|

**Swagger UI:** http://localhost:5000/swagger

---

## 🧪 Testing

### Run Tests

```bash
# Unit tests
dotnet test tests/BankingSystem.Tests

# Integration tests
dotnet test tests/BankingSystem.IntegrationTests

# Load tests (k6 required)
k6 run performance-tests/load-test.js

# Complete workflow
.\performance-tests\test-workflow.ps1
```
    
### Performance Benchmarks

| Operation | p(95) | p(90) | Target |
|-----------|-------|-------|--------|
| User Login | 9ms | 6ms | <100ms |
| Account Balance | 5ms | 3ms | <50ms |
| Money Transfer | 15ms | 10ms | <200ms |

---

## 🔒 Security

### Implementation

- **Authentication:** JWT with refresh tokens
- **Authorization:** Role-based access control
- **Encryption:** AES-256 for sensitive data
- **Password Hashing:** BCrypt (12 rounds)
- **Rate Limiting:** Multi-tier (auth: 10/min, API: 1000/min in prod)
- **Input Validation:** FluentValidation
- **HTTPS:** TLS 1.2+ required
- **Audit Logging:** All sensitive operations tracked

### Rate Limiting

| Tier | Dev | Production |
|------|-----|------------|
| Auth | 100/min | 10/min |
| API | 5000/min | 1000/min |
| Sensitive | 100/min | 20/min |
| Global (per IP) | 1000/min | 200/min |

---

## 📈 Monitoring

### Available Services

| Service | URL | Purpose | Profile Required |
|---------|-----|---------|------------------|
| API | http://localhost:5000 | Main application | default |
| Swagger | http://localhost:5000/swagger | API documentation | default |
| Grafana | http://localhost:3000 | Metrics dashboards | full/monitoring |
| Prometheus | http://localhost:9090 | Metrics storage | full/monitoring |
| Seq | http://localhost:5341 | Centralized logging | full/monitoring |
| Hangfire | http://localhost:5000/hangfire | Background jobs | default |
| PgAdmin | http://localhost:5050 | Database management | full/tools |

**Credentials:**
- Grafana: admin/admin
- PgAdmin: admin@bankingsystem.com/admin

**Start monitoring services:**
```bash
# Start all monitoring tools
docker-compose --profile full up -d

# Start only monitoring (no PgAdmin)
docker-compose --profile monitoring up -d

# Start only database tools
docker-compose --profile tools up -d
```

### Metrics

- Request rate, latency (p50, p90, p95, p99)
- Error rates by endpoint
- Database query performance
- Cache hit/miss rates
- Memory and CPU usage
- Background job status

---

## 🚢 Deployment

### Azure (Recommended)

**Automated deployment with Bicep:**

```bash
# Development
.\azure\scripts\deploy.ps1 -Environment dev

# Production
.\azure\scripts\deploy.ps1 -Environment prod -ImageTag v1.0.0
```

**Deployed resources:**
- Azure Container Apps (auto-scaling)
- PostgreSQL Flexible Servers
- Azure Cache for Redis
- Azure Key Vault
- Application Insights
- Log Analytics Workspace

**Cost:** ~$93/month (dev) | ~$362/month (prod)

### Kubernetes

```bash
kubectl apply -f k8s/
kubectl get pods -n banking-system
```

### Docker (Self-Hosted)

```bash
docker build -t banking-api -f src/BankingSystem.API/Dockerfile .
docker run -d -p 8080:8080 banking-api
```

---

## 🔄 CI/CD

### GitHub Actions Workflows

**CI Pipeline (`.github/workflows/ci.yml`):**
- Build & compile
- Unit tests
- Integration tests
- Security scanning

**CD Pipeline (`.github/workflows/cd.yml`):**
- Build Docker image
- Push to GitHub Container Registry
- Deploy to production (on tag)

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [AZURE-DEPLOYMENT.md](./docs/AZURE-DEPLOYMENT.md) | Azure deployment guide |
| [WORKFLOW-ARCHITECTURE.md](./docs/WORKFLOW-ARCHITECTURE.md) | CI/CD pipeline documentation |

---

## 🤝 Contributing

This is a demonstration project. For production use:

1. Review and update rate limits
2. Configure production secrets in Azure Key Vault
3. Set up monitoring alerts
4. Review security configurations
5. Update CORS origins

---

## 📝 License

MIT License - See LICENSE file for details

---

## 🔗 Links

- **Repository:** https://github.com/datngth03/banking-system-demo
- **Issues:** https://github.com/datngth03/banking-system-demo/issues
- **CI/CD:** https://github.com/datngth03/banking-system-demo/actions

---

**Built with .NET 8 | Clean Architecture | CQRS | Docker | Azure**
