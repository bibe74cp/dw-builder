# DW-Builder Deployment Guide
**Version:** 1.0  
**Last Updated:** 2026-05-16

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Environment Setup](#environment-setup)
3. [Deployment Options](#deployment-options)
   - [IIS Deployment](#option-1-iis-deployment)
   - [Windows Service](#option-2-windows-service)
   - [Docker](#option-3-docker)
   - [Azure App Service](#option-4-azure-app-service-optional)
4. [Post-Deployment Verification](#post-deployment-verification)
5. [Troubleshooting](#troubleshooting)
6. [Rollback Procedure](#rollback-procedure)

---

## Prerequisites

### Software Requirements

| Component | Minimum Version | Recommended | Purpose |
|-----------|----------------|-------------|---------|
| .NET Runtime | 10.0 | 10.0.latest | API execution |
| SQL Server | 2019 | 2022 | Database + Agent |
| SSIS Catalog | SQL Server 2019+ | 2022 | ETL packages |
| IIS (if using) | 10.0 | 10.0 | Web hosting |
| Windows Server (if using service) | 2019 | 2022 | Service hosting |

### Network Requirements

- **Inbound Ports:**
  - `443` (HTTPS) - API access
  - `80` (HTTP) - Redirect to HTTPS
  
- **Outbound Ports:**
  - `1433` (SQL Server) - Database connection
  - Source database ports (as configured)

### Permissions Required

- **SQL Server:**
  - `db_datareader`, `db_datawriter` on DwBuilderDW
  - `EXECUTE` on SSIS Catalog stored procedures
  - SQL Server Agent job creation permissions

- **File System:**
  - Read/write access to logs directory (`/var/log/dwbuilder` or `C:\Services\DwBuilder\logs`)
  - Read access to application directory

- **IIS/Windows Service:**
  - Local Administrator for initial setup
  - Application Pool identity with appropriate permissions

---

## Environment Setup

### 1. Database Initialization

```powershell
# Navigate to project root
cd c:\Work.Git\CodicePlastico\internal\dw-builder

# Run EF Core migrations
dotnet ef database update --project src/DwBuilder.Infrastructure --startup-project src/DwBuilder.Api

# Verify schema creation
sqlcmd -S YOUR_SQL_SERVER -d DwBuilderDW -Q "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '_meta'"
```

**Expected Output:**
- Schema `_meta` created
- Tables: `Sources`, `SourceTables`, `SourceFields`, `Logs`, `AspNetUsers`, etc.

### 2. SSIS Catalog Configuration

```sql
-- Create SSIS Catalog (if not exists)
USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'SSISDB')
BEGIN
    -- Enable CLR integration (required for SSIS Catalog)
    sp_configure 'clr enabled', 1;
    RECONFIGURE;
    
    -- Create SSIS Catalog (run in SSMS)
    -- Catalog → Right-click → Create Catalog → Set password
END
GO

-- Grant permissions to DW-Builder user
USE SSISDB;
GO

CREATE USER [dwbuilder_user] FOR LOGIN [dwbuilder_user];
ALTER ROLE [ssis_admin] ADD MEMBER [dwbuilder_user];
GO
```

### 3. SQL Server Agent Setup

```sql
-- Ensure SQL Server Agent is running
-- Run database\SqlAgent\CreateJobsForSource.sql after source configuration
-- See database\SqlAgent\README.md for details
```

### 4. Generate Secrets

```powershell
# Generate JWT key (64 bytes)
openssl rand -base64 64

# Generate encryption key (32 bytes)
openssl rand -base64 32
```

**Save these securely** — they will be needed for all deployment options.

---

## Deployment Options

### Option 1: IIS Deployment

**Best For:** Traditional Windows Server environments, existing IIS infrastructure.

#### Step 1: Publish

```powershell
# Run publish script
.\deployment\publish-iis.ps1
```

Output: `src\DwBuilder.Api\bin\Release\net10.0\publish\iis\`

#### Step 2: Copy Files to Server

```powershell
# Example: Copy to production server
xcopy /E /I src\DwBuilder.Api\bin\Release\net10.0\publish\iis \\PROD-SERVER\c$\inetpub\wwwroot\dwbuilder
```

#### Step 3: Configure IIS

1. **Create Application Pool:**
   - Name: `DwBuilderAppPool`
   - .NET CLR Version: **No Managed Code**
   - Managed Pipeline Mode: `Integrated`
   - Identity: `ApplicationPoolIdentity` (or custom service account)

2. **Create Website/Application:**
   - Site Name: `DwBuilder API`
   - Physical Path: `C:\inetpub\wwwroot\dwbuilder`
   - Application Pool: `DwBuilderAppPool`
   - Binding: HTTPS on port 443 (with SSL certificate)

3. **Configure web.config:**

```powershell
# Copy template and edit
Copy-Item deployment\iis\web.config.template C:\inetpub\wwwroot\dwbuilder\web.config

# Edit web.config and set environment variables:
# - DB_CONNECTION_STRING
# - JWT_KEY
# - ENCRYPTION_KEY
# - ALLOWED_CORS_ORIGINS
```

#### Step 4: Set Permissions

```powershell
# Grant App Pool identity access to logs folder
icacls C:\inetpub\wwwroot\dwbuilder\logs /grant "IIS APPPOOL\DwBuilderAppPool:(OI)(CI)F"
```

#### Step 5: Start Site

- IIS Manager → Select site → Start
- Browse to: `https://your-server/health`

---

### Option 2: Windows Service

**Best For:** Background service without IIS dependency.

#### Step 1: Publish

```powershell
# Run publish script
.\deployment\publish-service.ps1
```

Output: `src\DwBuilder.Api\bin\Release\net10.0\publish\service\DwBuilder.Api.exe`

#### Step 2: Copy to Server

```powershell
# Create directory
mkdir C:\Services\DwBuilder

# Copy published files
xcopy /E /I src\DwBuilder.Api\bin\Release\net10.0\publish\service C:\Services\DwBuilder
```

#### Step 3: Configure Secrets

Option A: **appsettings.Production.json**

```powershell
# Edit C:\Services\DwBuilder\appsettings.Production.json
# Replace placeholders with actual values
```

Option B: **Registry Environment Variables**

```powershell
# Run regedit as Administrator
# Navigate to: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\DwBuilderApi
# Create Multi-String Value: "Environment"
# Add values (one per line):
DB_CONNECTION_STRING=Server=...;Database=DwBuilderDW;...
JWT_KEY=<your_jwt_key>
ENCRYPTION_KEY=<your_encryption_key>
ASPNETCORE_ENVIRONMENT=Production
```

#### Step 4: Install Service

```powershell
# Run as Administrator
.\deployment\install-service.ps1
```

Follow prompts to start the service.

#### Step 5: Verify

```powershell
# Check service status
Get-Service -Name DwBuilderApi

# Test health endpoint
curl http://localhost:8080/health
```

---

### Option 3: Docker

**Best For:** Containerized environments, Kubernetes, cloud-native deployments.

#### Step 1: Build Image

```bash
# From project root
docker build -t dwbuilder-api:latest -f src/DwBuilder.Api/Dockerfile .
```

#### Step 2: Create docker-compose.yml

```yaml
version: '3.8'

services:
  dwbuilder-api:
    image: dwbuilder-api:latest
    container_name: dwbuilder-api
    restart: unless-stopped
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DB_CONNECTION_STRING=Server=host.docker.internal,1433;Database=DwBuilderDW;User Id=dwbuilder_user;Password=YourPassword;TrustServerCertificate=True;
      - JWT_KEY=${JWT_KEY}
      - ENCRYPTION_KEY=${ENCRYPTION_KEY}
      - ALLOWED_CORS_ORIGINS=https://your-frontend-domain.com
    volumes:
      - ./logs:/var/log/dwbuilder
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 3s
      retries: 3
      start_period: 10s
    networks:
      - dwbuilder-network

networks:
  dwbuilder-network:
    driver: bridge
```

#### Step 3: Create .env File

```bash
# .env (same directory as docker-compose.yml)
JWT_KEY=<your_jwt_key_here>
ENCRYPTION_KEY=<your_encryption_key_here>
```

#### Step 4: Run

```bash
docker-compose up -d

# Check logs
docker-compose logs -f dwbuilder-api

# Verify health
curl http://localhost:8080/health
```

---

### Option 4: Azure App Service (Optional)

**Best For:** Azure cloud deployments, PaaS approach.

#### Prerequisites

- Azure subscription
- Azure CLI installed: `az login`

#### Step 1: Create App Service

```bash
# Variables
RESOURCE_GROUP="rg-dwbuilder-prod"
APP_SERVICE_PLAN="asp-dwbuilder"
APP_NAME="dwbuilder-api-prod"
LOCATION="westeurope"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan (Linux, .NET 10)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create App Service
az webapp create \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNETCORE:10.0"
```

#### Step 2: Configure Settings

```bash
# Set connection string
az webapp config connection-string set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --connection-string-type SQLAzure \
  --settings DwBuilder="Server=..."

# Set app settings
az webapp config appsettings set \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    JWT_KEY="<your_jwt_key>" \
    ENCRYPTION_KEY="<your_encryption_key>" \
    ASPNETCORE_ENVIRONMENT="Production"
```

#### Step 3: Deploy

```bash
# Publish and deploy
dotnet publish src/DwBuilder.Api/DwBuilder.Api.csproj -c Release -o ./publish

cd publish
zip -r ../deploy.zip .
cd ..

az webapp deployment source config-zip \
  --name $APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --src deploy.zip
```

#### Step 4: Verify

```bash
# Get default hostname
az webapp show --name $APP_NAME --resource-group $RESOURCE_GROUP --query defaultHostName -o tsv

# Test health
curl https://<app-name>.azurewebsites.net/health
```

---

## Post-Deployment Verification

### 1. Health Check

```bash
curl -i https://your-server/health

# Expected: 200 OK
# Response: {"status":"Healthy","results":{...}}
```

### 2. Swagger UI

Navigate to: `https://your-server/swagger/index.html`

- Should display API documentation
- Test authentication with `/api/v1/auth/login`

### 3. Authentication Test

```bash
# Login
curl -X POST https://your-server/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"YourPassword"}'

# Should return JWT token
```

### 4. Database Connectivity

```bash
# Create a test source
curl -X POST https://your-server/api/v1/sources \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your_token>" \
  -d '{
    "name": "Test Source",
    "serverName": "localhost",
    "databaseName": "TestDB",
    "landingSchema": "test_landing"
  }'

# Verify in database
sqlcmd -S YOUR_SERVER -d DwBuilderDW -Q "SELECT * FROM _meta.Sources"
```

### 5. SSIS Package Execution Test

1. Configure a source via API
2. Generate BIML: `GET /api/v1/biml/download`
3. Deploy BIML to SSIS Catalog
4. Execute package manually
5. Verify data in landing tables

---

## Troubleshooting

### Issue: 500 Internal Server Error on Startup

**Symptoms:**
- API returns 500 on all requests
- Health check fails

**Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Database connection failed | Verify `ConnectionStrings:DwBuilder` in appsettings or env var |
| Missing JWT key | Set `Jwt:Key` in configuration |
| Missing encryption key | Set `Encryption:Key` in configuration |
| Migrations not applied | Run `dotnet ef database update` |

**Debug Steps:**

```powershell
# Enable detailed errors (Development mode)
$env:ASPNETCORE_ENVIRONMENT="Development"
# Restart service/IIS
# Check logs in: logs\dwbuilder-<date>.log
```

---

### Issue: 401 Unauthorized on API Calls

**Symptoms:**
- `/api/v1/sources` returns 401 even with token

**Causes & Solutions:**

| Cause | Solution |
|-------|----------|
| Invalid JWT | Regenerate token via `/api/v1/auth/login` |
| Token expired | Default expiry is 60 min — login again |
| Incorrect Authorization header | Use format: `Authorization: Bearer <token>` |
| JWT secret mismatch | Ensure same `Jwt:Key` across restarts |

---

### Issue: CORS Errors in Frontend

**Symptoms:**
- Browser console shows: `CORS policy: No 'Access-Control-Allow-Origin'`

**Solution:**

```json
// appsettings.Production.json or env var
"AllowedCorsOrigins": "https://your-frontend-domain.com,https://another-domain.com"
```

Restart API after change.

---

### Issue: SQL Server Agent Jobs Not Creating

**Symptoms:**
- Jobs not appearing in SQL Server Agent

**Debug Steps:**

```sql
-- Check if SQL Server Agent is running
EXEC master.dbo.xp_servicecontrol N'QUERYSTATE', N'SQLServerAGENT';

-- Verify permissions
SELECT * FROM msdb.dbo.sysjobs WHERE name LIKE 'DW_%';

-- Check logs
SELECT * FROM _meta.Logs WHERE Message LIKE '%SQL Agent%' ORDER BY Timestamp DESC;
```

**Solution:**
- Ensure SQL Server Agent service is started
- Grant `dwbuilder_user` permissions: `EXEC sp_addrolemember 'SQLAgentUserRole', 'dwbuilder_user'`

---

### Issue: High Memory Usage

**Symptoms:**
- API consuming excessive RAM (>2GB)

**Solutions:**

1. **Enable Server Garbage Collection** (already configured in project)
2. **Limit EF Core query tracking:**

```csharp
// Already applied in repository methods
context.Sources.AsNoTracking()
```

3. **Configure IIS Application Pool recycling:**

- IIS Manager → App Pool → Advanced Settings
- Regular Time Interval: 1740 minutes (29 hours)
- Private Memory Limit: 2097152 KB (2GB)

---

## Rollback Procedure

### Application Rollback

#### IIS

```powershell
# Stop IIS site
Stop-WebSite -Name "DwBuilder API"

# Restore previous deployment
Move-Item C:\inetpub\wwwroot\dwbuilder C:\inetpub\wwwroot\dwbuilder-BACKUP
Move-Item C:\inetpub\wwwroot\dwbuilder-v1.0.0 C:\inetpub\wwwroot\dwbuilder

# Start IIS site
Start-WebSite -Name "DwBuilder API"
```

#### Windows Service

```powershell
# Stop service
Stop-Service -Name DwBuilderApi

# Restore previous version
Move-Item C:\Services\DwBuilder C:\Services\DwBuilder-BACKUP
Move-Item C:\Services\DwBuilder-v1.0.0 C:\Services\DwBuilder

# Start service
Start-Service -Name DwBuilderApi
```

#### Docker

```bash
# Rollback to previous image
docker-compose down
docker tag dwbuilder-api:v1.0.0 dwbuilder-api:latest
docker-compose up -d
```

---

### Database Migration Rollback

```powershell
# List migrations
dotnet ef migrations list --project src/DwBuilder.Infrastructure --startup-project src/DwBuilder.Api

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project src/DwBuilder.Infrastructure --startup-project src/DwBuilder.Api
```

**⚠️ Warning:** Always backup database before rollback:

```sql
BACKUP DATABASE [DwBuilderDW] 
TO DISK = 'C:\Backup\DwBuilderDW_BeforeRollback.bak'
WITH FORMAT, COMPRESSION;
```

---

### SSIS Package Rollback

1. Navigate to SSIS Catalog (SSMS → Integration Services Catalogs → SSISDB)
2. Right-click previous package version → Deploy
3. Reconfigure SQL Agent jobs to point to correct version

---

## Maintenance

### Regular Tasks

| Task | Frequency | Command |
|------|-----------|---------|
| Log cleanup | Weekly | Delete logs older than 30 days |
| Database backup | Daily | SQL Server Maintenance Plan |
| Dependency updates | Monthly | `dotnet list package --outdated` |
| Security scan | Monthly | `dotnet list package --vulnerable` |
| SSL certificate renewal | As needed | IIS Certificate Renewal |

---

## Support

For issues not covered in this guide:

1. Check logs: `logs\dwbuilder-<date>.log`
2. Review SQL Server error logs
3. Consult `Documentation-web.md` for architecture details
4. Contact: `devteam@yourdomain.com`

---

**Document Version:** 1.0  
**Last Updated:** 2026-05-16  
**Maintained By:** DW-Builder DevOps Team
