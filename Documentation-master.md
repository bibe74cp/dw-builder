# DW-Builder — Documentazione Tecnica Master

**Versione:** 1.0  
**Data:** 16 maggio 2026  
**Autore:** Codice Plastico  
**Repository:** [GitHub bibe74cp/dw-builder](https://github.com/bibe74cp/dw-builder)

---

## Indice

1. [Panoramica del Progetto](#1-panoramica-del-progetto)
2. [Architettura](#2-architettura)
3. [Modello dei Dati](#3-modello-dei-dati)
4. [Backend API — Riferimento Endpoint](#4-backend-api--riferimento-endpoint)
5. [Sicurezza](#5-sicurezza)
6. [Configurazione](#6-configurazione)
7. [Setup & Deploy](#7-setup--deploy)
8. [ETL — SSIS & BIML](#8-etl--ssis--biml)
9. [Roadmap](#9-roadmap)
10. [Convenzioni di Sviluppo](#10-convenzioni-di-sviluppo)

---

## 1. Panoramica del Progetto

### 1.1 Descrizione e Scopo

**DW-Builder** è una piattaforma web per la configurazione e l'automazione di un Data Warehouse SQL Server alimentato da molteplici sorgenti eterogenee (ERP, CRM, gestionali, ecc.), ciascuno su istanze SQL Server distinte nella rete locale.

Il sistema permette di:
- Configurare sorgenti dati SQL Server remote
- Selezionare tabelle e campi da sincronizzare
- Generare automaticamente lo schema delle tabelle di landing nel DW
- Generare pacchetti SSIS tramite BIML per l'estrazione e sincronizzazione automatica
- Monitorare lo stato delle sincronizzazioni

### 1.2 Stack Tecnologico

| Layer | Tecnologia | Versione |
|---|---|---|
| **Backend API** | ASP.NET Core | 10.0 |
| **Linguaggio Backend** | C# | 13 |
| **Frontend** | React | 18 |
| **Linguaggio Frontend** | TypeScript | 5.x |
| **Build Tool Frontend** | Vite | 6.x |
| **UI Components** | Ant Design (antd) | 5.x |
| **ORM / Data Access** | Entity Framework Core | 10.0 |
| **Database Metadati** | SQL Server | 2019+ (schema `_meta`) |
| **Connettività Sorgenti** | Microsoft.Data.SqlClient | 5.x |
| **ETL** | SQL Server Integration Services (SSIS) | 2019+ |
| **Generazione Pacchetti ETL** | BIML (BimlScript) | - |
| **Compilazione BIML** | BimlExpress / BimlStudio | - |
| **Hashing (ETL)** | SHA-256 in C# Script Component | - |
| **Schedulazione ETL** | SQL Server Agent Jobs | - |
| **Autenticazione** | ASP.NET Core Identity + JWT | - |
| **Logging** | Serilog | 4.x |
| **Cifratura** | AES-256-CBC (.NET Cryptography) | - |

### 1.3 Link Utili

- **Requirements:** [requirements.md](requirements.md)
- **Repository GitHub:** [bibe74cp/dw-builder](https://github.com/bibe74cp/dw-builder)
- **Activity Log:** [README.md](README.md)

---

## 2. Architettura

### 2.1 Architettura di Sistema

```
┌─────────────────────────────────────────────────────────────────┐
│  Rete Locale                                                    │
│                                                                 │
│  ERPSERVER\SQL2019     CRMSERVER\SQLEXPRESS    SERVER3\SQL2022  │
│  └─ ErpDB              └─ CrmDB                └─ HrDb         │
│        │                      │                       │        │
└────────┼──────────────────────┼───────────────────────┼────────┘
         │                      │                       │
         ▼                      ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│  DWSERVER\DW — Database: DataWarehouse                          │
│                                                                 │
│  Schema ERP  ←── Landing Area sorgente ERP                     │
│  Schema CRM  ←── Landing Area sorgente CRM                     │
│  Schema HR   ←── Landing Area sorgente HR                      │
│                                                                 │
│  Schema _meta ←── Metadati configurazione (gestiti da DW-Builder) │
└─────────────────────────────────────────────────────────────────┘
         ▲
         │
┌────────────────┐
│  DW-Builder    │
│  Web App       │  (ASP.NET Core 10 + React 18)
└────────────────┘
```

### 2.2 Struttura della Solution

```
dw-builder.slnx
│
├── src/
│   ├── DwBuilder.Api/            # ASP.NET Core 10 Web API
│   │   ├── Controllers/          # AuthController, SourcesController
│   │   ├── Program.cs            # Configurazione host, DI, middleware
│   │   ├── appsettings.json      # Configurazione applicazione
│   │   └── Properties/
│   │       └── launchSettings.json
│   │
│   ├── DwBuilder.Core/           # Modelli di dominio, interfacce, DTOs
│   │   ├── Entities/             # Source, SourceTable, SourceField, Log
│   │   ├── DTOs/                 # CreateSourceRequest, UpdateSourceRequest, SourceDto
│   │   └── Interfaces/           # ISourceRepository, IEncryptionService
│   │
│   ├── DwBuilder.Infrastructure/ # Implementazioni EF Core, repository, servizi
│   │   ├── Data/
│   │   │   ├── DwBuilderDbContext.cs
│   │   │   ├── DwBuilderDbContextFactory.cs
│   │   │   └── Configurations/   # SourceConfiguration, SourceTableConfiguration, etc.
│   │   ├── Migrations/           # EF Core Migrations
│   │   ├── Repositories/         # SourceRepository
│   │   ├── Services/             # EncryptionService
│   │   └── Extensions/           # HostExtensions (MigrateDatabase)
│   │
│   ├── DwBuilder.Biml/           # Template BIML master + generatore BimlScript
│   │   └── (placeholder)
│   │
│   └── DwBuilder.Web/            # Frontend React (Vite) [FASE 6 — da implementare]
│       └── (placeholder)
│
└── tests/
    ├── DwBuilder.Core.Tests/
    └── DwBuilder.Infrastructure.Tests/
```

### 2.3 Dipendenze tra Progetti

```
DwBuilder.Api
  ├─► DwBuilder.Core
  ├─► DwBuilder.Infrastructure
  └─► DwBuilder.Biml

DwBuilder.Infrastructure
  └─► DwBuilder.Core

DwBuilder.Biml
  └─► DwBuilder.Core
```

---

## 3. Modello dei Dati

Tutti i metadati sono persistiti nello schema **`_meta`** del database DW.

### 3.1 Tabella `_meta.Sources` — Sorgenti Applicative

Rappresenta un database sorgente da cui estrarre dati.

| Colonna | Tipo | Vincoli | Descrizione |
|---|---|---|---|
| `Id` | `INT` | PK, IDENTITY | Identificativo univoco |
| `Name` | `NVARCHAR(100)` | NOT NULL, UNIQUE | Nome descrittivo (es. "ERP Aziendale") |
| `ServerName` | `NVARCHAR(200)` | NOT NULL | Nome server o indirizzo IP |
| `InstanceName` | `NVARCHAR(100)` | NULL | Istanza SQL Server (null = istanza default) |
| `DatabaseName` | `NVARCHAR(200)` | NOT NULL | Nome database sorgente |
| `LandingSchema` | `NVARCHAR(100)` | NOT NULL | Schema di destinazione nel DW (es. `ERP`) |
| `ConnectionUser` | `NVARCHAR(200)` | NULL | Username SQL (null = Windows Authentication) |
| `ConnectionPasswordEncrypted` | `NVARCHAR(500)` | NULL | Password cifrata AES-256-CBC (formato `IV:CipherText`) |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 | Flag attivazione (soft-delete) |
| `CreatedAt` | `DATETIMEOFFSET` | NOT NULL | Timestamp creazione |
| `UpdatedAt` | `DATETIMEOFFSET` | NOT NULL | Timestamp ultima modifica |

**Indici:**
- `UNIQUE` su `Name`
- Non-clustered su `IsActive`

**Entità C#:**
```csharp
namespace DwBuilder.Core.Entities;

public class Source
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ServerName { get; set; } = null!;
    public string? InstanceName { get; set; }
    public string DatabaseName { get; set; } = null!;
    public string LandingSchema { get; set; } = null!;
    public string? ConnectionUser { get; set; }
    public string? ConnectionPasswordEncrypted { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public ICollection<SourceTable> SourceTables { get; set; } = new List<SourceTable>();
}
```

---

### 3.2 Tabella `_meta.SourceTables` — Tabelle Sorgente Configurate

Rappresenta una tabella sorgente selezionata per la sincronizzazione.

| Colonna | Tipo | Vincoli | Descrizione |
|---|---|---|---|
| `Id` | `INT` | PK, IDENTITY | Identificativo univoco |
| `SourceId` | `INT` | FK → `Sources.Id`, ON DELETE CASCADE | Riferimento alla sorgente |
| `SchemaName` | `NVARCHAR(100)` | NOT NULL | Schema sorgente (es. `dbo`) |
| `TableName` | `NVARCHAR(200)` | NOT NULL | Nome tabella sorgente |
| `LandingTableName` | `NVARCHAR(200)` | NOT NULL | Nome tabella nel DW (rinominabile) |
| `IsActive` | `BIT` | NOT NULL, DEFAULT 1 | Flag attivazione |
| `LastSyncAt` | `DATETIMEOFFSET` | NULL | Data/ora ultima sincronizzazione (aggiornata da SSIS) |
| `LastSyncStatus` | `NVARCHAR(50)` | NULL | Stato ultimo sync: `Success`, `Error`, `Running` |
| `LastSyncMessage` | `NVARCHAR(4000)` | NULL | Messaggio di errore o log |
| `CreatedAt` | `DATETIMEOFFSET` | NOT NULL | Timestamp creazione |
| `UpdatedAt` | `DATETIMEOFFSET` | NOT NULL | Timestamp ultima modifica |

**Relazioni:**
- `1 Source : N SourceTables`

**Entità C#:**
```csharp
namespace DwBuilder.Core.Entities;

public class SourceTable
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string SchemaName { get; set; } = null!;
    public string TableName { get; set; } = null!;
    public string LandingTableName { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastSyncAt { get; set; }
    public string? LastSyncStatus { get; set; }
    public string? LastSyncMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public Source Source { get; set; } = null!;
    public ICollection<SourceField> SourceFields { get; set; } = new List<SourceField>();
}
```

---

### 3.3 Tabella `_meta.SourceFields` — Campi Configurati

Rappresenta un campo selezionato per ciascuna tabella sorgente.

| Colonna | Tipo | Vincoli | Descrizione |
|---|---|---|---|
| `Id` | `INT` | PK, IDENTITY | Identificativo univoco |
| `SourceTableId` | `INT` | FK → `SourceTables.Id`, ON DELETE CASCADE | Riferimento alla tabella |
| `SourceColumnName` | `NVARCHAR(200)` | NOT NULL | Nome colonna sorgente |
| `LandingColumnName` | `NVARCHAR(200)` | NOT NULL | Nome colonna nel DW (rinominabile) |
| `SqlDataType` | `NVARCHAR(100)` | NOT NULL | Tipo dati SQL rilevato (es. `nvarchar(50)`) |
| `IsBusinessKey` | `BIT` | NOT NULL, DEFAULT 0 | Se `1`, fa parte della chiave di business (PK landing) |
| `IsNullable` | `BIT` | NOT NULL, DEFAULT 1 | Nullability rilevata dalla sorgente |
| `OrdinalPosition` | `INT` | NOT NULL | Posizione nel SELECT e nella tabella di landing |
| `CreatedAt` | `DATETIMEOFFSET` | NOT NULL | Timestamp creazione |
| `UpdatedAt` | `DATETIMEOFFSET` | NOT NULL | Timestamp ultima modifica |

**Relazioni:**
- `1 SourceTable : N SourceFields`

**Entità C#:**
```csharp
namespace DwBuilder.Core.Entities;

public class SourceField
{
    public int Id { get; set; }
    public int SourceTableId { get; set; }
    public string SourceColumnName { get; set; } = null!;
    public string LandingColumnName { get; set; } = null!;
    public string SqlDataType { get; set; } = null!;
    public bool IsBusinessKey { get; set; } = false;
    public bool IsNullable { get; set; } = true;
    public int OrdinalPosition { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    
    public SourceTable SourceTable { get; set; } = null!;
}
```

---

### 3.4 Tabella `_meta.Logs` — Log Applicazione (Serilog Sink)

Raccoglie i log dell'applicazione web tramite Serilog.

| Colonna | Tipo | Vincoli | Descrizione |
|---|---|---|---|
| `Id` | `BIGINT` | PK, IDENTITY | Identificativo univoco |
| `Timestamp` | `DATETIMEOFFSET` | NOT NULL | Data/ora evento |
| `Level` | `NVARCHAR(15)` | NOT NULL | Livello log (`Information`, `Warning`, `Error`, etc.) |
| `Message` | `NVARCHAR(MAX)` | NOT NULL | Messaggio di log |
| `Exception` | `NVARCHAR(MAX)` | NULL | Stack trace eccezione |
| `Properties` | `NVARCHAR(MAX)` | NULL | Proprietà strutturate (JSON) |

**Entità C#:**
```csharp
namespace DwBuilder.Core.Entities;

public class Log
{
    public long Id { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Exception { get; set; }
    public string? Properties { get; set; }
}
```

---

### 3.5 Tabelle ASP.NET Core Identity

Le tabelle di autenticazione sono mappate nello schema `_meta`:

- `_meta.AspNetUsers`
- `_meta.AspNetRoles`
- `_meta.AspNetUserRoles`
- `_meta.AspNetUserClaims`
- `_meta.AspNetUserLogins`
- `_meta.AspNetUserTokens`
- `_meta.AspNetRoleClaims`

Gestite automaticamente da ASP.NET Core Identity.

---

### 3.6 Struttura Tabelle di Landing

Per ciascuna tabella sorgente configurata, viene generata una tabella nel DW con questa struttura:

```sql
CREATE TABLE [<LandingSchema>].[<LandingTableName>] (

    -- Chiavi di business (esempio: DataAreaId + AccountCode)
    [DataAreaId]      NVARCHAR(10)   NOT NULL,
    [AccountCode]     NVARCHAR(20)   NOT NULL,

    -- Colonne tecniche
    [ChangeHashKey]   CHAR(64)       NOT NULL,  -- SHA-256 hex lowercase
    [InsertDatetime]  DATETIME2      NOT NULL,  -- GETUTCDATE() al primo insert
    [UpdateDatetime]  DATETIME2      NOT NULL,  -- GETUTCDATE() ad ogni modifica
    [IsDeleted]       BIT            NOT NULL DEFAULT 0,  -- Soft-delete

    -- Campi dati (non-chiave) selezionati
    [Field1]          <tipo>         NULL,
    [Field2]          <tipo>         NULL,
    ...

    CONSTRAINT [PK_<LandingSchema>_<LandingTableName>]
        PRIMARY KEY CLUSTERED (<chiavi di business>)
);
```

**Regole dei campi tecnici:**

| Campo | INSERT | UPDATE | Soft-delete |
|---|---|---|---|
| `ChangeHashKey` | SHA-256 dei valori non-chiave | Ricalcolato | Invariato |
| `InsertDatetime` | `GETUTCDATE()` | Invariato | Invariato |
| `UpdateDatetime` | `GETUTCDATE()` | `GETUTCDATE()` | `GETUTCDATE()` |
| `IsDeleted` | `0` | `0` | `1` |

---

## 4. Backend API — Riferimento Endpoint

**Base URL:** `/api/v1`

Tutti gli endpoint (tranne `/auth/login`) richiedono autenticazione JWT tramite header:
```
Authorization: Bearer <token>
```

---

### 4.1 Autenticazione

#### `POST /api/v1/auth/login`

Autenticazione utente e generazione JWT.

**Request Body:**
```json
{
  "username": "admin",
  "password": "Password123!"
}
```

**Response 200 OK:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiry": "2026-05-17T08:30:00Z",
  "username": "admin"
}
```

**Response 401 Unauthorized:**
```json
{
  "message": "Invalid username or password."
}
```

---

#### `POST /api/v1/auth/register`

Registrazione nuovo utente (richiede autenticazione — solo admin).

**Request Body:**
```json
{
  "username": "newuser",
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response 201 Created:**
```json
{
  "message": "User created successfully."
}
```

**Response 400 Bad Request:**
```json
{
  "errors": [
    "Passwords must have at least one non alphanumeric character.",
    "Passwords must have at least one digit ('0'-'9')."
  ]
}
```

---

### 4.2 Gestione Sorgenti

#### `GET /api/v1/sources`

Recupera tutte le sorgenti attive.

**Response 200 OK:**
```json
[
  {
    "id": 1,
    "name": "ERP Aziendale",
    "serverName": "ERPSERVER\\SQL2019",
    "instanceName": null,
    "databaseName": "ErpDB",
    "landingSchema": "ERP",
    "connectionUser": "sa",
    "hasPassword": true,
    "isActive": true,
    "createdAt": "2026-05-16T10:00:00Z",
    "updatedAt": "2026-05-16T10:00:00Z"
  }
]
```

---

#### `GET /api/v1/sources/{id}`

Recupera dettaglio singola sorgente.

**Response 200 OK:**
```json
{
  "id": 1,
  "name": "ERP Aziendale",
  "serverName": "ERPSERVER\\SQL2019",
  "instanceName": null,
  "databaseName": "ErpDB",
  "landingSchema": "ERP",
  "connectionUser": "sa",
  "hasPassword": true,
  "isActive": true,
  "createdAt": "2026-05-16T10:00:00Z",
  "updatedAt": "2026-05-16T10:00:00Z"
}
```

**Response 404 Not Found**

---

#### `POST /api/v1/sources`

Crea una nuova sorgente.

**Request Body:**
```json
{
  "name": "CRM Aziendale",
  "serverName": "CRMSERVER",
  "instanceName": "SQLEXPRESS",
  "databaseName": "CrmDB",
  "landingSchema": "CRM",
  "connectionUser": "crmuser",
  "connectionPassword": "MySecretPassword123"
}
```

**Response 201 Created:**
```json
{
  "id": 2,
  "name": "CRM Aziendale",
  "serverName": "CRMSERVER",
  "instanceName": "SQLEXPRESS",
  "databaseName": "CrmDB",
  "landingSchema": "CRM",
  "connectionUser": "crmuser",
  "hasPassword": true,
  "isActive": true,
  "createdAt": "2026-05-16T12:00:00Z",
  "updatedAt": "2026-05-16T12:00:00Z"
}
```

**Response 400 Bad Request** (validazione fallita)

---

#### `PUT /api/v1/sources/{id}`

Modifica una sorgente esistente.

**Request Body:**
```json
{
  "name": "CRM Aziendale Updated",
  "serverName": "CRMSERVER",
  "instanceName": "SQLEXPRESS",
  "databaseName": "CrmDB",
  "landingSchema": "CRM",
  "connectionUser": "crmuser",
  "connectionPassword": null  // null = mantieni password esistente
}
```

**Response 200 OK:** (stesso formato GET)

**Response 404 Not Found**

---

#### `DELETE /api/v1/sources/{id}`

Disattiva una sorgente (soft-delete).

**Response 204 No Content**

**Response 404 Not Found**

---

### 4.3 Endpoint Futuri (FASE 2-6)

I seguenti endpoint sono pianificati per le fasi successive:

| Metodo | Path | Descrizione | Fase |
|---|---|---|---|
| `POST` | `/api/v1/sources/{id}/test-connection` | Test connessione sorgente | 2 |
| `GET` | `/api/v1/sources/{id}/available-tables` | Tabelle disponibili sulla sorgente | 2 |
| `GET` | `/api/v1/sources/{id}/tables` | Tabelle configurate | 2 |
| `PUT` | `/api/v1/sources/{id}/tables` | Salva selezione tabelle (bulk) | 2 |
| `GET` | `/api/v1/sources/{id}/tables/{tableId}/available-fields` | Colonne disponibili | 2 |
| `GET` | `/api/v1/sources/{id}/tables/{tableId}/fields` | Campi configurati | 2 |
| `PUT` | `/api/v1/sources/{id}/tables/{tableId}/fields` | Salva configurazione campi (bulk) | 2 |
| `GET` | `/api/v1/sources/{id}/tables/{tableId}/ddl` | Genera DDL tabella landing | 3 |
| `POST` | `/api/v1/sources/{id}/tables/{tableId}/apply-ddl` | Applica DDL al DW | 3 |
| `GET` | `/api/v1/biml` | Genera e scarica file `.biml` master | 4 |
| `GET` | `/api/v1/sync/logs` | Log sincronizzazioni | 7 |

---

## 5. Sicurezza

### 5.1 Autenticazione — JWT Bearer Token

DW-Builder utilizza **ASP.NET Core Identity** per la gestione utenti locali e **JWT (JSON Web Token)** per l'autenticazione stateless delle API.

**Flusso di autenticazione:**

1. L'utente invia username e password a `POST /api/v1/auth/login`
2. Il server valida le credenziali tramite Identity
3. In caso di successo, genera un JWT firmato con chiave simmetrica HMAC-SHA256
4. Il client riceve il token e lo include nell'header `Authorization: Bearer <token>` per tutte le successive richieste
5. Il middleware JWT valida firma, issuer, audience ed expiry ad ogni richiesta

**Configurazione JWT (appsettings.json):**
```json
{
  "Jwt": {
    "Key": "<chiave-segreta-256-bit>",
    "Issuer": "DwBuilder",
    "Audience": "DwBuilderUsers",
    "ExpiryMinutes": 480
  }
}
```

**Claim inseriti nel token:**
- `sub` — User ID
- `unique_name` — Username
- `email` — Email utente
- `jti` — Token ID (GUID univoco)
- `role` — Ruoli utente (se presenti)

**Configurazione password policy:**
```csharp
options.Password.RequireDigit = true;
options.Password.RequireLowercase = true;
options.Password.RequireUppercase = true;
options.Password.RequireNonAlphanumeric = false;
options.Password.RequiredLength = 8;
```

---

### 5.2 Cifratura Password Sorgenti — AES-256-CBC

Le password per la connessione alle sorgenti sono cifrate tramite **AES-256-CBC** prima di essere salvate in `Sources.ConnectionPasswordEncrypted`.

**Formato ciphertext:** `IV:CipherText` (entrambi base64-encoded)

**Chiave AES:**
- Configurata in `appsettings.json` → `Encryption:Key`
- Deve essere una chiave a 32 byte (256 bit) codificata in Base64
- Vettore di inizializzazione (IV) generato casualmente per ogni cifratura

**Implementazione:** `DwBuilder.Infrastructure.Services.EncryptionService`

```csharp
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
```

**Generazione chiave AES-256 (PowerShell):**
```powershell
$key = [byte[]]::new(32)
[Security.Cryptography.RNGCryptoServiceProvider]::Create().GetBytes($key)
[Convert]::ToBase64String($key)
```

**⚠️ Nota Sicurezza:**
- La chiave AES **non deve mai** essere committata in Git
- Utilizzare **variabili d'ambiente** o **Azure Key Vault** in produzione
- La password in chiaro non viene mai esposta tramite API (il DTO `SourceDto` espone solo `HasPassword: bool`)

---

### 5.3 CORS (Cross-Origin Resource Sharing)

Configurazione CORS esplicita per permettere chiamate dal frontend React.

**Configurazione (Program.cs):**
```csharp
var allowedOrigins = builder.Configuration["AllowedCorsOrigins"]?.Split(',') 
    ?? new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

**appsettings.json:**
```json
{
  "AllowedCorsOrigins": "http://localhost:5173,https://dwbuilder.yourdomain.com"
}
```

**⚠️ Non utilizzare wildcard (`*`) in produzione.**

---

### 5.4 SQL Injection Prevention

- Tutte le query verso database sorgenti e DW utilizzano **parametri SQL**
- Entity Framework Core genera automaticamente query parametrizzate
- Le connessioni dirette usano `SqlCommand` con `SqlParameter`

---

### 5.5 Logging Sicuro

Serilog è configurato per **non loggare dati sensibili**:
- Le password (cifrate o in chiaro) non vengono mai loggiate
- I token JWT non vengono loggati nei log applicativi
- Gli eventi di login falliti loggano solo lo username (senza password)

---

## 6. Configurazione

### 6.1 Struttura `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DwBuilder": "Server=.;Database=DwBuilderDW;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_32_CHAR_SECRET_KEY_12345678901234567890",
    "Issuer": "DwBuilder",
    "Audience": "DwBuilderUsers",
    "ExpiryMinutes": 480
  },
  "Encryption": {
    "Key": "REPLACE_WITH_BASE64_32BYTE_AES_KEY_AAAAAAAAAAAAAAAAAAAAAA=="
  },
  "AllowedCorsOrigins": "http://localhost:5173",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

### 6.2 Descrizione Campi Configurazione

| Sezione | Campo | Descrizione | Esempio |
|---|---|---|---|
| **ConnectionStrings** | `DwBuilder` | Connection string al database DW (schema `_meta`) | `Server=.;Database=DwBuilderDW;Trusted_Connection=True;TrustServerCertificate=True` |
| **Jwt** | `Key` | Chiave segreta per firma JWT (min 32 caratteri) | `YourSecretKeyMustBeAtLeast32CharsLong123456` |
| | `Issuer` | Identificativo issuer del token | `DwBuilder` |
| | `Audience` | Identificativo audience del token | `DwBuilderUsers` |
| | `ExpiryMinutes` | Durata validità token in minuti | `480` (8 ore) |
| **Encryption** | `Key` | Chiave AES-256 base64 (32 byte) per cifrare password sorgenti | (generata con comando PowerShell) |
| **AllowedCorsOrigins** | - | Elenco origini CORS consentite (separati da virgola) | `http://localhost:5173,https://dwbuilder.example.com` |
| **Serilog** | `MinimumLevel` | Livello minimo di logging (Information, Warning, Error) | `Information` |
| **AllowedHosts** | - | Host consentiti (`*` = tutti) | `*` |

---

### 6.3 Variabili d'Ambiente Raccomandate per Produzione

Per motivi di sicurezza, **non salvare segreti in appsettings.json**. Usare variabili d'ambiente o Azure Key Vault.

**Esempio configurazione variabili d'ambiente (Windows):**

```powershell
[Environment]::SetEnvironmentVariable("ConnectionStrings__DwBuilder", "Server=DWSERVER;Database=DwBuilderDW;User Id=dwbuilder_app;Password=***;TrustServerCertificate=True", "Machine")

[Environment]::SetEnvironmentVariable("Jwt__Key", "YourProductionSecretKey32CharsMinimum123", "Machine")

[Environment]::SetEnvironmentVariable("Encryption__Key", "a3F2c...Base64Key...", "Machine")
```

**ASP.NET Core legge automaticamente le variabili d'ambiente** con il pattern:
```
Sezione__Chiave
```

---

### 6.4 Generazione Chiavi Sicure

#### Chiave JWT (min 32 caratteri)

```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 40 | ForEach-Object {[char]$_})
```

#### Chiave AES-256 (32 byte, Base64)

```powershell
$key = [byte[]]::new(32)
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($key)
[Convert]::ToBase64String($key)
```

---

## 7. Setup & Deploy

### 7.1 Prerequisiti

| Componente | Versione Minima | Note |
|---|---|---|
| **.NET SDK** | 10.0 | Per compilare il progetto |
| **SQL Server** | 2019 | Per database DW |
| **SQL Server Integration Services** | 2019 | Per esecuzione pacchetti SSIS |
| **BimlExpress** o **BimlStudio** | - | Per compilare file `.biml` in pacchetti `.dtsx` |
| **Node.js** | 20.x | Per build frontend React (FASE 6) |

---

### 7.2 Setup Locale

#### 1. Clone Repository

```bash
git clone https://github.com/bibe74cp/dw-builder.git
cd dw-builder
```

#### 2. Configurazione Connection String

Modifica `src/DwBuilder.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DwBuilder": "Server=localhost;Database=DwBuilderDW;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

#### 3. Generazione Chiavi Sicurezza

Esegui i comandi PowerShell per generare `Jwt:Key` e `Encryption:Key`, poi aggiorna `appsettings.Development.json`.

#### 4. Applicazione Migration EF Core

La migration viene applicata automaticamente all'avvio tramite `app.MigrateDatabase()` in `Program.cs`.

Oppure manualmente:

```bash
cd src/DwBuilder.Api
dotnet ef database update --project ../DwBuilder.Infrastructure
```

#### 5. Creazione Primo Utente

Usare direttamente SQL Server Management Studio per inserire un utente admin:

```sql
-- L'hash corrisponde alla password "Password123!"
INSERT INTO _meta.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
VALUES (NEWID(), 'admin', 'ADMIN', 'admin@dwbuilder.local', 'ADMIN@DWBUILDER.LOCAL', 1, 
'AQAAAAIAAYagAAAAEJ...', -- hash generato tramite Identity
NEWID(), NEWID(), 0, 0, 0, 0);
```

**Oppure** (consigliato): registrare temporaneamente l'endpoint `/auth/register` senza `[Authorize]`, creare il primo utente, poi rimettere l'attributo `[Authorize]`.

#### 6. Avvio Applicazione

```bash
cd src/DwBuilder.Api
dotnet run
```

L'applicazione sarà disponibile su `https://localhost:5001` (o porta configurata in `launchSettings.json`).

Swagger UI: `https://localhost:5001/swagger`

---

### 7.3 Deploy su IIS (Windows Server)

#### 1. Pubblicazione

```bash
dotnet publish src/DwBuilder.Api/DwBuilder.Api.csproj -c Release -o C:\Deploy\DwBuilder
```

#### 2. Configurazione IIS

- Creare un nuovo **Application Pool** (.NET CLR Version: **No Managed Code**)
- Creare un nuovo **Website** o **Application** puntando a `C:\Deploy\DwBuilder`
- Assicurarsi che l'**ASP.NET Core Hosting Bundle** sia installato sul server

#### 3. Configurazione Variabili d'Ambiente

Impostare le variabili d'ambiente a livello di sistema (vedi sezione 6.3).

#### 4. Permessi SQL Server

- Creare un **SQL Login** dedicato per l'applicazione con permessi su schema `_meta`
- Aggiornare la connection string con le credenziali SQL

```json
{
  "ConnectionStrings": {
    "DwBuilder": "Server=DWSERVER;Database=DwBuilderDW;User Id=dwbuilder_app;Password=***;TrustServerCertificate=True"
  }
}
```

---

### 7.4 Deploy come Windows Service (alternativa a IIS)

```bash
dotnet publish src/DwBuilder.Api/DwBuilder.Api.csproj -c Release -o C:\Services\DwBuilder

sc.exe create DwBuilderService binPath= "C:\Services\DwBuilder\DwBuilder.Api.exe"
sc.exe start DwBuilderService
```

---

## 8. ETL — SSIS & BIML

### 8.1 Panoramica BIML (Business Intelligence Markup Language)

**BIML** è un linguaggio XML per definire pacchetti SSIS in modo dichiarativo. Tramite **BimlScript** (C# embedded), è possibile generare dinamicamente pacchetti SSIS leggendo i metadati dal database.

**Vantaggi:**
- Un unico file `.biml` master genera N pacchetti `.dtsx` (uno per tabella configurata)
- Modifiche alla configurazione richiedono solo una ricompilazione del BIML (no editing manuale di pacchetti)
- Codice C# dello Script Component generato automaticamente

---

### 8.2 Struttura Pacchetto SSIS Generato

Ogni tabella configurata produce un pacchetto `.dtsx` con questa struttura:

**Nome pacchetto:** `<LandingSchema>_<LandingTableName>.dtsx`

**Control Flow:**
```
[Sequence Container: Sync <Schema>.<Table>]
  ├── Execute SQL Task: "Truncate Staging"
  ├── Data Flow Task: "Load Data"
  ├── Execute SQL Task: "MERGE to Landing"
  └── Execute SQL Task: "Update _meta.SourceTables"
```

**Data Flow (all'interno del Data Flow Task):**
```
OLE DB Source (sorgente remota)
  └── SELECT <campi configurati> FROM [<SourceDb>].[<Schema>].[<Table>]
       │
       ▼
Script Component (Transformation) — calcolo ChangeHashKey SHA-256
       │
       ▼
OLE DB Destination → [<LandingSchema>].[stg_<LandingTableName>]
```

---

### 8.3 Calcolo ChangeHashKey (Script Component C#)

Il `ChangeHashKey` è calcolato tramite uno **Script Component C#** (tipo: Transformation) all'interno del Data Flow.

**Logica:**
- Concatena i valori dei campi **non-chiave** con separatore `|`, ordinati per `OrdinalPosition`
- I valori `NULL` sono normalizzati alla stringa `"NULL"`
- Calcola l'hash SHA-256 della stringa risultante
- Converte l'hash in rappresentazione esadecimale lowercase (64 caratteri)

**Esempio codice generato dal BIML:**
```csharp
public override void Input0_ProcessInputRow(Input0Buffer Row)
{
    var parts = new List<string>();
    
    if (Row.Field1_IsNull) parts.Add("NULL"); 
    else parts.Add(Row.Field1.ToString());
    
    if (Row.Field2_IsNull) parts.Add("NULL"); 
    else parts.Add(Row.Field2.ToString());
    
    // ... altri campi non-chiave
    
    var raw = string.Join("|", parts);
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    Row.ChangeHashKey = Convert.ToHexString(bytes).ToLowerInvariant();
}
```

---

### 8.4 Logica MERGE (Execute SQL Task)

Dopo il caricamento nella staging table, un **Execute SQL Task** esegue il MERGE dalla staging alla landing table.

**Pseudo-codice MERGE:**
```sql
MERGE [<LandingSchema>].[<LandingTableName>] AS tgt
USING [<LandingSchema>].[stg_<LandingTableName>] AS src
  ON  tgt.[<Key1>] = src.[<Key1>]
  AND tgt.[<Key2>] = src.[<Key2>]

-- Record esistente con dati modificati → UPDATE
WHEN MATCHED AND tgt.ChangeHashKey <> src.ChangeHashKey THEN
    UPDATE SET
        tgt.ChangeHashKey  = src.ChangeHashKey,
        tgt.UpdateDatetime = GETUTCDATE(),
        tgt.IsDeleted      = 0,
        tgt.[Field1]       = src.[Field1],
        tgt.[Field2]       = src.[Field2]

-- Nuovo record → INSERT
WHEN NOT MATCHED BY TARGET THEN
    INSERT (<chiavi>, ChangeHashKey, InsertDatetime, UpdateDatetime, IsDeleted, <campi>)
    VALUES (src.<chiavi>, src.ChangeHashKey, GETUTCDATE(), GETUTCDATE(), 0, src.<campi>)

-- Record cancellato nella sorgente → Soft-delete
WHEN NOT MATCHED BY SOURCE AND tgt.IsDeleted = 0 THEN
    UPDATE SET
        tgt.IsDeleted      = 1,
        tgt.UpdateDatetime = GETUTCDATE();
```

**⚠️ Nota:** il MERGE rileva solo **cambiamenti ai dati** (via `ChangeHashKey`), non gestisce automaticamente le cancellazioni hard. La clausola `WHEN NOT MATCHED BY SOURCE` esegue soft-delete dei record non più presenti nella sorgente.

---

### 8.5 Aggiornamento Stato Sync

Al termine del pacchetto, un **Execute SQL Task** aggiorna `_meta.SourceTables`:

```sql
UPDATE _meta.SourceTables
SET 
    LastSyncAt = GETUTCDATE(),
    LastSyncStatus = 'Success',  -- o 'Error' in caso di fallimento
    LastSyncMessage = NULL       -- o descrizione errore
WHERE Id = ?;  -- parametro passato dal pacchetto
```

---

### 8.6 Rigenerazione BIML

**Workflow:**

1. L'utente modifica la configurazione (aggiunge/modifica tabelle o campi) tramite Web App
2. Preme il pulsante **"Genera BIML"** nell'interfaccia
3. L'API genera il file `MasterTemplate.biml` aggiornato e lo restituisce come download
4. L'utente apre il file `.biml` in **Visual Studio con BimlExpress** (o BimlStudio)
5. Compila il BIML → genera i pacchetti `.dtsx` aggiornati
6. Redistribuisce il progetto SSIS (`.ispac`) nel **SSIS Catalog**
7. I **SQL Server Agent Jobs** eseguono i pacchetti aggiornati secondo schedulazione

---

### 8.7 Schedulazione con SQL Server Agent (FASE 7)

**DW-Builder** automatizza la schedulazione dei pacchetti SSIS tramite **SQL Server Agent jobs**, con configurazione parametrizzata via metadati `_meta.SourceTables` e logging avanzato in `_meta.Logs`.

---

#### 8.7.1 Architettura Schedulazione

```
┌────────────────────────────────────────────────────────────────┐
│  _meta.SourceTables                                            │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ ScheduleEnabled  = 1 (bit)                                │ │
│  │ ScheduleType     = 'Daily' | 'Weekly' | 'Monthly' | ...   │ │
│  │ ScheduleTime     = '02:00:00' (time)                      │ │
│  │ ScheduleDaysOfWeek = 'Monday,Wednesday,Friday'            │ │
│  │ ScheduleFrequency = 1 (int)                               │ │
│  └───────────────────────────────────────────────────────────┘ │
└──────────────────────────┬─────────────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────────────┐
│  CreateJobsForSource.sql (DDL Script)                          │
│                                                                │
│  Legge _meta.SourceTables con ScheduleEnabled = 1             │
│  Crea SQL Server Agent Job per ogni tabella:                  │
│    - Nome: DwBuilder_Source{n}_Table{n}_{TableName}           │
│    - Step: Esegue pacchetto SSIS da SSISDB Catalog           │
│    - Schedule: Basato su ScheduleType/Time/DaysOfWeek        │
└──────────────────────────┬─────────────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────────────┐
│  SQL Server Agent (msdb)                                       │
│  Jobs eseguono pacchetti SSIS su schedule configurata         │
└──────────────────────────┬─────────────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────────────┐
│  SSIS Package Execution (SSISDB Catalog)                       │
│  Esegue ETL: Source → Staging → MERGE → Landing               │
└──────────────────────────┬─────────────────────────────────────┘
                           │
                           ▼
┌────────────────────────────────────────────────────────────────┐
│  _meta.Logs (Enhanced Logging)                                 │
│  ┌───────────────────────────────────────────────────────────┐ │
│  │ JobName, PackageName                                      │ │
│  │ RowsInserted, RowsUpdated, RowsDeleted                    │ │
│  │ ExecutionDurationMs                                        │ │
│  │ ErrorDetails (se fallito)                                  │ │
│  └───────────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────────────┘
```

---

#### 8.7.2 Configurazione Scheduling in _meta.SourceTables

La tabella `_meta.SourceTables` è stata estesa con le seguenti colonne:

| Colonna | Tipo | Descrizione |
|---|---|---|
| `ScheduleEnabled` | `BIT` | Se `1`, il job viene creato e schedulato |
| `ScheduleType` | `NVARCHAR(20)` | Tipo di schedule: `Daily`, `Weekly`, `Monthly`, `OnDemand` |
| `ScheduleTime` | `TIME` | Ora di esecuzione giornaliera (es. `02:00:00`) |
| `ScheduleFrequency` | `INT` | Frequenza: ogni N giorni (Daily), giorno del mese (Monthly) |
| `ScheduleDaysOfWeek` | `NVARCHAR(50)` | Giorni settimana per schedule Weekly (es. `Monday,Wednesday,Friday`) |
| `ScheduleDescription` | `NVARCHAR(500)` | Descrizione human-readable della schedule |

**Esempio configurazione:**

```sql
-- Daily sync alle 2 AM
UPDATE _meta.SourceTables
SET 
    ScheduleEnabled = 1,
    ScheduleType = 'Daily',
    ScheduleTime = '02:00:00',
    ScheduleFrequency = 1, -- Ogni 1 giorno
    ScheduleDescription = 'Daily customer sync at 2 AM'
WHERE SourceId = 1 AND TableName = 'Customers';

-- Weekly sync Lunedì/Mercoledì/Venerdì alle 18:00
UPDATE _meta.SourceTables
SET 
    ScheduleEnabled = 1,
    ScheduleType = 'Weekly',
    ScheduleTime = '18:00:00',
    ScheduleDaysOfWeek = 'Monday,Wednesday,Friday',
    ScheduleDescription = 'Weekly orders sync MWF at 6 PM'
WHERE SourceId = 1 AND TableName = 'Orders';

-- OnDemand (solo esecuzione manuale)
UPDATE _meta.SourceTables
SET 
    ScheduleEnabled = 1,
    ScheduleType = 'OnDemand',
    ScheduleDescription = 'Manual execution only'
WHERE SourceId = 1 AND TableName = 'ProductCatalog';
```

---

#### 8.7.3 Creazione SQL Server Agent Jobs

Lo script `database/SqlAgent/CreateJobsForSource.sql` crea automaticamente i jobs per una sorgente specificata.

**Esecuzione:**

```sql
USE DwBuilderDW;

-- Impostare parametri
DECLARE @SourceId INT = 1; -- ID della sorgente
DECLARE @SsisCatalogFolder NVARCHAR(200) = N'DwBuilder'; -- Folder SSISDB

-- Eseguire lo script
:r database\SqlAgent\CreateJobsForSource.sql
```

**Lo script esegue:**

1. Legge tutte le `SourceTables` con `SourceId = @SourceId` e `ScheduleEnabled = 1`
2. Per ogni tabella:
   - Crea job con naming: `DwBuilder_Source{SourceId}_Table{TableId}_{TableName}`
   - Configura job step che esegue pacchetto SSIS da SSISDB tramite `catalog.create_execution`
   - Configura schedule basato su `ScheduleType`, `ScheduleTime`, `ScheduleDaysOfWeek`
   - Configura logging tramite chiamata a `[_meta].[usp_LogJobExecution]` dopo esecuzione
3. Jobs creati in stato **enabled** (esecuzione automatica attiva)

**Job Step Command (generato automaticamente):**

```sql
-- Step: Execute SSIS Package
DECLARE @execution_id BIGINT;

EXEC [SSISDB].[catalog].[create_execution]
    @package_name = N'Customers.dtsx',
    @folder_name = N'DwBuilder',
    @project_name = N'ERP',
    @use32bitruntime = 0,
    @execution_id = @execution_id OUTPUT;

EXEC [SSISDB].[catalog].[start_execution] @execution_id;

-- Polling per completamento
WHILE 1=1
BEGIN
    SELECT @status = [status] 
    FROM [SSISDB].[catalog].[executions]
    WHERE execution_id = @execution_id;
    
    IF @status IN (4, 7) -- Succeeded / Stopped
        BREAK;
    ELSE IF @status IN (5, 6) -- Failed / Cancelled
        RAISERROR('Package execution failed.', 16, 1);
    
    WAITFOR DELAY '00:00:05'; -- Poll ogni 5 secondi
END

-- Log risultato
EXEC [_meta].[usp_LogJobExecution]
    @JobName = N'DwBuilder_Source1_Table5_Customers',
    @PackageName = N'Customers.dtsx',
    @Status = 'Success',
    @DurationMs = <durata_calcolata>;
```

---

#### 8.7.4 Enhanced Logging (_meta.Logs)

La tabella `_meta.Logs` è stata estesa per tracciare esecuzioni SQL Agent:

| Colonna Aggiunta | Tipo | Descrizione |
|---|---|---|
| `JobName` | `NVARCHAR(200)` | Nome SQL Server Agent job |
| `JobExecutionId` | `UNIQUEIDENTIFIER` | ID univoco esecuzione (correlazione con msdb) |
| `PackageName` | `NVARCHAR(200)` | Nome pacchetto SSIS eseguito |
| `RowsInserted` | `INT` | Numero righe inserite |
| `RowsUpdated` | `INT` | Numero righe aggiornate |
| `RowsDeleted` | `INT` | Numero righe soft-deleted |
| `ExecutionDurationMs` | `INT` | Durata esecuzione in millisecondi |
| `ErrorDetails` | `NVARCHAR(MAX)` | Dettaglio errore se fallito |

**Stored Procedure: [_meta].[usp_LogJobExecution]**

```sql
EXEC [_meta].[usp_LogJobExecution]
    @JobName = N'DwBuilder_Source1_Table1_Customers',
    @PackageName = N'Customers.dtsx',
    @Status = 'Success',
    @RowsInserted = 150,
    @RowsUpdated = 25,
    @RowsDeleted = 3,
    @DurationMs = 12350,
    @ErrorDetails = NULL;
```

Questa stored procedure:
- Inserisce record in `_meta.Logs`
- Imposta `Level` = `Information` per successo, `Error` per fallimento
- Genera `JobExecutionId` univoco per correlazione
- Costruisce messaggio di log strutturato con metriche

---

#### 8.7.5 Monitoring Job Execution

**Query Recent Job Executions:**

```sql
-- Ultimi 50 job eseguiti con metriche
SELECT TOP 50
    Timestamp,
    Level,
    JobName,
    PackageName,
    RowsInserted,
    RowsUpdated,
    RowsDeleted,
    ExecutionDurationMs,
    CASE WHEN Level = 'Error' THEN ErrorDetails ELSE NULL END AS ErrorDetails
FROM [_meta].[Logs]
WHERE JobName IS NOT NULL
ORDER BY Timestamp DESC;
```

**Query Performance Summary (Last 7 Days):**

```sql
SELECT 
    j.name AS JobName,
    COUNT(*) AS Executions,
    SUM(CASE WHEN h.run_status = 1 THEN 1 ELSE 0 END) AS Successes,
    SUM(CASE WHEN h.run_status = 0 THEN 1 ELSE 0 END) AS Failures,
    AVG(l.ExecutionDurationMs) / 1000.0 AS AvgDurationSeconds,
    AVG(l.RowsInserted + l.RowsUpdated + l.RowsDeleted) AS AvgRowsProcessed
FROM msdb.dbo.sysjobs j
INNER JOIN msdb.dbo.sysjobhistory h ON j.job_id = h.job_id
LEFT JOIN [_meta].[Logs] l 
    ON l.JobName = j.name
    AND ABS(DATEDIFF(SECOND, msdb.dbo.agent_datetime(h.run_date, h.run_time), l.Timestamp)) < 60
WHERE j.name LIKE 'DwBuilder_%'
  AND h.step_id = 0
  AND msdb.dbo.agent_datetime(h.run_date, h.run_time) >= DATEADD(DAY, -7, GETDATE())
GROUP BY j.name
ORDER BY Failures DESC, AvgDurationSeconds DESC;
```

---

#### 8.7.6 Deployment Options

**Opzione 1: SSISDB Catalog Deployment (Consigliata)**

- Pacchetti deployati in `SSISDB/DwBuilder/{SourceName}/{TableName}.dtsx`
- Gestione centralizzata, logging integrato, environment variables
- Job step usa `catalog.create_execution` e `catalog.start_execution`

**Opzione 2: File System Deployment**

- Pacchetti salvati in `C:\SSIS\DwBuilder\{SourceName}\{TableName}.dtsx`
- Job step usa `dtexec` tramite `xp_cmdshell`
- Richiede configurazione permessi file system

**La documentazione e gli script forniti supportano SSISDB Catalog deployment.**

---

#### 8.7.7 Maintenance Operations

**Disable Jobs Temporarily (Maintenance Window):**

```sql
DECLARE @JobName NVARCHAR(200);

DECLARE job_cursor CURSOR FOR
SELECT name FROM msdb.dbo.sysjobs WHERE name LIKE 'DwBuilder_%';

OPEN job_cursor;
FETCH NEXT FROM job_cursor INTO @JobName;

WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC msdb.dbo.sp_update_job @job_name = @JobName, @enabled = 0;
    FETCH NEXT FROM job_cursor INTO @JobName;
END

CLOSE job_cursor;
DEALLOCATE job_cursor;
```

**Re-enable After Maintenance:**

```sql
-- Stesso pattern con @enabled = 1
```

**Update Schedule:**

```sql
-- Modificare configurazione in _meta.SourceTables
UPDATE _meta.SourceTables
SET ScheduleTime = '03:00:00'
WHERE SourceId = 1 AND TableName = 'Customers';

-- Ri-eseguire CreateJobsForSource.sql (idempotente: drop + recreate)
```

---

#### 8.7.8 Troubleshooting

| Problema | Soluzione |
|---|---|
| **Jobs non creati** | Verificare SQL Server Agent running: `EXEC xp_servicecontrol 'QueryState', N'SQLServerAGENT';` |
| **Job fallisce: "Package not found"** | Verificare deploy pacchetti in SSISDB: `SELECT * FROM SSISDB.catalog.packages;` |
| **Schedule non triggerata** | Verificare job e schedule enabled in `msdb.dbo.sysjobs` e `msdb.dbo.sysschedules` |
| **Logs non popolati** | Verificare `usp_LogJobExecution` esista e sia chiamata nello step del job |

**Diagnostic Query:**

```sql
-- Verifica stato SQL Server Agent
EXEC xp_servicecontrol 'QueryState', N'SQLServerAGENT';

-- Verifica SSISDB
SELECT name FROM sys.databases WHERE name = 'SSISDB';

-- Verifica pacchetti deployati
SELECT f.name AS Folder, p.name AS Project, pkg.name AS Package
FROM SSISDB.catalog.folders f
INNER JOIN SSISDB.catalog.projects p ON f.folder_id = p.folder_id
INNER JOIN SSISDB.catalog.packages pkg ON p.project_id = pkg.project_id
WHERE f.name = 'DwBuilder';
```

---

#### 8.7.9 Alerting (Email Notifications)

**Setup Database Mail:**

```sql
-- Enable Database Mail
EXEC sp_configure 'Database Mail XPs', 1;
RECONFIGURE;

-- Configurare mail profile in SSMS:
-- Management > Database Mail > Configure Database Mail
```

**Create SQL Agent Operator:**

```sql
EXEC msdb.dbo.sp_add_operator
    @name = N'DwBuilderAdmin',
    @enabled = 1,
    @email_address = N'dwbuilder-admin@company.com';
```

**Configure Job Failure Notification:**

```sql
EXEC msdb.dbo.sp_update_job
    @job_name = N'DwBuilder_Source1_Table1_Customers',
    @notify_level_email = 2, -- 0=Never, 1=Success, 2=Failure, 3=Always
    @notify_email_operator_name = N'DwBuilderAdmin';
```

---

#### 8.7.10 Riferimenti

- **Script DDL:** `database/SqlAgent/CreateJobsForSource.sql`
- **Stored Procedure:** `database/SqlAgent/usp_LogJobExecution.sql`
- **Test Scenarios:** `database/SqlAgent/TestScenarios/README.md`
- **Setup Guide:** `database/SqlAgent/README.md`
- **Microsoft Docs:** [SQL Server Agent](https://learn.microsoft.com/en-us/sql/ssms/agent/sql-server-agent)

---

## 9. Roadmap

| Fase | Descrizione | Stato | Issue GitHub |
|---|---|---|---|
| **FASE 1** | Setup solution, modello metadati, EF Core Migrations, API CRUD sorgenti, Identity + JWT, Serilog | ✅ Completata | [#1](https://github.com/bibe74cp/dw-builder/issues/1) |
| **FASE 2** | Connettività sorgenti: test connessione, lettura schema, API tabelle e campi | ⏳ Da fare | [#2](https://github.com/bibe74cp/dw-builder/issues/2) |
| **FASE 3** | Generatore DDL landing tables e applicazione al DW | ⏳ Da fare | [#3](https://github.com/bibe74cp/dw-builder/issues/3) |
| **FASE 4** | Motore BIML: template master, generazione Script Component C#, download `.biml` | ⏳ Da fare | [#4](https://github.com/bibe74cp/dw-builder/issues/4) |
| **FASE 5** | Struttura pacchetti SSIS: staging, MERGE, aggiornamento `_meta`, test compilazione BIML | ⏳ Da fare | [#5](https://github.com/bibe74cp/dw-builder/issues/5) |
| **FASE 6** | Frontend React: dashboard, gestione sorgenti, configurazione tabelle/campi, pulsante Genera BIML | ⏳ Da fare | [#6](https://github.com/bibe74cp/dw-builder/issues/6) |
| **FASE 7** | SQL Server Agent Jobs, logging avanzato, notifiche errori | ⏳ Da fare | [#7](https://github.com/bibe74cp/dw-builder/issues/7) |
| **FASE 8** | Test end-to-end, hardening sicurezza, packaging, documentazione deploy | ⏳ Da fare | [#8](https://github.com/bibe74cp/dw-builder/issues/8) |

---

## 10. Convenzioni di Sviluppo

### 10.1 Naming Conventions C#

- **Classi, interfacce, record:** `PascalCase` (es. `SourceRepository`, `IEncryptionService`, `LoginRequest`)
- **Metodi pubblici:** `PascalCase` (es. `GetAllActiveAsync`, `Encrypt`)
- **Metodi privati:** `PascalCase` (es. `UpdateTimestamps`)
- **Proprietà pubbliche:** `PascalCase` (es. `ServerName`, `IsActive`)
- **Parametri e variabili locali:** `camelCase` (es. `sourceId`, `plainText`)
- **Campi privati:** `_camelCase` con underscore (es. `_context`, `_key`)
- **Costanti:** `PascalCase` (es. `DefaultPort`)

### 10.2 Pattern Repository

L'accesso ai dati è gestito tramite **pattern Repository**:
- Interfaccia `ISourceRepository` in `DwBuilder.Core.Interfaces`
- Implementazione `SourceRepository` in `DwBuilder.Infrastructure.Repositories`
- Registrazione DI in `Program.cs`: `services.AddScoped<ISourceRepository, SourceRepository>()`

### 10.3 Soft Delete

Tutte le entità principali (Source, SourceTable) utilizzano **soft-delete** tramite il flag `IsActive`:
- `DELETE` logico → `IsActive = false`
- Le query filtrano automaticamente i record con `IsActive = false`
- Non eliminare mai fisicamente record con relazioni (CASCADE soft-delete se necessario)

### 10.4 Timestamp Automatici

Il `DwBuilderDbContext` gestisce automaticamente i timestamp `CreatedAt` e `UpdatedAt` tramite override di `SaveChangesAsync()`:
- `CreatedAt` impostato solo su `EntityState.Added`
- `UpdatedAt` aggiornato su `EntityState.Added` e `EntityState.Modified`

```csharp
private void UpdateTimestamps()
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
    
    var now = DateTimeOffset.UtcNow;
    
    foreach (var entry in entries)
    {
        if (entry.State == EntityState.Added)
        {
            if (entry.Property("CreatedAt").CurrentValue == null || 
                (DateTimeOffset)entry.Property("CreatedAt").CurrentValue == default)
            {
                entry.Property("CreatedAt").CurrentValue = now;
            }
        }
        
        if (entry.Property("UpdatedAt") != null)
        {
            entry.Property("UpdatedAt").CurrentValue = now;
        }
    }
}
```

### 10.5 Gestione Eccezioni e Logging

- Usare Serilog per logging strutturato
- Loggare sempre operazioni critiche (login, creazione/modifica/eliminazione sorgenti)
- Non loggare mai dati sensibili (password, token JWT)
- Eccezioni gestite nei controller con response appropriati (400, 404, 500)

### 10.6 Testing

- **Unit test:** `DwBuilder.Core.Tests`, `DwBuilder.Infrastructure.Tests`
- Framework: **xUnit**
- Mock: **Moq** / **NSubstitute**
- Test EF Core: usare **In-Memory Database** o **SQLite**

### 10.7 Configurazioni EF Core

Le configurazioni delle entità sono separate in classi dedicate `IEntityTypeConfiguration<T>`:
- `SourceConfiguration`
- `SourceTableConfiguration`
- `SourceFieldConfiguration`
- `LogConfiguration`

Applicate automaticamente tramite:
```csharp
modelBuilder.ApplyConfigurationsFromAssembly(typeof(DwBuilderDbContext).Assembly);
```

### 10.8 Migration EF Core

Comando per aggiungere una migration:
```bash
cd src/DwBuilder.Infrastructure
dotnet ef migrations add <NomeMigration> --startup-project ../DwBuilder.Api
```

Comando per applicare migration:
```bash
dotnet ef database update --startup-project ../DwBuilder.Api
```

Le migration vengono applicate automaticamente all'avvio tramite:
```csharp
app.MigrateDatabase();
```

---

## Appendice A — Comandi Utili

### A.1 Build Solution

```bash
dotnet build dw-builder.slnx
```

### A.2 Run API

```bash
cd src/DwBuilder.Api
dotnet run
```

### A.3 Run Tests

```bash
dotnet test
```

### A.4 Generare Migration

```bash
cd src/DwBuilder.Infrastructure
dotnet ef migrations add <NomeMigration> --startup-project ../DwBuilder.Api
```

### A.5 Applicare Migration

```bash
cd src/DwBuilder.Infrastructure
dotnet ef database update --startup-project ../DwBuilder.Api
```

### A.6 Publish per Deploy

```bash
dotnet publish src/DwBuilder.Api/DwBuilder.Api.csproj -c Release -o C:\Deploy\DwBuilder
```

---

## Appendice B — Riferimenti

- **ASP.NET Core 10:** [https://learn.microsoft.com/en-us/aspnet/core/](https://learn.microsoft.com/en-us/aspnet/core/)
- **Entity Framework Core 10:** [https://learn.microsoft.com/en-us/ef/core/](https://learn.microsoft.com/en-us/ef/core/)
- **React 18:** [https://react.dev/](https://react.dev/)
- **Ant Design:** [https://ant.design/](https://ant.design/)
- **BIML:** [https://www.varigence.com/biml](https://www.varigence.com/biml)
- **Serilog:** [https://serilog.net/](https://serilog.net/)

---

## Appendice C — Contatti e Supporto

**Team:** Codice Plastico  
**Email:** info@codiceplastico.com  
**GitHub:** [https://github.com/bibe74cp/dw-builder](https://github.com/bibe74cp/dw-builder)

---

**Fine Documentazione**
