# Banking System - Complete Project Structure

## 📁 Root Directory Structure

```
Bank/
├── BankingSystem.sln                    # Visual Studio Solution file
├── README.md                            # Project documentation
├── docker-compose.yml                   # Docker Compose configuration
├── .env                                 # Environment variables (local)
├── .env.example                         # Environment variables template
├── .gitignore                           # Git ignore rules
├── .dockerignore                        # Docker ignore rules
│
├── src/                                 # Source code
│   ├── BankingSystem.API/               # API Layer (Presentation)
│   ├── BankingSystem.Application/       # Application Layer (CQRS, Business Logic)
│   ├── BankingSystem.Domain/            # Domain Layer (Entities, Value Objects)
│   └── BankingSystem.Infrastructure/    # Infrastructure Layer (Data Access, Services)
│
├── tests/                               # Test projects
│   ├── BankingSystem.Tests/             # Unit and integration tests
│   └── BankingSystem.IntegrationTests/  # API integration tests
│
├── docs/                                # Documentation
│   ├── AZURE-DEPLOYMENT.md              # Azure deployment guide
│   └── WORKFLOW-ARCHITECTURE.md         # CI/CD pipeline documentation
│
├── azure/                               # Azure infrastructure as code
│   ├── bicep/                           # Bicep Infrastructure templates
│   ├── scripts/                         # Deployment scripts
│   └── appsettings/                     # Azure-specific configuration
│
├── .github/                             # GitHub configuration
│   └── workflows/                       # CI/CD workflows
│
├── k8s/                                 # Kubernetes manifests
│   ├── deployment.yml
│   ├── deployment-local.yml
│   ├── monitoring.yml
│   └── postgres.yml
│
├── monitoring/                          # Monitoring & observability
│   ├── prometheus.yml                   # Prometheus configuration
│   ├── alertmanager.yml                 # Alert rules
│   ├── grafana-datasource.yml           # Grafana datasource config
│   ├── grafana-dashboard.json           # Dashboard JSON
│   ├── grafana/                         # Grafana provisioning
│   └── alerts/                          # Alert rules
│
├── performance-tests/                   # Load testing
│   ├── load-test.js                     # k6 load test script
│   ├── auth-load-test.js                # Authentication load test
│   ├── test-workflow.ps1                # Test workflow automation
│   └── README.md                        # Load testing guide
│
└── publish/                             # Published artifacts
    ├── appsettings files
    └── Compiled DLLs
```

---

## 🔹 SRC/ - Source Code Structure

### 1. **BankingSystem.API/** - Presentation Layer

```
BankingSystem.API/
├── Program.cs                           # Application entry point & configuration
├── BankingSystem.API.csproj             # Project file
├── BankingSystem.API.http               # HTTP requests for testing
├── Dockerfile                           # Docker configuration
│
├── appsettings.json                     # Base configuration
├── appsettings.Development.json         # Development settings
├── appsettings.Docker.json              # Docker settings
├── appsettings.Production.json          # Production settings
├── appsettings.Test.json                # Test settings
│
├── Controllers/                         # REST API Endpoints
│   ├── AuthController.cs                # Authentication (Login, Register)
│   ├── AccountsController.cs            # Account operations
│   ├── TransactionsController.cs        # Transaction management
│   ├── CardsController.cs               # Card management
│   ├── BillsController.cs               # Bill payments
│   ├── UsersController.cs               # User management
│   └── MonitoringController.cs          # Health checks & monitoring
│
├── Middleware/                          # Request/Response Pipeline
│   ├── GlobalExceptionHandlerMiddleware.cs   # Global error handling
│   ├── CorrelationIdMiddleware.cs            # Trace correlation IDs
│   ├── RequestResponseLoggingMiddleware.cs   # Request/response logging
│   ├── SecurityHeadersMiddleware.cs          # Security headers
│   ├── InputSanitizationMiddleware.cs        # Input sanitization
│   └── HealthCheckResponseWriter.cs         # Custom health check responses
│
├── Extensions/                          # Dependency Injection & Setup
│   ├── ServiceCollectionExtensions.cs   # Service registration
│   ├── ApplicationBuilderExtensions.cs   # Middleware configuration
│   ├── AuthorizationExtensions.cs       # Authorization setup
│   ├── CorsExtensions.cs                # CORS configuration
│   ├── ExceptionHandlerExtensions.cs    # Exception handling setup
│   ├── HangfireExtensions.cs            # Hangfire configuration
│   ├── HealthCheckExtensions.cs         # Health checks setup
│   ├── MonitoringExtensions.cs          # Monitoring setup
│   ├── RateLimitExtensions.cs           # Rate limiting setup
│   └── SwaggerExtensions.cs             # Swagger/OpenAPI setup
│
├── Properties/
│   └── launchSettings.json              # Launch profiles
│
└── logs/                                # Application logs
    ├── banking-system-20251211.log
    └── banking-system-20260127.log
```

**Key Responsibilities:**
- REST API endpoints
- Request validation
- Response formatting
- Authentication/Authorization
- Middleware pipeline
- Swagger documentation

---

### 2. **BankingSystem.Application/** - Application Layer (CQRS)

```
BankingSystem.Application/
├── BankingSystem.Application.csproj
│
├── Commands/                            # State-Changing Operations
│   ├── Accounts/
│   │   ├── CreateAccountCommand.cs
│   │   ├── DepositCommand.cs
│   │   ├── WithdrawCommand.cs
│   │   ├── TransferFundsCommand.cs
│   │   ├── CloseAccountCommand.cs
│   │   └── Handlers/
│   │       ├── CreateAccountHandler.cs
│   │       ├── DepositHandler.cs
│   │       ├── WithdrawHandler.cs
│   │       ├── TransferFundsHandler.cs
│   │       └── CloseAccountHandler.cs
│   │
│   ├── Auth/
│   │   ├── RegisterCommand.cs
│   │   ├── LoginCommand.cs
│   │   ├── RefreshTokenCommand.cs
│   │   ├── ChangePasswordCommand.cs
│   │   └── Handlers/
│   │       ├── RegisterHandler.cs
│   │       ├── LoginHandler.cs
│   │       ├── RefreshTokenHandler.cs
│   │       └── ChangePasswordHandler.cs
│   │
│   ├── Cards/
│   │   ├── IssueCardCommand.cs
│   │   ├── ActivateCardCommand.cs
│   │   ├── BlockCardCommand.cs
│   │   └── Handlers/
│   │
│   ├── Bills/
│   │   ├── PayBillCommand.cs
│   │   └── Handlers/
│   │
│   ├── Transactions/
│   │   ├── AddTransactionCommand.cs
│   │   └── Handlers/
│   │
│   ├── Users/
│   │   ├── CreateUserCommand.cs
│   │   ├── UpdateUserCommand.cs
│   │   ├── DeleteUserCommand.cs
│   │   ├── UnlockAccountCommand.cs
│   │   └── Handlers/
│   │
│   └── Notifications/
│       ├── CreateNotificationCommand.cs
│       └── Handlers/
│
├── Queries/                             # Read-Only Operations
│   ├── Accounts/
│   │   ├── GetAccountsByUserIdQuery.cs
│   │   ├── GetAccountDetailsQuery.cs
│   │   └── Handlers/
│   │
│   ├── Transactions/
│   │   ├── GetTransactionsByUserIdQuery.cs
│   │   ├── GetTransactionsByUserIdPagedQuery.cs
│   │   ├── GetTransactionReceiptQuery.cs
│   │   └── Handlers/
│   │
│   ├── Cards/
│   │   ├── GetCardsByUserIdQuery.cs
│   │   ├── GetCardsByAccountIdQuery.cs
│   │   └── Handlers/
│   │
│   ├── Bills/
│   │   ├── GetPendingBillsQuery.cs
│   │   └── Handlers/
│   │
│   ├── Users/
│   │   ├── GetUserByIdQuery.cs
│   │   └── Handlers/
│   │
│   └── Notifications/
│       ├── GetUnreadNotificationsQuery.cs
│       └── Handlers/
│
├── Behaviors/                           # MediatR Pipeline Behaviors
│   ├── ValidationBehavior.cs            # Input validation
│   ├── PerformanceBehavior.cs           # Performance monitoring
│   ├── TransactionBehavior.cs           # Database transactions
│   └── LoggingBehavior.cs               # Request/response logging
│
├── DTOs/                                # Data Transfer Objects
│   ├── Accounts/
│   │   ├── AccountDto.cs
│   │   ├── DepositRequest.cs
│   │   └── WithdrawRequest.cs
│   │
│   ├── Auth/
│   │   ├── RegisterDto.cs
│   │   ├── LoginDto.cs
│   │   ├── AuthResponseDto.cs
│   │   ├── RefreshTokenDto.cs
│   │   └── ChangePasswordDto.cs
│   │
│   ├── Cards/
│   │   ├── CardDto.cs
│   │   ├── ActivateCardRequest.cs
│   │   └── BlockCardRequest.cs
│   │
│   ├── Transactions/
│   │   ├── TransactionDto.cs
│   │   └── TransactionReceiptDto.cs
│   │
│   ├── Bills/
│   │   └── BillDto.cs
│   │
│   ├── Users/
│   │   ├── UserDto.cs
│   │   └── UserCreateDto.cs
│   │
│   ├── Notifications/
│   │   └── NotificationDto.cs
│   │
│   └── AuditLogs/
│       └── AuditLogDto.cs
│
├── Validators/                          # FluentValidation
│   ├── RegisterValidator.cs
│   ├── LoginValidator.cs
│   ├── CreateAccountValidator.cs
│   ├── DepositRequestValidator.cs
│   ├── WithdrawRequestValidator.cs
│   ├── TransferFundsValidator.cs
│   ├── IssueCardValidator.cs
│   ├── ActivateCardRequestValidator.cs
│   ├── BlockCardValidator.cs
│   ├── PayBillValidator.cs
│   ├── CreateUserValidator.cs
│   ├── UpdateUserValidator.cs
│   ├── DeleteUserValidator.cs
│   ├── ChangePasswordValidator.cs
│   ├── PasswordComplexityValidator.cs
│   ├── AddTransactionValidator.cs
│   └── CreateNotificationValidator.cs
│
├── Events/                              # Domain Events
│   ├── AccountCreatedEvent.cs
│   ├── TransactionCompletedEvent.cs
│   └── BillPaymentCompletedEvent.cs
│
├── EventHandlers/                       # Domain Event Handlers
│   ├── AccountCreatedEventHandler.cs
│   ├── TransactionCompletedEventHandler.cs
│   └── BillPaymentCompletedEventHandler.cs
│
├── Exceptions/                          # Application Exceptions
│   ├── BankingApplicationException.cs
│   ├── ValidationFailureException.cs
│   ├── NotFoundException.cs
│   ├── UnauthorizedException.cs
│   ├── ForbiddenException.cs
│   └── README.md
│
├── Interfaces/                          # Service Contracts
│   ├── IApplicationDbContext.cs         # Database context
│   ├── IAccountService.cs
│   ├── ITransactionService.cs
│   ├── IUserService.cs
│   ├── IAuditLogService.cs
│   ├── ICacheService.cs
│   ├── IJwtService.cs
│   ├── IPasswordHasher.cs
│   ├── IDataEncryptionService.cs
│   ├── IEmailService.cs
│   ├── INotificationService.cs
│   ├── IBackgroundJobScheduler.cs
│   ├── IEventPublisher.cs
│   ├── IMetricsService.cs
│   ├── IErrorTrackingService.cs
│   ├── ICurrentUserService.cs
│   ├── IInterestCalculationService.cs
│   └── IOutboxService.cs
│
├── Models/                              # Configuration Models
│   ├── JwtSettings.cs
│   ├── EmailSettings.cs
│   ├── CorsSettings.cs
│   ├── RateLimitSettings.cs
│   ├── InterestSettings.cs
│   ├── OutboxMessage.cs
│   └── PaginationParams.cs
│
├── Constants/                           # Constant values
│   ├── Roles.cs                         # User roles
│   ├── Policies.cs                      # Authorization policies
│   └── ValidationMessages.cs            # Error messages
│
├── Mappings/
│   └── MappingProfile.cs                # AutoMapper configuration
│
├── Extensions/
│   └── ApplicationServiceExtensions.cs  # DI registration
│
├── Common/
│   └── Result.cs                        # Result wrapper
│
└── Properties/
    └── launchSettings.json
```

**Key Responsibilities:**
- CQRS Commands & Queries
- MediatR handlers
- Business logic orchestration
- DTOs for API communication
- Validation rules
- Domain events
- Service interfaces

---

### 3. **BankingSystem.Domain/** - Domain Layer

```
BankingSystem.Domain/
├── BankingSystem.Domain.csproj
│
├── Entities/                            # Core Business Entities
│   ├── User.cs                          # User entity (with Identity)
│   ├── Account.cs                       # Bank account
│   ├── Transaction.cs                   # Financial transactions
│   ├── Card.cs                          # Debit/Credit cards
│   ├── Bill.cs                          # Bill payments
│   ├── Notification.cs                  # User notifications
│   ├── RefreshToken.cs                  # JWT refresh tokens
│   └── AuditLog.cs                      # Audit trail
│
├── ValueObjects/                        # Domain Value Objects
│   ├── Money.cs                         # Currency & amount
│   ├── Address.cs                       # User address
│   ├── DateRange.cs                     # Date range
│   └── ValueObject.cs                   # Base class
│
├── Enums/                               # Domain Enumerations
│   ├── Role.cs                          # User roles (Admin, User, Manager, Support)
│   ├── AccountType.cs                   # Account types (Checking, Savings, Business)
│   ├── TransactionType.cs               # Transaction types (Deposit, Withdraw, Transfer)
│   └── CardStatus.cs                    # Card states (Active, Inactive, Blocked, Expired)
│
├── Exceptions/                          # Domain Exceptions
│   ├── DomainException.cs               # Base domain exception
│   ├── InsufficientFundsException.cs    # Low balance
│   ├── InvalidAccountException.cs       # Bad account state
│   └── InvalidCardException.cs          # Bad card state
│
├── Interfaces/                          # Domain Contracts
│   ├── IAggregateRoot.cs                # Aggregate root marker
│   ├── IEntity.cs                       # Entity marker
│   ├── IDomainEvent.cs                  # Domain event marker
│   ├── IRepository.cs                   # Repository pattern
│   └── IUnitOfWork.cs                   # Unit of work pattern
│
├── DomainEvents/                        # Domain Events (if separate from Application)
│
└── Properties/
    └── launchSettings.json
```

**Key Responsibilities:**
- Business entities
- Value objects
- Domain rules & constraints
- Domain exceptions
- Aggregate roots
- Entity relationships

---

### 4. **BankingSystem.Infrastructure/** - Infrastructure Layer

```
BankingSystem.Infrastructure/
├── BankingSystem.Infrastructure.csproj
│
├── Persistence/                         # Data Access
│   ├── BankingSystemDbContext.cs        # EF Core DbContext
│   │
│   ├── Configurations/                  # EF Core Entity Configurations
│   │   ├── UserConfiguration.cs
│   │   ├── AccountConfiguration.cs
│   │   ├── TransactionConfiguration.cs
│   │   ├── CardConfiguration.cs
│   │   ├── BillConfiguration.cs
│   │   ├── NotificationConfiguration.cs
│   │   ├── AuditLogConfiguration.cs
│   │   └── OutboxMessageConfiguration.cs
│   │
│   ├── UnitOfWork.cs                    # Unit of work implementation
│   │
│   └── Migrations/                      # EF Core Migrations
│       ├── 20251120020236_AddAccountLockout.cs
│       ├── 20251120071004_AddPerformanceIndexes.cs
│       ├── 20251128023909_AddSecurityAndPerformanceEnhancements.cs
│       └── BankingSystemDbContextModelSnapshot.cs
│
├── Repositories/                        # Repository Pattern
│   └── GenericRepository.cs             # Generic repository implementation
│
├── Services/                            # Business Services
│   ├── JwtService.cs                    # JWT token generation
│   ├── PasswordHasher.cs                # Password hashing (BCrypt)
│   ├── DataEncryptionService.cs         # AES-256 encryption
│   ├── EmailService.cs                  # Email notifications
│   ├── MockEmailService.cs              # Mock email (dev/test)
│   ├── NotificationService.cs           # In-app notifications
│   ├── AuditLogService.cs               # Audit logging
│   ├── AccountService.cs                # Account business logic
│   ├── TransactionService.cs            # Transaction processing
│   ├── UserService.cs                   # User management
│   ├── CacheService.cs                  # Redis caching
│   ├── CurrentUserService.cs            # Current user info (from HttpContext)
│   ├── InterestCalculationService.cs    # Interest calculations
│   ├── ErrorTrackingService.cs          # Error tracking/reporting
│   ├── MetricsService.cs                # Metrics publishing
│   └── OutboxService.cs                 # Outbox pattern for events
│
├── Caching/                             # Cache Management
│   └── CacheKeys.cs                     # Cache key definitions
│
├── BackgroundJobs/                      # Hangfire Background Tasks
│   ├── BackgroundJobScheduler.cs        # Job scheduler interface
│   ├── InterestApplicationJob.cs        # Apply interest (scheduled)
│   └── OutboxPublisherJob.cs            # Process outbox messages
│
├── Events/                              # Event Publishing
│   └── EventPublisher.cs                # Domain event publisher
│
├── Monitoring/                          # Observability
│   ├── BankingSystemMetrics.cs          # Prometheus metrics definitions
│   └── MetricsCollectorService.cs       # Metrics collection logic
│
├── Extensions/
│   └── InfrastructureServiceExtensions.cs  # DI registration
│
├── Properties/
│   └── launchSettings.json
│
└── src/
    └── BankingSystem.Infrastructure/     # (Appears to be duplicate/artifact)
```

**Key Responsibilities:**
- Database context (EF Core)
- Repository implementation
- Data migrations
- Service implementations
- Background job scheduling
- Cache management
- Event publishing
- Monitoring metrics

---

## 🔹 TESTS/ - Test Projects

```
tests/
│
├── BankingSystem.Tests/                 # Unit & Integration Tests
│   ├── BankingSystem.Tests.csproj
│   │
│   ├── Unit/
│   │   ├── Domain/
│   │   │   ├── AccountTests.cs
│   │   │   └── MoneyTests.cs
│   │   │
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateAccountCommandTests.cs
│   │   │   │   ├── DepositCommandTests.cs
│   │   │   │   ├── TransferFundsCommandTests.cs
│   │   │   │   └── ...
│   │   │   │
│   │   │   └── Queries/
│   │   │       ├── GetAccountsByUserIdQueryTests.cs
│   │   │       └── ...
│   │   │
│   │   └── Services/
│   │       ├── PasswordHasherTests.cs
│   │       └── MetricsServiceTests.cs
│   │
│   ├── Integration/
│   │   ├── Api/
│   │   │   ├── Account integration tests
│   │   │   └── ...
│   │   │
│   │   ├── Database/
│   │   │   └── Database integration tests
│   │   │
│   │   └── Performance/
│   │       └── Performance tests
│   │
│   └── (Test fixture files)
│
└── BankingSystem.IntegrationTests/      # API Integration Tests
    ├── BankingSystem.IntegrationTests.csproj
    ├── CustomWebApplicationFactory.cs   # Test server setup
    │
    ├── Api/
    │   ├── HealthCheckTests.cs
    │   └── ...
    │
    ├── Controllers/
    │   └── API endpoint tests
    │
    ├── Persistence/
    │   └── Database tests
    │
    └── API Endpoints/ (folder)
```

---

## 🔹 DOCS/ - Documentation

```
docs/
├── AZURE-DEPLOYMENT.md                  # Azure deployment guide
│   - Bicep templates
│   - Resource provisioning
│   - Cost estimation
│   - Scaling strategies
│
└── WORKFLOW-ARCHITECTURE.md             # CI/CD documentation
    - GitHub Actions workflows
    - Build process
    - Test automation
    - Deployment pipeline
```

---

## 🔹 AZURE/ - Infrastructure as Code

```
azure/
│
├── bicep/                               # Bicep IaC Templates
│   ├── main.bicep                       # Main orchestration
│   │
│   ├── modules/
│   │   ├── container-app.bicep          # Container Apps
│   │   ├── container-apps-env.bicep     # Environment
│   │   ├── container-registry.bicep     # ACR
│   │   ├── postgresql.bicep             # PostgreSQL
│   │   ├── redis.bicep                  # Redis
│   │   ├── keyvault.bicep               # Key Vault
│   │   ├── keyvault-secrets.bicep       # Secrets management
│   │   ├── keyvault-access.bicep        # Access policies
│   │   ├── app-insights.bicep           # Application Insights
│   │   └── log-analytics.bicep          # Log Analytics
│   │
│   └── parameters/
│       ├── dev.parameters.json          # Dev parameters
│       └── prod.parameters.json         # Prod parameters
│
├── appsettings/                         # Azure-specific configs
│   ├── appsettings.Azure.Development.json
│   ├── appsettings.Azure.Production.json
│   └── README.md
│
└── scripts/
    ├── deploy.ps1                       # Deployment automation
    └── cleanup.ps1                      # Resource cleanup
```

---

## 🔹 K8S/ - Kubernetes Manifests

```
k8s/
├── deployment.yml                       # Production deployment
├── deployment-local.yml                 # Local development
├── postgres.yml                         # PostgreSQL StatefulSet
└── monitoring.yml                       # Monitoring stack
```

---

## 🔹 MONITORING/ - Observability Stack

```
monitoring/
│
├── prometheus.yml                       # Prometheus scrape config
├── alertmanager.yml                     # Alert rules
│
├── grafana/
│   ├── grafana-datasource.yml           # Prometheus datasource
│   │
│   ├── provisioning/
│   │   ├── dashboards/
│   │   │   └── default.yaml
│   │   └── datasources/
│   │       └── prometheus.yaml
│   │
│   └── dashboards/
│       └── banking-system-overview.json # Main dashboard
│
└── alerts/
    └── banking-system-rules.yml         # Alert rules (Prometheus)
```

---

## 🔹 PERFORMANCE-TESTS/ - Load Testing

```
performance-tests/
├── load-test.js                         # General load test (k6)
├── auth-load-test.js                    # Authentication load test
├── test-workflow.ps1                    # Test automation script
└── README.md                            # Load test guide
```

---

## 🔹 PUBLISH/ - Compiled Artifacts

```
publish/
├── appsettings.*.json                   # Configuration files
├── BankingSystem.API.dll                # Main assembly
├── BankingSystem.Application.dll        # Application layer
├── BankingSystem.Domain.dll             # Domain layer
├── BankingSystem.Infrastructure.dll     # Infrastructure layer
├── web.config                           # IIS config
│
└── (NuGet dependencies in language-specific folders)
    ├── Asp.Versioning.*
    ├── AutoMapper.*
    ├── Hangfire.*
    ├── MediatR.*
    ├── Serilog.*
    ├── FluentValidation.*
    ├── Npgsql.*
    ├── StackExchange.Redis.*
    └── ... (many more)
```

---

## 📊 Key Statistics

| Category | Count | Details |
|----------|-------|---------|
| **Projects** | 4 | API, Application, Domain, Infrastructure |
| **Test Projects** | 2 | Unit/Integration, API Integration |
| **Controllers** | 7 | Auth, Accounts, Transactions, Cards, Bills, Users, Monitoring |
| **Commands** | 20+ | Account, Auth, Cards, Bills, Transactions, Users, Notifications |
| **Queries** | 10+ | Account, Transactions, Cards, Bills, Users, Notifications |
| **Entities** | 8 | User, Account, Transaction, Card, Bill, Notification, RefreshToken, AuditLog |
| **Value Objects** | 4 | Money, Address, DateRange, ValueObject |
| **Services** | 15+ | JWT, Password, Encryption, Email, Notification, Audit, Cache, etc. |
| **Validators** | 18+ | FluentValidation for all DTOs |
| **Database Migrations** | 3 | Account Lockout, Performance Indexes, Security Enhancements |
| **API Endpoints** | 30+ | RESTful endpoints across all domains |

---

## 🔗 Key File Relationships

```
Controller
    ↓
API Request → Middleware (Auth, Logging, Validation)
    ↓
MediatR Dispatcher
    ↓
Command/Query Handler
    ↓
Domain Service / Repository
    ↓
DbContext (EF Core) / Cache / External Service
    ↓
Database / Redis / Email Service / etc.
```

---

## 📝 Configuration Files (Root Level)

| File | Purpose |
|------|---------|
| `BankingSystem.sln` | Visual Studio solution |
| `docker-compose.yml` | Local development environment |
| `.env` | Local environment variables |
| `.env.example` | Environment template |
| `.gitignore` | Git ignore rules |
| `.dockerignore` | Docker ignore rules |
| `README.md` | Project documentation |

---

## 🚀 Startup Entry Points

1. **API**: `src/BankingSystem.API/Program.cs`
2. **Docker**: `docker-compose.yml` (build from `Dockerfile`)
3. **Azure**: `azure/scripts/deploy.ps1`
4. **Kubernetes**: `k8s/deployment.yml`
5. **Load Tests**: `performance-tests/test-workflow.ps1`

---

## 🎯 Architecture Layers Summary

| Layer | Projects | Responsibility |
|-------|----------|-----------------|
| **Presentation** | BankingSystem.API | Controllers, Middleware, HTTP |
| **Application** | BankingSystem.Application | CQRS, Business Logic, DTOs |
| **Domain** | BankingSystem.Domain | Entities, Value Objects, Rules |
| **Infrastructure** | BankingSystem.Infrastructure | Data Access, Services, External Calls |
| **Tests** | BankingSystem.Tests* | Unit & Integration Testing |
| **DevOps** | azure/, k8s/, monitoring/ | Deployment & Observability |

---

**Generated**: 2026-01-27
**Total Directories**: 40+
**Total Files**: 200+
