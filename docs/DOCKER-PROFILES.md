# ?? Docker Compose - Profiles Guide

## ?? **OVERVIEW**

Docker Compose ?ã ???c c?u hình v?i **profiles** ?? b?n có th? ch?n ch?y services nào.

---

## ?? **PROFILES**

### **1. Default (No Profile) - RECOMMENDED FOR LOCAL DEV**
Ch? ch?y services **B?T BU?C** cho development:

```yaml
? postgres-business  # Database chính
? postgres-hangfire  # Database cho background jobs
? redis             # Cache
? banking-api       # API application
```

**Start:**
```powershell
docker-compose up -d
```

**Services Running:**
- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- PostgreSQL Business: localhost:5432
- PostgreSQL Hangfire: localhost:5433
- Redis: localhost:6379

---

### **2. Monitoring Profile - FOR DEBUGGING**
Thêm các tools monitoring:

```yaml
? All default services
? seq        # Centralized logging
? prometheus # Metrics collection
? grafana    # Dashboards
```

**Start:**
```powershell
docker-compose --profile monitoring up -d
```

**Additional Services:**
- Seq Logs: http://localhost:5341
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)

---

### **3. Tools Profile - FOR DATABASE MANAGEMENT**
Thêm PgAdmin:

```yaml
? All default services
? pgadmin    # PostgreSQL management UI
```

**Start:**
```powershell
docker-compose --profile tools up -d
```

**Additional Services:**
- PgAdmin: http://localhost:5050 (admin@bankingsystem.com/admin)

---

### **4. Full Profile - ALL SERVICES**
Ch?y t?t c? services:

```yaml
? All services above
```

**Start:**
```powershell
docker-compose --profile full up -d
```

---

## ?? **RECOMMENDED USAGE**

### **For Daily Development:**
```powershell
# Ch? c?n core services
docker-compose up -d

# Test
curl http://localhost:5000/health
start http://localhost:5000/swagger
```

### **When Debugging Issues:**
```powershell
# Add monitoring
docker-compose --profile monitoring up -d

# Check logs in Seq
start http://localhost:5341
```

### **When Need Database Management:**
```powershell
# Add PgAdmin
docker-compose --profile tools up -d

# Access PgAdmin
start http://localhost:5050
```

### **For Full Testing:**
```powershell
# All services
docker-compose --profile full up -d
```

---

## ?? **COMPARISON**

| Profile | Services | RAM Usage | Use Case |
|---------|----------|-----------|----------|
| **Default** | 4 | ~500MB | Daily development |
| **Monitoring** | 7 | ~1.5GB | Debugging, performance testing |
| **Tools** | 5 | ~700MB | Database management |
| **Full** | 8 | ~2GB | Full testing, demos |

---

## ?? **WHY PROFILES?**

### **Benefits:**
1. ? **Faster startup** - Ch? ch?y services c?n thi?t
2. ? **Less RAM** - Ti?t ki?m tài nguyên
3. ? **Cleaner** - Không b? nhi?u b?i services không dùng
4. ? **Flexible** - D? dàng thêm/b?t services

### **What We Removed (from default):**
- ? **Alertmanager** - Không c?n cho local dev (ch? production)
- ? **Seq** - Optional, ch? khi c?n debug logs
- ? **Prometheus** - Optional, ch? khi c?n metrics
- ? **Grafana** - Optional, ch? khi c?n dashboards
- ? **PgAdmin** - Optional, ch? khi c?n UI database

---

## ?? **COMMANDS**

### **Start Services:**
```powershell
# Core only (recommended)
docker-compose up -d

# With monitoring
docker-compose --profile monitoring up -d

# With database tools
docker-compose --profile tools up -d

# Everything
docker-compose --profile full up -d
```

### **Stop Services:**
```powershell
# Stop all running services
docker-compose down

# Stop and remove volumes (reset data)
docker-compose down -v
```

### **View Running Services:**
```powershell
docker-compose ps
```

### **View Logs:**
```powershell
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f banking-api
```

---

## ?? **QUICK START (MINIMAL)**

```powershell
# 1. Start core services only
docker-compose up -d

# 2. Wait for health checks
Start-Sleep -Seconds 30

# 3. Test
curl http://localhost:5000/health

# 4. Open Swagger
start http://localhost:5000/swagger
```

**That's it! No monitoring clutter, just what you need.** ??

---

## ?? **NOTES**

### **About Alertmanager:**
- ? **Removed** from docker-compose
- ?? **Why?** Local development doesn't need alerts
- ? **Production:** Add it back for production deployments
- ?? **Config:** Alert rules still in `monitoring/alerts/` for future use

### **About CI/CD:**
- ?? **CI/CD ? Monitoring**
- ? **CI/CD:** Automated testing, builds (needs GitHub)
- ? **Monitoring:** Logs, metrics, dashboards (runs local)
- ?? **Current:** Focus on local dev, CI/CD ready for when you have GitHub

### **For Production:**
- Use `docker-compose.prod.yml` (to be created)
- Include Alertmanager
- Configure real alert destinations (email, Slack)
- See `docs/DEPLOYMENT-GUIDE.md`

---

## ?? **UPDATED START-DEV.PS1**

Script ?ã t? ??ng s? d?ng default profile (core services only):

```powershell
.\start-dev.ps1
# Automatically starts: PostgreSQL, Redis, API only
# Fast, clean, minimal resource usage
```

---

**Recommendation:** Ch? dùng default profile cho daily dev! ??
