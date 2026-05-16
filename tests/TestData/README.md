# Test Data Setup — README

## Overview

This folder contains SQL scripts to create a **test source database** with sample data for validating the DW-Builder BIML generation and SSIS ETL execution.

---

## Prerequisites

1. **SQL Server running** in Docker container:
   ```powershell
   docker compose up -d sqlserver
   ```

2. **Connection string**:
   - Server: `localhost,1433`
   - User: `sa`
   - Password: defined in `.env.docker` file (`SA_PASSWORD` variable)
   - TrustServerCertificate: `True`

3. **Tools**:
   - SQL Server Management Studio (SSMS) **OR**
   - Azure Data Studio **OR**
   - `sqlcmd` command-line tool

---

## Execution Order

Execute the scripts in the following order:

### Step 1: Create Test Source Database

**File:** `01_CreateTestSourceDatabase.sql`

Creates:
- Database `TestSourceDB`
- Table `dbo.Customers` (20 customers)
- Table `dbo.Orders` (30 orders with FK to Customers)

**Execute:**
```powershell
# Using sqlcmd
sqlcmd -S localhost,1433 -U sa -P <SA_PASSWORD> -i "01_CreateTestSourceDatabase.sql" -C

# Using SSMS or Azure Data Studio
# Open file and execute in context of master database
```

---

### Step 2: Insert Test Data

**File:** `02_InsertTestData.sql`

Inserts:
- 20 test customers (CustomerCode: `CUST001` to `CUST020`)
- 30 test orders with realistic dates and amounts

**Execute:**
```powershell
sqlcmd -S localhost,1433 -U sa -P <SA_PASSWORD> -i "02_InsertTestData.sql" -C
```

---

### Step 3: Configure Test Source in DW-Builder Metadata

**File:** `03_ConfigureTestSourceInDwBuilder.sql`

Configures:
- `_meta.Sources`: entry for `TestSource` (pointing to `TestSourceDB`)
- `_meta.SourceTables`: entry for `dbo.Customers` → landing table `[test].[Customers]`
- `_meta.SourceFields`: 6 fields configured (1 business key + 5 non-key fields)

**Execute:**
```powershell
sqlcmd -S localhost,1433 -U sa -P <SA_PASSWORD> -d DwBuilderDW -i "03_ConfigureTestSourceInDwBuilder.sql" -C
```

---

### Step 4: Create Landing and Staging Tables in DW

**File:** `04_CreateLandingAndStagingTables.sql`

Creates:
- Schema `[test]`
- Landing table `[test].[Customers]` with standard structure:
  - Business key: `CustomerId`
  - Technical fields: `ChangeHashKey`, `InsertDatetime`, `UpdateDatetime`, `IsDeleted`
  - Non-key fields: `CustomerCode`, `CustomerName`, `Email`, `City`, `IsActive`
- Staging table `[test].[stg_Customers]` (no PK, used for TRUNCATE + MERGE pattern)

**Execute:**
```powershell
sqlcmd -S localhost,1433 -U sa -P <SA_PASSWORD> -d DwBuilderDW -i "04_CreateLandingAndStagingTables.sql" -C
```

---

## Validation

After executing all scripts, verify setup:

```sql
-- Verify test source database
USE TestSourceDB;
SELECT 'Customers' AS TableName, COUNT(*) AS RecordCount FROM dbo.Customers
UNION ALL
SELECT 'Orders', COUNT(*) FROM dbo.Orders;

-- Verify DW metadata configuration
USE DwBuilderDW;
SELECT 
    s.[Name] AS SourceName,
    st.SchemaName + '.' + st.TableName AS SourceTable,
    st.LandingTableName,
    COUNT(sf.Id) AS ConfiguredFields
FROM _meta.Sources s
JOIN _meta.SourceTables st ON s.Id = st.SourceId
LEFT JOIN _meta.SourceFields sf ON st.Id = sf.SourceTableId
WHERE s.[Name] = N'TestSource'
GROUP BY s.[Name], st.SchemaName, st.TableName, st.LandingTableName;

-- Verify landing/staging tables exist
SELECT 
    SCHEMA_NAME(schema_id) AS SchemaName,
    name AS TableName,
    type_desc
FROM sys.tables
WHERE SCHEMA_NAME(schema_id) = 'test'
ORDER BY name;
```

**Expected results:**
- TestSourceDB: 20 customers, 30 orders
- DwBuilderDW metadata: 1 source, 1 table, 6 fields
- DwBuilderDW tables: `[test].[Customers]` (landing), `[test].[stg_Customers]` (staging)

---

## Next Steps

1. Navigate to `../BimlCompilation/` folder
2. Download BIML file from DW-Builder API using `DownloadBiml.ps1`
3. Compile BIML file with BimlExpress in Visual Studio
4. Execute SSIS packages and validate with scripts in `../SsisValidation/`

---

## Troubleshooting

### Connection failed

Check that SQL Server container is running:
```powershell
docker ps | Select-String "dw-sqlserver"
```

If not running:
```powershell
docker compose up -d sqlserver
```

### Database already exists

Scripts are idempotent. Re-running will drop and recreate tables (TEST ENVIRONMENT ONLY).

### Metadata insert fails

Ensure `DwBuilderDW` database exists and `_meta` schema is initialized. Run EF Core migrations:
```powershell
cd src/DwBuilder.Api
dotnet ef database update
```

---

## Security Note

**For test environment only.** Connection strings and credentials are stored in `.env.docker` (not committed to Git). Never use `sa` account credentials in production environments.
