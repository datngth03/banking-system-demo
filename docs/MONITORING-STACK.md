# Monitoring Stack for Banking System

## ?? **L?u Ý:**

Monitoring stack (Prometheus + Grafana + Seq) **KHÔNG BAO G?M** trong deployment m?c ??nh vì:

1. **Chi phí cao** - C?n thêm VMs/Containers (~$50-80/month)
2. **Azure Student/Trial restrictions** - Có th? v??t quota
3. **Azure ?ã có s?n** - Application Insights + Log Analytics ?? dùng

---

## ?? **?ã Deploy (Azure Managed):**

### **1. Log Analytics Workspace**
```
Name: banking-dev-logs
Purpose: Centralized logging
Query Language: KQL (Kusto)
Retention: 30 days
Cost: ~$2/month

Features:
- Structured logs from Container Apps
- Query with KQL
- Alerts & dashboards
- Integration v?i Azure Monitor
```

**Sample Query:**
```kql
ContainerAppConsoleLogs_CL
| where ContainerAppName_s == "banking-dev-api"
| where TimeGenerated > ago(1h)
| project TimeGenerated, Log_s
| order by TimeGenerated desc
```

### **2. Application Insights (Optional)**
```
Name: banking-dev-insights
Purpose: APM, telemetry, metrics
Status: DISABLED (can enable if needed)
Cost: ~$2-5/month

Features:
- Request tracking
- Dependency tracking
- Exception tracking
- Performance metrics
- Application map
```

**Enable if needed:**
```powershell
# Re-deploy v?i App Insights enabled
.\azure\scripts\deploy.ps1 -Environment dev

# Trong script, set:
enableAppInsights=true
```

---

## ?? **N?u Mu?n Self-Hosted Monitoring:**

### **Prometheus + Grafana + Seq Stack**

**Architecture:**
```
???????????????????????????????????????
? Banking API (Container Apps)        ?
? ?? Exposes /metrics (Prometheus)   ?
? ?? Sends logs to Seq               ?
? ?? Sends telemetry to App Insights ?
???????????????????????????????????????
         ?           ?           ?
         ?           ?           ?
   ??????????? ??????????? ???????????
   ?Prometheus? ?   Seq   ? ? Azure   ?
   ?(Metrics)? ? (Logs)  ? ? Monitor ?
   ??????????? ??????????? ???????????
         ?
         ?
   ???????????
   ? Grafana ?
   ?(Dashboards)
   ???????????
```

### **Deployment Options:**

#### **Option A: Kubernetes (Recommended)**

```bash
# S? d?ng k8s/monitoring.yml ?ã có s?n
kubectl apply -f k8s/monitoring.yml

# Ho?c monitoring-simple.yml (lightweight)
kubectl apply -f k8s/monitoring-simple.yml
```

**Resources deployed:**
- Prometheus Server (scrapes metrics)
- Grafana (visualization)
- Seq (structured logging)

**Cost:** ~$50-80/month (n?u ch?y trên AKS)

#### **Option B: Azure Container Instances**

```bicep
// Thêm vào main.bicep

module prometheus 'modules/prometheus.bicep' = if (enableMonitoringStack) {
  name: 'prometheus-deployment'
  params: {
    name: '${resourceNamePrefix}-prometheus'
    location: location
    // ... config
  }
}

module grafana 'modules/grafana.bicep' = if (enableMonitoringStack) {
  name: 'grafana-deployment'
  params: {
    name: '${resourceNamePrefix}-grafana'
    location: location
    // ... config
  }
}

module seq 'modules/seq.bicep' = if (enableMonitoringStack) {
  name: 'seq-deployment'
  params: {
    name: '${resourceNamePrefix}-seq'
    location: location
    // ... config
  }
}
```

**Cost:** ~$30-50/month (Container Instances)

#### **Option C: Docker Compose (Local Development)**

```yaml
# docker-compose.monitoring.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus:latest
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
    
  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
    
  seq:
    image: datalust/seq:latest
    ports:
      - "5341:80"
    environment:
      - ACCEPT_EULA=Y
```

**Cost:** FREE (local only)

---

## ?? **Cost Comparison:**

| Solution | Cost/Month | Pros | Cons |
|----------|------------|------|------|
| **Azure Managed (Current)** | ~$2-4 | ? Built-in, managed | Limited customization |
| **Kubernetes Stack** | ~$50-80 | Full control, powerful | Complex, expensive |
| **Container Instances** | ~$30-50 | Simpler than K8s | Limited features |
| **Local Docker Compose** | FREE | Great for dev | Not production |

---

## ?? **KHUY?N NGH?:**

### **For Development:**
```
? Dùng Azure Log Analytics + Application Insights
   - ?ã có s?n trong deployment
   - Query v?i KQL
   - Alerts & dashboards
   - Chi phí th?p (~$2-4/month)
```

### **For Production:**
```
? Enable Application Insights
? Thêm custom dashboards trong Azure Portal
? Setup alerts cho critical metrics
? Có th? thêm Grafana sau n?u c?n
```

---

## ?? **Enable Monitoring (Current Setup):**

### **1. Enable Application Insights:**

```powershell
# Edit deploy.ps1, change:
--parameters enableAppInsights=false

# To:
--parameters enableAppInsights=true

# Re-deploy
.\azure\scripts\deploy.ps1 -Environment dev
```

### **2. View Logs in Azure Portal:**

```
1. Go to: https://portal.azure.com
2. ? Container Apps ? banking-dev-api
3. ? Monitoring ? Logs
4. Run KQL query:

ContainerAppConsoleLogs_CL
| where TimeGenerated > ago(1h)
| project TimeGenerated, ContainerName_s, Log_s
```

### **3. Create Dashboard:**

```
1. Azure Portal ? Dashboards ? New Dashboard
2. Add tiles:
   - Metrics (CPU, Memory, Requests)
   - Logs (Errors, Warnings)
   - Application Map
```

---

## ?? **N?u C?n Deploy Full Stack:**

Contact me và tôi s? t?o Bicep modules cho:
- ? Prometheus (metrics collection)
- ? Grafana (visualization dashboards)
- ? Seq (structured logging UI)
- ? Loki (log aggregation alternative)

Nh?ng l?u ý: **Chi phí s? t?ng ?áng k?** (~$50-100/month)!

---

## ?? **Resources:**

- [Azure Monitor Docs](https://learn.microsoft.com/azure/azure-monitor/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Log Analytics KQL](https://learn.microsoft.com/azure/data-explorer/kusto/query/)
- [Prometheus on Azure](https://learn.microsoft.com/azure/azure-monitor/essentials/prometheus-metrics-overview)
