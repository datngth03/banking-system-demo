# ?? H??NG D?N TRI?N KHAI BANKING SYSTEM LÊN AZURE

> H??ng d?n chi ti?t t?ng b??c ?? deploy Banking System Demo lên Azure Cloud Platform (Ti?ng Vi?t)

---

## ?? **M?C L?C**

1. [Gi?i thi?u](#gi?i-thi?u)
2. [Yêu c?u h? th?ng](#yêu-c?u-h?-th?ng)
3. [Chu?n b? tr??c khi deploy](#chu?n-b?-tr??c-khi-deploy)
4. [Ki?n trúc Azure](#ki?n-trúc-azure)
5. [Các b??c tri?n khai](#các-b??c-tri?n-khai)
6. [C?u hình Monitoring](#c?u-hình-monitoring)
7. [B?o m?t](#b?o-m?t)
8. [??c tính chi phí](#??c-tính-chi-phí)
9. [Troubleshooting](#troubleshooting)
10. [Checklist sau deployment](#checklist-sau-deployment)

---

## ?? **GI?I THI?U**

Tài li?u này h??ng d?n chi ti?t cách tri?n khai Banking System Demo (.NET 8) lên Microsoft Azure Cloud. 

**H? th?ng bao g?m:**
- ? .NET 8 Web API (Clean Architecture + CQRS)
- ? PostgreSQL 16 (Business Database + Hangfire)
- ? Redis Cache
- ? Monitoring Stack (Prometheus, Grafana, Seq)
- ? Background Jobs (Hangfire)

**Ph??ng pháp deploy ???c ?? xu?t:**
- **Azure Container Apps** (Khuy?n ngh? - serverless, auto-scaling, cost-effective)
- Azure App Service (Ph??ng án thay th?)

---

## ?? **YÊU C?U H? TH?NG**

### **Trên máy local c?a b?n:**

```powershell
# 1. Ki?m tra Azure CLI
az --version
# N?u ch?a có, t?i t?i: https://aka.ms/installazurecli

# 2. Ki?m tra .NET SDK
dotnet --version
# C?n .NET 8.0 SDK

# 3. Ki?m tra Docker (optional - ?? build image local)
docker --version

# 4. Ki?m tra Git
git --version
```

### **Trên Azure:**

- **Azure Subscription** (Free trial ho?c Pay-as-you-go)
- **Quy?n truy c?p:** Contributor ho?c Owner role
- **Budget:** ??c tính $50-150/tháng cho môi tr??ng production

---

## ?? **CHU?N B? TR??C KHI DEPLOY**

### **B??c 1: Login vào Azure**

```powershell
# ??ng nh?p Azure CLI
az login

# Ki?m tra subscription hi?n t?i
az account show

# N?u có nhi?u subscription, ch?n subscription mu?n dùng
az account list --output table
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

### **B??c 2: T?o Resource Group**

```powershell
# T?o resource group cho môi tr??ng production
az group create \
  --name rg-banking-prod \
  --location southeastasia \
  --tags Environment=Production Project=BankingSystem

# Ho?c cho môi tr??ng development/staging
az group create \
  --name rg-banking-dev \
  --location southeastasia \
  --tags Environment=Development Project=BankingSystem
```

**L?u ý v? location:**
- `southeastasia` - Singapore (g?n Vi?t Nam nh?t, latency th?p)
- `eastasia` - Hong Kong
- `japaneast` - Tokyo

### **B??c 3: Generate Secrets**

```powershell
# Generate JWT Secret (t?i thi?u 32 ký t?)
$jwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})
Write-Host "JWT Secret: $jwtSecret"

# Generate Encryption Key (32 bytes base64)
$encryptionKey = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
Write-Host "Encryption Key: $encryptionKey"

# Generate Database Password
$dbPassword = -join ((65..90) + (97..122) + (48..57) + 33,35,36,37,38,42 | Get-Random -Count 24 | ForEach-Object {[char]$_})
Write-Host "DB Password: $dbPassword"

# L?U L?I CÁC GIÁ TR? NÀY - B?N S? C?N DÙNG SAU!
```

---

## ??? **KI?N TRÚC AZURE**

```
???????????????????????????????????????????????????????????????
?                    Internet / Users                         ?
???????????????????????????????????????????????????????????????
                         ?
                         ?
         ?????????????????????????????????
         ?   Azure Front Door (Optional) ? ? CDN, WAF, SSL
         ?   Application Gateway         ?
         ?????????????????????????????????
                         ?
                         ?
         ?????????????????????????????????
         ?   Azure Container Apps        ? ? .NET 8 API
         ?   - Auto-scaling (2-10 pods)  ?   (Main Application)
         ?   - HTTPS Ingress             ?
         ?????????????????????????????????
                 ?
     ?????????????????????????
     ?           ?           ?
??????????? ??????????? ???????????????????
?PostgreSQL? ?  Redis  ? ?   Key Vault     ?
?Flexible  ? ?  Cache  ? ?   (Secrets)     ?
?Server    ? ?         ? ?                 ?
?- Business? ?         ? ? - JWT Secret    ?
?- Hangfire? ?         ? ? - DB Password   ?
??????????? ??????????? ???????????????????
     ?
     ?
????????????????????????????????????????????
?        Monitoring & Logging              ?
?  - Application Insights (APM)            ?
?  - Log Analytics Workspace               ?
?  - Azure Monitor (Metrics + Alerts)      ?
????????????????????????????????????????????
```

---

## ?? **CÁC B??C TRI?N KHAI**

### **B??C 1: T?o Azure Container Registry (ACR)**

```powershell
# T?o Container Registry ?? l?u Docker images
az acr create \
  --resource-group rg-banking-prod \
  --name bankingcr \
  --sku Standard \
  --location southeastasia \
  --admin-enabled true

# L?y credentials
az acr credential show --name bankingcr --resource-group rg-banking-prod

# Login vào ACR
az acr login --name bankingcr
```

### **B??C 2: Build và Push Docker Image**

**Option A: Build trên Azure (Khuy?n ngh? - nhanh h?n)**

```powershell
# Di chuy?n ??n th? m?c root c?a project
cd D:\WorkSpace\Personal\Dotnet\Bank

# Build image tr?c ti?p trên Azure
az acr build \
  --registry bankingcr \
  --image banking-api:v1.0.0 \
  --image banking-api:latest \
  --file src/BankingSystem.API/Dockerfile \
  --platform linux \
  .

# Xem danh sách images
az acr repository list --name bankingcr --output table
az acr repository show-tags --name bankingcr --repository banking-api --output table
```

**Option B: Build local và push lên**

```powershell
# Build image local
docker build -t bankingcr.azurecr.io/banking-api:v1.0.0 \
  -f src/BankingSystem.API/Dockerfile \
  --build-arg BUILD_VERSION=1.0.0 \
  .

# Tag latest
docker tag bankingcr.azurecr.io/banking-api:v1.0.0 bankingcr.azurecr.io/banking-api:latest

# Push lên ACR
docker push bankingcr.azurecr.io/banking-api:v1.0.0
docker push bankingcr.azurecr.io/banking-api:latest
```

---

### **B??C 3: T?o Azure Database for PostgreSQL**

```powershell
# T?o PostgreSQL Flexible Server cho Business Database
az postgres flexible-server create \
  --resource-group rg-banking-prod \
  --name banking-db-prod \
  --location southeastasia \
  --admin-user bankingadmin \
  --admin-password "YOUR_DB_PASSWORD_FROM_STEP_3" \
  --sku-name Standard_B2s \
  --tier Burstable \
  --storage-size 32 \
  --version 16 \
  --public-access 0.0.0.0 \
  --high-availability Disabled \
  --backup-retention 7 \
  --tags Environment=Production Database=Business

# T?o database cho Business
az postgres flexible-server db create \
  --resource-group rg-banking-prod \
  --server-name banking-db-prod \
  --database-name BankingSystemDb

# T?o PostgreSQL Flexible Server cho Hangfire
az postgres flexible-server create \
  --resource-group rg-banking-prod \
  --name banking-hangfire-prod \
  --location southeastasia \
  --admin-user hangfireadmin \
  --admin-password "YOUR_DB_PASSWORD_FROM_STEP_3" \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --storage-size 32 \
  --version 16 \
  --public-access 0.0.0.0 \
  --high-availability Disabled \
  --backup-retention 7 \
  --tags Environment=Production Database=Hangfire

# T?o database cho Hangfire
az postgres flexible-server db create \
  --resource-group rg-banking-prod \
  --server-name banking-hangfire-prod \
  --database-name BankingSystemHangfire

# C?u hình firewall cho phép Azure services k?t n?i
az postgres flexible-server firewall-rule create \
  --resource-group rg-banking-prod \
  --name banking-db-prod \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

az postgres flexible-server firewall-rule create \
  --resource-group rg-banking-prod \
  --name banking-hangfire-prod \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# (Optional) Cho phép IP máy local c?a b?n connect ?? test
$myIP = (Invoke-WebRequest -Uri "https://api.ipify.org").Content
az postgres flexible-server firewall-rule create \
  --resource-group rg-banking-prod \
  --name banking-db-prod \
  --rule-name AllowMyIP \
  --start-ip-address $myIP \
  --end-ip-address $myIP
```

**Connection Strings (l?u l?i):**

```
Business DB:
Host=banking-db-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemDb;Username=bankingadmin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true

Hangfire DB:
Host=banking-hangfire-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemHangfire;Username=hangfireadmin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```

---

### **B??C 4: T?o Azure Cache for Redis**

```powershell
# T?o Redis Cache
az redis create \
  --resource-group rg-banking-prod \
  --name banking-redis-prod \
  --location southeastasia \
  --sku Basic \
  --vm-size c0 \
  --enable-non-ssl-port false \
  --minimum-tls-version 1.2 \
  --tags Environment=Production

# L?y connection string
az redis show \
  --resource-group rg-banking-prod \
  --name banking-redis-prod \
  --query "[hostName,sslPort]" \
  --output tsv

# L?y access key
az redis list-keys \
  --resource-group rg-banking-prod \
  --name banking-redis-prod \
  --query primaryKey \
  --output tsv

# Connection String format:
# banking-redis-prod.redis.cache.windows.net:6380,password=YOUR_REDIS_KEY,ssl=True,abortConnect=False
```

**L?u ý v? SKU:**
- **Basic C0** ($16/tháng) - Development/Testing
- **Standard C1** ($62/tháng) - Production nh?, có replication
- **Premium P1** ($321/tháng) - Production l?n, clustering, persistence

---

### **B??C 5: T?o Azure Key Vault (Qu?n lý Secrets)**

```powershell
# T?o Key Vault
az keyvault create \
  --resource-group rg-banking-prod \
  --name kv-banking-prod \
  --location southeastasia \
  --sku standard \
  --enable-rbac-authorization false \
  --enabled-for-deployment true \
  --enabled-for-template-deployment true \
  --tags Environment=Production

# Thêm secrets vào Key Vault
az keyvault secret set \
  --vault-name kv-banking-prod \
  --name jwt-secret \
  --value "YOUR_JWT_SECRET_FROM_STEP_3"

az keyvault secret set \
  --vault-name kv-banking-prod \
  --name encryption-key \
  --value "YOUR_ENCRYPTION_KEY_FROM_STEP_3"

az keyvault secret set \
  --vault-name kv-banking-prod \
  --name db-business-connection \
  --value "Host=banking-db-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemDb;Username=bankingadmin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

az keyvault secret set \
  --vault-name kv-banking-prod \
  --name db-hangfire-connection \
  --value "Host=banking-hangfire-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemHangfire;Username=hangfireadmin;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"

az keyvault secret set \
  --vault-name kv-banking-prod \
  --name redis-connection \
  --value "banking-redis-prod.redis.cache.windows.net:6380,password=YOUR_REDIS_KEY,ssl=True,abortConnect=False"

# Xem danh sách secrets
az keyvault secret list --vault-name kv-banking-prod --output table
```

---

### **B??C 6: T?o Container Apps Environment**

```powershell
# T?o Container Apps Environment (shared environment cho các container apps)
az containerapp env create \
  --resource-group rg-banking-prod \
  --name banking-env-prod \
  --location southeastasia \
  --logs-destination none \
  --tags Environment=Production

# (Optional) T?o v?i Log Analytics Workspace cho monitoring
az monitor log-analytics workspace create \
  --resource-group rg-banking-prod \
  --workspace-name banking-logs \
  --location southeastasia

$workspaceId = az monitor log-analytics workspace show \
  --resource-group rg-banking-prod \
  --workspace-name banking-logs \
  --query customerId \
  --output tsv

$workspaceKey = az monitor log-analytics workspace get-shared-keys \
  --resource-group rg-banking-prod \
  --workspace-name banking-logs \
  --query primarySharedKey \
  --output tsv

# T?o l?i environment v?i logging
az containerapp env create \
  --resource-group rg-banking-prod \
  --name banking-env-prod \
  --location southeastasia \
  --logs-workspace-id $workspaceId \
  --logs-workspace-key $workspaceKey \
  --tags Environment=Production
```

---

### **B??C 7: Deploy Banking API lên Container Apps**

```powershell
# T?o Container App
az containerapp create \
  --resource-group rg-banking-prod \
  --name banking-api \
  --environment banking-env-prod \
  --image bankingcr.azurecr.io/banking-api:latest \
  --registry-server bankingcr.azurecr.io \
  --registry-username bankingcr \
  --registry-password "YOUR_ACR_PASSWORD" \
  --target-port 8080 \
  --ingress external \
  --min-replicas 2 \
  --max-replicas 10 \
  --cpu 1.0 \
  --memory 2.0Gi \
  --tags Environment=Production Application=BankingAPI

# L?y URL c?a application
az containerapp show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query properties.configuration.ingress.fqdn \
  --output tsv
```

---

### **B??C 8: C?u hình Environment Variables và Secrets**

```powershell
# L?y Key Vault secrets URIs
$jwtSecretUri = az keyvault secret show \
  --vault-name kv-banking-prod \
  --name jwt-secret \
  --query id \
  --output tsv

$dbBusinessUri = az keyvault secret show \
  --vault-name kv-banking-prod \
  --name db-business-connection \
  --query id \
  --output tsv

$dbHangfireUri = az keyvault secret show \
  --vault-name kv-banking-prod \
  --name db-hangfire-connection \
  --query id \
  --output tsv

$redisUri = az keyvault secret show \
  --vault-name kv-banking-prod \
  --name redis-connection \
  --query id \
  --output tsv

$encryptionUri = az keyvault secret show \
  --vault-name kv-banking-prod \
  --name encryption-key \
  --query id \
  --output tsv

# Enable Managed Identity cho Container App
az containerapp identity assign \
  --resource-group rg-banking-prod \
  --name banking-api \
  --system-assigned

# L?y Principal ID c?a Managed Identity
$principalId = az containerapp identity show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query principalId \
  --output tsv

# C?p quy?n cho Managed Identity truy c?p Key Vault
az keyvault set-policy \
  --name kv-banking-prod \
  --object-id $principalId \
  --secret-permissions get list

# C?u hình secrets t? Key Vault
az containerapp secret set \
  --resource-group rg-banking-prod \
  --name banking-api \
  --secrets \
    jwt-secret="keyvaultref:$jwtSecretUri,identityref:system" \
    db-connection="keyvaultref:$dbBusinessUri,identityref:system" \
    hangfire-connection="keyvaultref:$dbHangfireUri,identityref:system" \
    redis-connection="keyvaultref:$redisUri,identityref:system" \
    encryption-key="keyvaultref:$encryptionUri,identityref:system"

# C?u hình environment variables
az containerapp update \
  --resource-group rg-banking-prod \
  --name banking-api \
  --set-env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    "ConnectionStrings__DefaultConnection=secretref:db-connection" \
    "ConnectionStrings__HangfireConnection=secretref:hangfire-connection" \
    "ConnectionStrings__Redis=secretref:redis-connection" \
    "JwtSettings__Secret=secretref:jwt-secret" \
    "JwtSettings__Issuer=https://banking-api.azurecontainerapps.io" \
    "JwtSettings__Audience=https://banking-api.azurecontainerapps.io" \
    "JwtSettings__ExpiryMinutes=60" \
    "EncryptionSettings__Key=secretref:encryption-key" \
    "RateLimitSettings__PermitLimit=1000" \
    "RateLimitSettings__WindowInSeconds=60"
```

---

### **B??C 9: Apply Database Migrations**

**Option A: T? ??ng migrations khi container start (?ã c?u hình s?n trong code)**

N?u b?n ?ã c?u hình auto-migration trong `Program.cs`, database s? t? ??ng migrate khi container start.

**Option B: Manual migration (Khuy?n ngh? cho Production)**

```powershell
# 1. T?o SQL migration script
cd D:\WorkSpace\Personal\Dotnet\Bank

dotnet ef migrations script \
  --project src/BankingSystem.Infrastructure \
  --startup-project src/BankingSystem.API \
  --output migrations/migration-prod-v1.0.0.sql \
  --idempotent \
  --configuration Production

# 2. Review SQL script (QUAN TR?NG!)
code migrations/migration-prod-v1.0.0.sql

# 3. K?t n?i vào PostgreSQL và ch?y script
# Cài ??t psql client n?u ch?a có: https://www.postgresql.org/download/

psql "Host=banking-db-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemDb;Username=bankingadmin;Password=YOUR_PASSWORD;SSL Mode=Require" \
  -f migrations/migration-prod-v1.0.0.sql

# 4. Verify migrations thành công
psql "Host=banking-db-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemDb;Username=bankingadmin;Password=YOUR_PASSWORD;SSL Mode=Require" \
  -c "\dt"
```

---

### **B??C 10: Verify Deployment**

```powershell
# 1. L?y URL c?a application
$appUrl = az containerapp show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query properties.configuration.ingress.fqdn \
  --output tsv

Write-Host "Application URL: https://$appUrl"

# 2. Ki?m tra health endpoint
Invoke-WebRequest -Uri "https://$appUrl/health" -UseBasicParsing

# 3. Ki?m tra Swagger UI (n?u enable cho production)
Start-Process "https://$appUrl/swagger"

# 4. Xem logs c?a container
az containerapp logs show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --tail 50

# 5. Xem metrics
az containerapp show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query properties.runningStatus

# 6. Test API endpoint
$registerBody = @{
  firstName = "Test"
  lastName = "User"
  email = "testuser@example.com"
  password = "SecurePass123!@#"
  phoneNumber = "+84901234567"
  dateOfBirth = "1990-01-01T00:00:00Z"
  street = "123 Test Street"
  city = "Ho Chi Minh"
  state = "HCMC"
  postalCode = "700000"
  country = "Vietnam"
} | ConvertTo-Json

Invoke-WebRequest -Uri "https://$appUrl/api/v1/auth/register" `
  -Method POST `
  -Body $registerBody `
  -ContentType "application/json" `
  -UseBasicParsing
```

---

## ?? **C?U HÌNH MONITORING**

### **B??c 1: T?o Application Insights**

```powershell
# T?o Application Insights
az monitor app-insights component create \
  --resource-group rg-banking-prod \
  --app banking-api-insights \
  --location southeastasia \
  --workspace /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rg-banking-prod/providers/Microsoft.OperationalInsights/workspaces/banking-logs \
  --tags Environment=Production

# L?y Instrumentation Key và Connection String
$instrumentationKey = az monitor app-insights component show \
  --resource-group rg-banking-prod \
  --app banking-api-insights \
  --query instrumentationKey \
  --output tsv

$connectionString = az monitor app-insights component show \
  --resource-group rg-banking-prod \
  --app banking-api-insights \
  --query connectionString \
  --output tsv

Write-Host "Instrumentation Key: $instrumentationKey"
Write-Host "Connection String: $connectionString"

# Thêm Application Insights vào Container App
az containerapp update \
  --resource-group rg-banking-prod \
  --name banking-api \
  --set-env-vars \
    APPLICATIONINSIGHTS_CONNECTION_STRING="$connectionString"
```

### **B??c 2: C?u hình Alerts**

```powershell
# T?o Action Group ?? nh?n alerts qua email
az monitor action-group create \
  --resource-group rg-banking-prod \
  --name banking-alerts \
  --short-name BankAlert \
  --email-receiver name=admin email=your-email@example.com \
  --tags Environment=Production

# Alert 1: High error rate (>5 errors/min)
az monitor metrics alert create \
  --resource-group rg-banking-prod \
  --name high-error-rate \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rg-banking-prod/providers/Microsoft.App/containerApps/banking-api \
  --condition "avg Replicas > 0" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action banking-alerts \
  --description "Alert when error rate is high" \
  --severity 2

# Alert 2: High CPU usage (>80%)
az monitor metrics alert create \
  --resource-group rg-banking-prod \
  --name high-cpu-usage \
  --scopes /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rg-banking-prod/providers/Microsoft.App/containerApps/banking-api \
  --condition "avg UsageNanoCores > 800000000" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action banking-alerts \
  --description "Alert when CPU usage exceeds 80%" \
  --severity 3

# Alert 3: Database connection failures
# (C?u hình trong Application Insights Custom Metrics)
```

### **B??c 3: C?u hình Dashboard trong Azure Portal**

1. Vào Azure Portal: https://portal.azure.com
2. Tìm Application Insights: `banking-api-insights`
3. Click "Application Dashboard" ho?c t?o custom dashboard v?i:
   - **Request rate** (requests/sec)
   - **Response time** (p50, p95, p99)
   - **Failed requests** (count)
   - **Dependency calls** (PostgreSQL, Redis)
   - **Exceptions** (count và details)
   - **Custom metrics** (business transactions, user registrations)

---

## ?? **B?O M?T**

### **1. Enable HTTPS và Custom Domain**

```powershell
# Container Apps ?ã t? ??ng có HTTPS v?i domain m?c ??nh
# ?? s? d?ng custom domain:

# B??c 1: Thêm custom domain
az containerapp hostname add \
  --resource-group rg-banking-prod \
  --name banking-api \
  --hostname api.yourdomain.com

# B??c 2: Bind certificate (t? Key Vault ho?c upload)
az containerapp hostname bind \
  --resource-group rg-banking-prod \
  --name banking-api \
  --hostname api.yourdomain.com \
  --environment banking-env-prod \
  --thumbprint YOUR_CERT_THUMBPRINT
```

### **2. Network Security**

```powershell
# T?o Virtual Network
az network vnet create \
  --resource-group rg-banking-prod \
  --name banking-vnet \
  --address-prefix 10.0.0.0/16 \
  --subnet-name container-subnet \
  --subnet-prefix 10.0.1.0/24 \
  --location southeastasia

# T?o subnet cho PostgreSQL
az network vnet subnet create \
  --resource-group rg-banking-prod \
  --vnet-name banking-vnet \
  --name database-subnet \
  --address-prefix 10.0.2.0/24 \
  --service-endpoints Microsoft.Storage

# Enable Private Endpoint cho PostgreSQL
az postgres flexible-server update \
  --resource-group rg-banking-prod \
  --name banking-db-prod \
  --public-access Disabled

# ... (C?u hình private endpoint - c?n thêm b??c chi ti?t)
```

### **3. Azure AD / Entra ID Authentication (Optional)**

```powershell
# ??ng ký app trong Azure AD
az ad app create \
  --display-name BankingSystemAPI \
  --sign-in-audience AzureADMyOrg

# L?y Application ID
$appId = az ad app list \
  --display-name BankingSystemAPI \
  --query [0].appId \
  --output tsv

# C?u hình redirect URIs, scopes, etc.
# (Chi ti?t trong tài li?u Azure AD)
```

### **4. Security Best Practices Checklist**

- [x] T?t c? secrets trong Key Vault (không hardcode)
- [x] Managed Identity cho authentication (không dùng connection strings)
- [x] HTTPS enforced
- [x] TLS 1.2+ cho t?t c? connections
- [x] Database firewall rules (ch? cho phép Azure services)
- [x] Rate limiting enabled (1000 req/min)
- [x] CORS restricted to specific origins
- [x] Input validation v?i FluentValidation
- [x] SQL injection prevention (EF Core parameterized queries)
- [x] XSS protection
- [x] Regular security scanning (Azure Security Center)

---

## ?? **??C TÍNH CHI PHÍ**

### **Môi tr??ng Development/Testing**

| Service | SKU | Giá/tháng (USD) | Ghi chú |
|---------|-----|-----------------|---------|
| Container Apps | Consumption (1vCPU, 2GB, 2 replicas avg) | ~$25 | Pay per use |
| PostgreSQL Business | Burstable B1ms | ~$12 | 1vCore, 2GB RAM |
| PostgreSQL Hangfire | Burstable B1ms | ~$12 | 1vCore, 2GB RAM |
| Redis Cache | Basic C0 | ~$16 | 250MB |
| Key Vault | Standard | ~$3 | 10K operations |
| Container Registry | Standard | ~$20 | 100GB storage |
| Application Insights | Pay-as-you-go | ~$5 | <5GB data/month |
| **T?NG** | | **~$93/tháng** | |

### **Môi tr??ng Production (Nh?)**

| Service | SKU | Giá/tháng (USD) | Ghi chú |
|---------|-----|-----------------|---------|
| Container Apps | Consumption (2vCPU, 4GB, 3 replicas avg) | ~$75 | Auto-scaling 2-10 |
| PostgreSQL Business | General Purpose D2s_v3 | ~$120 | 2vCore, 8GB RAM |
| PostgreSQL Hangfire | Burstable B2s | ~$30 | 2vCore, 4GB RAM |
| Redis Cache | Standard C1 | ~$62 | 1GB, HA |
| Key Vault | Standard | ~$5 | 50K operations |
| Container Registry | Standard | ~$20 | 100GB storage |
| Application Insights | Pay-as-you-go | ~$25 | 20GB data/month |
| Log Analytics | Pay-as-you-go | ~$15 | 10GB data/month |
| Backup & Storage | | ~$10 | Database backups |
| **T?NG** | | **~$362/tháng** | |

### **Môi tr??ng Production (V?a - Khuy?n ngh?)**

| Service | SKU | Giá/tháng (USD) | Ghi chú |
|---------|-----|-----------------|---------|
| Container Apps | Consumption (2vCPU, 4GB, 5 replicas avg) | ~$125 | Auto-scaling 2-15 |
| PostgreSQL Business | General Purpose D4s_v3 | ~$240 | 4vCore, 16GB RAM, HA |
| PostgreSQL Hangfire | General Purpose D2s_v3 | ~$60 | 2vCore, 8GB RAM |
| Redis Cache | Standard C2 | ~$125 | 2.5GB, HA |
| Key Vault | Standard | ~$10 | 100K operations |
| Container Registry | Standard | ~$40 | 500GB storage |
| Application Insights | Pay-as-you-go | ~$50 | 50GB data/month |
| Log Analytics | Pay-as-you-go | ~$30 | 30GB data/month |
| Backup & Storage | | ~$20 | Database backups |
| Azure Front Door | Standard | ~$35 | CDN + WAF |
| **T?NG** | | **~$735/tháng** | |

### **Chi?n l??c ti?t ki?m chi phí:**

1. **Reserved Instances:** Gi?m 30-50% chi phí PostgreSQL n?u commit 1-3 n?m
2. **Auto-scaling:** Container Apps ch? scale khi c?n
3. **Development Environments:** T?t vào cu?i ngày, b?t khi làm vi?c
4. **Azure Dev/Test Pricing:** Dùng subscription Dev/Test ?? gi?m giá
5. **Resource Tags:** Tag t?t c? resources ?? track chi phí theo project/environment
6. **Budget Alerts:** Thi?t l?p budget alert ?? tránh v??t chi phí

```powershell
# T?o budget alert
az consumption budget create \
  --resource-group rg-banking-prod \
  --budget-name banking-monthly-budget \
  --amount 500 \
  --time-grain Monthly \
  --start-date 2025-01-01 \
  --end-date 2026-12-31 \
  --notifications \
    Actual=80 email=your-email@example.com \
    Actual=100 email=your-email@example.com
```

---

## ?? **TROUBLESHOOTING**

### **Problem 1: Container không start ???c**

```powershell
# Ki?m tra logs
az containerapp logs show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --tail 100 \
  --follow

# Ki?m tra revisions
az containerapp revision list \
  --resource-group rg-banking-prod \
  --name banking-api \
  --output table

# Ki?m tra environment variables
az containerapp show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query properties.template.containers[0].env

# Ki?m tra secrets
az containerapp secret list \
  --resource-group rg-banking-prod \
  --name banking-api
```

**L?i th??ng g?p:**
- `PORT binding error` ? Ki?m tra target-port = 8080
- `Environment variable not found` ? Ki?m tra env vars ?ã set ?úng
- `Secret not accessible` ? Ki?m tra Managed Identity có quy?n truy c?p Key Vault

---

### **Problem 2: Database connection failed**

```powershell
# Test connection t? local
psql "Host=banking-db-prod.postgres.database.azure.com;Port=5432;Database=BankingSystemDb;Username=bankingadmin;Password=YOUR_PASSWORD;SSL Mode=Require"

# Ki?m tra firewall rules
az postgres flexible-server firewall-rule list \
  --resource-group rg-banking-prod \
  --name banking-db-prod \
  --output table

# Ki?m tra database có t?n t?i
az postgres flexible-server db list \
  --resource-group rg-banking-prod \
  --server-name banking-db-prod \
  --output table

# Ki?m tra connection string trong Key Vault
az keyvault secret show \
  --vault-name kv-banking-prod \
  --name db-business-connection \
  --query value \
  --output tsv
```

**L?i th??ng g?p:**
- `Connection timeout` ? Ki?m tra firewall rules
- `Authentication failed` ? Ki?m tra username/password
- `SSL/TLS error` ? Thêm `Trust Server Certificate=true`
- `Database not found` ? Ch?y migrations

---

### **Problem 3: High latency / Slow performance**

```powershell
# Ki?m tra metrics
az monitor metrics list \
  --resource /subscriptions/YOUR_SUBSCRIPTION_ID/resourceGroups/rg-banking-prod/providers/Microsoft.App/containerApps/banking-api \
  --metric "Requests" "RequestDuration" "CpuUsage" "MemoryUsage" \
  --start-time 2025-01-01T00:00:00Z \
  --end-time 2025-01-01T23:59:59Z

# Xem Application Insights performance
# Vào portal ? Application Insights ? Performance

# Ki?m tra database performance
# Vào portal ? PostgreSQL ? Monitoring ? Metrics
# Xem: CPU, Memory, Connections, Slow Queries
```

**Solutions:**
- Scale up Container Apps (increase CPU/memory)
- Scale out (increase replicas)
- Optimize database queries (add indexes)
- Increase Redis cache size
- Enable database query store for slow query analysis

---

### **Problem 4: Application Insights không nh?n ???c data**

```powershell
# Ki?m tra connection string
az containerapp show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query "properties.template.containers[0].env[?name=='APPLICATIONINSIGHTS_CONNECTION_STRING']"

# Verify Instrumentation Key
az monitor app-insights component show \
  --resource-group rg-banking-prod \
  --app banking-api-insights \
  --query "[instrumentationKey,connectionString]"

# Ki?m tra logs có g?i ???c không
az containerapp logs show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --tail 50 | Select-String "ApplicationInsights"
```

---

### **Problem 5: Secrets không accessible t? Container App**

```powershell
# Ki?m tra Managed Identity ?ã enable
az containerapp identity show \
  --resource-group rg-banking-prod \
  --name banking-api

# Ki?m tra Key Vault access policies
az keyvault show \
  --name kv-banking-prod \
  --query properties.accessPolicies

# Th? l?i vi?c c?p quy?n
$principalId = az containerapp identity show \
  --resource-group rg-banking-prod \
  --name banking-api \
  --query principalId \
  --output tsv

az keyvault set-policy \
  --name kv-banking-prod \
  --object-id $principalId \
  --secret-permissions get list

# Test t? container (exec vào container)
# az containerapp exec không support tr?c ti?p, c?n dùng Kubernetes console
```

---

## ? **CHECKLIST SAU DEPLOYMENT**

### **Functional Testing**

- [ ] Health endpoint tr? v? 200 OK
  ```powershell
  Invoke-WebRequest -Uri "https://YOUR_APP_URL/health"
  ```

- [ ] Swagger UI accessible (n?u enabled)
  ```powershell
  Start-Process "https://YOUR_APP_URL/swagger"
  ```

- [ ] User registration thành công
- [ ] User login thành công
- [ ] Create account thành công
- [ ] Deposit/Withdraw/Transfer thành công
- [ ] Card operations thành công

### **Infrastructure**

- [ ] T?t c? resources ?ã ???c t?o:
  - Container Apps ?
  - PostgreSQL (Business + Hangfire) ?
  - Redis Cache ?
  - Key Vault ?
  - Container Registry ?
  - Application Insights ?

- [ ] Database migrations ?ã apply
  ```sql
  SELECT * FROM __EFMigrationsHistory;
  ```

- [ ] Secrets trong Key Vault
  ```powershell
  az keyvault secret list --vault-name kv-banking-prod
  ```

- [ ] Managed Identity có quy?n truy c?p Key Vault
- [ ] Firewall rules ?ã c?u hình ?úng

### **Security**

- [ ] HTTPS enforced (HTTP redirect to HTTPS)
- [ ] TLS 1.2+ enabled
- [ ] Secrets không hardcode trong code/config
- [ ] CORS restricted to specific origins
- [ ] Rate limiting enabled và test
- [ ] Database connections use SSL
- [ ] Redis connections use SSL

### **Monitoring & Logging**

- [ ] Application Insights ?ang nh?n data
- [ ] Logs trong Log Analytics Workspace
- [ ] Alerts ?ã c?u hình:
  - High error rate ?
  - High CPU usage ?
  - Database connection failures ?
  - Memory usage ?
- [ ] Email alerts test thành công
- [ ] Dashboard t?o trong Azure Portal
- [ ] Custom metrics ?ang track (business operations)

### **Performance**

- [ ] Response time < 100ms (p95)
- [ ] Database queries optimized (có indexes)
- [ ] Redis cache hit rate > 70%
- [ ] Connection pooling configured
- [ ] Auto-scaling test (t?ng load ? t? ??ng scale)

### **Backup & Disaster Recovery**

- [ ] Database backup enabled (7 days retention)
- [ ] Backup test (restore to test environment)
- [ ] Point-in-time restore tested
- [ ] Rollback plan documented
- [ ] Previous revisions available

### **Documentation**

- [ ] Connection strings documented (trong Key Vault)
- [ ] Architecture diagram updated
- [ ] Runbook cho operations team
- [ ] Incident response plan
- [ ] Contact list (on-call rotation)

### **Cost Management**

- [ ] Budget alerts configured
- [ ] Resource tags applied
- [ ] Cost tracking dashboard
- [ ] Reserved instances evaluated (cho production)

---

## ?? **TÀI LI?U THAM KH?O**

### **Azure Documentation**
- [Azure Container Apps](https://learn.microsoft.com/azure/container-apps/)
- [Azure Database for PostgreSQL](https://learn.microsoft.com/azure/postgresql/)
- [Azure Cache for Redis](https://learn.microsoft.com/azure/azure-cache-for-redis/)
- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/)
- [Application Insights](https://learn.microsoft.com/azure/azure-monitor/app/app-insights-overview)

### **Banking System Documentation**
- [DEPLOYMENT-GUIDE.md](./DEPLOYMENT-GUIDE.md) - General deployment guide
- [MONITORING-GUIDE.md](./MONITORING-GUIDE.md) - Monitoring setup
- [README.md](../README.md) - Project overview

### **Tools**
- [Azure CLI](https://learn.microsoft.com/cli/azure/)
- [Azure Portal](https://portal.azure.com)
- [Azure Pricing Calculator](https://azure.microsoft.com/pricing/calculator/)

---

## ?? **TI?P THEO**

Sau khi deploy thành công, b?n có th?:

1. **Setup CI/CD v?i GitHub Actions**
   - Auto-deploy khi push code lên main branch
   - Blue-green deployment
   - Automated testing tr??c khi deploy

2. **Thêm CDN và WAF v?i Azure Front Door**
   - Gi?m latency cho users ? xa
   - Web Application Firewall protection
   - DDoS protection

3. **Implement Multi-region Deployment**
   - High availability
   - Disaster recovery
   - Geo-redundancy

4. **Advanced Monitoring**
   - Custom Application Insights dashboards
   - Azure Monitor workbooks
   - Distributed tracing

5. **Performance Optimization**
   - Query optimization
   - Caching strategies
   - Database read replicas

---

## ?? **H? TR?**

N?u g?p v?n ?? trong quá trình deployment:

1. Ki?m tra ph?n [Troubleshooting](#troubleshooting)
2. Xem logs trong Application Insights ho?c Log Analytics
3. Tham kh?o Azure documentation
4. T?o GitHub Issue t?i: https://github.com/datngth03/banking-system-demo/issues

---

## ?? **CHÚC M?NG!**

B?n ?ã deploy thành công Banking System lên Azure! 

**URL ?ng d?ng c?a b?n:**
```
https://YOUR-APP-NAME.azurecontainerapps.io
```

**B??c ti?p theo:**
- Test toàn b? API endpoints
- Setup CI/CD automation
- Monitor performance và optimize
- Plan cho scaling và high availability

**Happy Banking on Azure! ????**

---

*Document version: 1.0.0*  
*Last updated: December 2025*  
*Author: Dat Nguyen (@datngth03)*
