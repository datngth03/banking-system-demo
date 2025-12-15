# Nguyen Thanh Dat - SOFTWARE ENGINEER

?? datnt9903@gmail.com | ?? +84 388-741-305  
?? [LinkedIn](https://linkedin.com/in/datngth03) | ?? [GitHub](https://github.com/datngth03)  
?? [Portfolio Project](https://github.com/datngth03/banking-system-demo)

---

## ????? ABOUT ME

Software Engineer with 1+ years of experience building scalable, high-performance systems. Proven track record in developing POS systems handling 900,000+ daily requests, serving 650 stores. Passionate about performance optimization, system design, and delivering high-quality technology products with production-grade architecture.

---

## ??? TECHNICAL SKILLS

**Programming Languages:** C#, JavaScript, TypeScript, Go, Python  
**Databases:** PostgreSQL, MS SQL Server, Redis  
**Frameworks/Platforms:** ASP.NET Core (MVC, Web API), Node.js, React.js, Gin Framework  
**Architecture & Patterns:** Clean Architecture, CQRS, Event-Driven Architecture, Repository Pattern, Microservices  
**DevOps & Cloud:** Docker, Kubernetes, Azure (AKS, Container Apps), IIS, GitHub Actions  
**Monitoring & Observability:** Prometheus, Grafana, Seq, Application Insights, APM Server, Kibana  
**Testing:** xUnit, Integration Testing, Load Testing (k6)  
**Security:** OAuth2, JWT, AES-256 Encryption, Rate Limiting  
**Protocols:** REST API, gRPC, Protocol Buffers  
**Version Control:** Git, GitHub, SVN  
**Others:** Message Broker (RabbitMQ, Hangfire), Serilog, AutoMapper, FluentValidation, MediatR

---

## ?? WORK EXPERIENCE

### Software Engineer | Enrich Management System Co., Ltd
**Duration:** 07/2024 - Present

**Project:** POS System  
**Technologies:** C#, JavaScript, ASP.NET Core Web API, REST API, MS SQL Server, IIS, Redis, APM Server, Kibana

**Main Responsibilities:**
- Proposed and implemented code refactoring initiatives based on Clean Architecture and conducted code reviews with senior developers to improve code quality
- Led development of work schedule feature, combined with team to build new online booking feature, and refactored salon report module for better performance and maintainability
- Designed and optimized high-performance SQL Server solutions, including indexes, views, stored procedures, and functions
- Monitored application performance using APM tools (APM Server + Kibana) and resolved critical bugs

**Achievements:**
- ? Successfully built and launched work schedule with user-friendly interface and new online booking feature, **increasing customer engagement by 25% within 3 months**
- ? Refactored salon reporting module, **improving report generation speed by nearly 70%** (from 2-3s to 0.5-1s)
- ? Reduced API response times **over 60%**, from 1-2s to 0.3-0.5s, improving user experience and contributing to **doubling of revenue**
- ? Diagnosed and resolved critical production incident where improper connection handling and cache overflow caused high memory usage and application pool restarts
- ? Optimized server: leveraged APM tools to trace root causes and implemented fixes, cutting average RAM and CPU usage
- ? Improved work quality through regular code reviews

---

## ?? SIDE PROJECT

### Banking System Demo - Production-Ready API | [GitHub](https://github.com/datngth03/banking-system-demo)

**Technologies:** .NET 8, C# 12, PostgreSQL 16, Redis 7, Docker, Kubernetes, GitHub Actions, Prometheus, Grafana, Seq, k6

**Architecture & Design:**
- **Clean Architecture + CQRS:** Implemented complete separation of concerns with MediatR pipeline, achieving maintainable and testable codebase following SOLID principles
- **Database Design:** Designed normalized schema with 10+ tables, 50+ optimized indexes, and complete audit logging system. Implemented database-per-context pattern with separate PostgreSQL instances for business logic and background jobs
- **Security Architecture:** Multi-layered security with JWT authentication (RS256), AES-256 data encryption, password complexity validation (FluentValidation), rate limiting (4-tier: auth/api/sensitive/global), and account lockout protection
- **Performance Optimization:** Achieved **p(95) = 9ms response time** through Redis caching (75-90% hit rate), AsNoTracking queries, connection pooling (5-50 connections), and optimized database indexes (10-200x faster queries)
- **Observability Stack:** Built complete monitoring with Prometheus metrics, Grafana dashboards (7 panels: request rate, latency, memory, GC), Seq structured logging with correlation IDs, and health checks (liveness/readiness probes)
- **CI/CD Pipeline:** Automated GitHub Actions workflows with parallel jobs (build, test, security scan), Docker multi-stage builds, and deployment automation with rollback capability

**Key Features Implemented:**

**Core Banking Operations:**
- ?? **User Management:** Complete registration/login system with role-based access control (User/Manager/Support/Admin), profile management, and account lockout mechanism (5 failed attempts = 15min lockout)
- ?? **Account Management:** Multi-account support per user with real-time balance tracking, concurrent transaction handling, and overdraft protection
- ?? **Transaction Processing:** Deposit/Withdraw/Transfer operations with complete audit trail, outbox pattern for reliability, and automatic reconciliation
- ?? **Card Management:** Issue/Activate/Block debit and credit cards (Visa/Mastercard) with security validations and spending limits
- ?? **Bill Payments:** Pay bills from account with transaction validation and balance verification
- ?? **Notification System:** In-app notification creation and tracking with read/unread status
- ?? **Email Service:** Automated email notifications for transactions, welcome emails, and password reset functionality

**Technical Implementation:**
- **Background Jobs:** Hangfire for monthly interest calculation, outbox pattern message processing, and scheduled background tasks
- **Data Validation:** FluentValidation with custom validators rejecting common password patterns and enforcing business rules
- **Error Handling:** Global exception middleware with detailed error tracking and correlation IDs
- **API Documentation:** Complete Swagger/OpenAPI documentation with examples and authentication flows
- **Testing Suite:** 50+ unit tests, 20+ integration tests with real database, and load tests achieving stable performance under 10 req/s

**Performance Metrics:**
- ? **Response Times:** p(95) = 9ms, p(90) = 6ms for critical operations
- ?? **Throughput:** Tested stable under load with Docker Compose deployment
- ??? **Database Optimization:** 50+ indexes covering all query patterns, achieving 10-200x faster query performance
- ?? **Cache Efficiency:** 75-90% Redis hit rate for frequently accessed data
- ?? **Resource Usage:** Optimized for production with connection pooling and async processing

**DevOps & Deployment:**
- **Docker:** Multi-stage Dockerfile optimized for production (minimal image size, security scanning with Trivy)
- **Kubernetes:** Production-ready K8s manifests with deployment, service, ingress, HPA (auto-scaling 2-10 pods), and ConfigMap/Secret management
- **CI/CD:** GitHub Actions workflows with automated testing, security scanning, Docker build/push, and deployment automation
- **Monitoring:** Prometheus + Grafana stack with custom dashboards, Seq centralized logging, and Application Insights ready

**Documentation:**
- ?? 6 comprehensive guides (Deployment, Monitoring, Rate Limiting, Secrets Management, Workflow Architecture, K8s Testing)
- ?? Complete API documentation with Swagger UI
- ?? Setup scripts for automated testing workflow
- ?? Load test results and performance benchmarks

**Project Highlights:**
- ? **Production-Ready:** Complete with CI/CD, monitoring, security, and deployment guides
- ? **Best Practices:** Clean Architecture, SOLID principles, CQRS pattern, Repository pattern
- ? **High Performance:** 9ms p(95) response time, optimized database queries, efficient caching
- ? **Security-First:** JWT auth, AES encryption, rate limiting, password validation, audit logging
- ? **Docker-Ready:** Multi-stage Dockerfile, docker-compose for local development, K8s manifests prepared
- ? **Well-Documented:** 10+ markdown guides, comprehensive README, inline code comments

**GitHub Repository:** [banking-system-demo](https://github.com/datngth03/banking-system-demo)  
**Live Demo:** Available on request  
**CI/CD Status:** ? All workflows passing

---

## ?? EDUCATION & CERTIFICATIONS

**University of Information Technology** | 2021 - 2024 *(Early Graduation - 3.5 years)*  
Bachelor's Degree in Computer Science  
**GPA:** 8.22/10 · Graduated with **Very Good** ranking

**English Proficiency:**  
- TOEIC: 770 (Intermediate)
- EF SET Certificate: Advanced (C1 CEFR)
- Fluent in technical communication

---

## ?? CONTACT

**Email:** datnt9903@gmail.com  
**Phone:** +84 388-741-305  
**LinkedIn:** [linkedin.com/in/datngth03](https://linkedin.com/in/datngth03)  
**GitHub:** [github.com/datngth03](https://github.com/datngth03)  
**Portfolio:** [banking-system-demo](https://github.com/datngth03/banking-system-demo)

---

*Last Updated: December 2024*  
*Available for: Full-time Software Engineer positions*  
*Open to: .NET, Microservices, Cloud (Azure/AWS), Backend Development roles*
