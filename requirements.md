# DW-Builder — Specifiche di Progetto

## 1. Panoramica

**DW-Builder** è una piattaforma web per la configurazione e l'automazione di un Data Warehouse SQL Server alimentato da molteplici sorgenti eterogenee (applicativi ERP, CRM, gestionali, ecc.), ciascuno su istanze SQL Server distinte nella rete locale.

Il sistema si articola in due macro-componenti:

| Componente | Descrizione |
|---|---|
| **Web App di configurazione** | Interfaccia per definire sorgenti, tabelle, campi e mapping verso il DW |
| **ETL Engine** | Motore che legge i metadati e sincronizza i dati dalle sorgenti al DW |

---

## 2. Architettura di Sistema

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

### 2.1 Flusso dati (ETL — SSIS + BIML)

1. DW-Builder genera un file `.biml` master che incorpora BimlScript in grado di leggere i metadati dallo schema `_meta` del DW.
2. Il file `.biml`, compilato tramite BimlExpress (Visual Studio) o BimlStudio, produce **un pacchetto SSIS `.dtsx` per ciascuna tabella configurata**.
3. Ogni pacchetto SSIS:
   a. Legge dalla tabella sorgente (OLE DB Source) le colonne configurate.
   b. Calcola il `ChangeHashKey` SHA-256 internamente tramite uno **Script Component** C# nel Data Flow.
   c. Esegue un `MERGE` sulla tabella di landing corrispondente (insert / update / soft-delete) tramite OLE DB Command o staging table + Execute SQL Task.
4. I pacchetti vengono organizzati in un progetto SSIS (`.ispac`) e distribuiti nel **SSIS Catalog** del server DW.
5. La schedulazione avviene tramite **SQL Server Agent Jobs**, uno per sorgente (che esegue in sequenza i pacchetti delle tabelle di quella sorgente).
6. Al termine di ogni pacchetto, una Execute SQL Task aggiorna `LastSyncAt`, `LastSyncStatus` e `LastSyncMessage` in `_meta.SourceTables`.

---

## 3. Stack Tecnologico

| Layer | Tecnologia |
|---|---|
| **Backend API** | ASP.NET Core 10, C# 13 |
| **Frontend** | React 18, TypeScript, Vite |
| **UI Components** | Ant Design (antd) |
| **ORM / Data Access** | Entity Framework Core 10 (Code First) |
| **Database Metadati** | SQL Server (schema `_meta` all'interno del DW) |
| **Connettività sorgenti** | Microsoft.Data.SqlClient (lettura schema) |
| **ETL** | SQL Server Integration Services (SSIS) |
| **Generazione pacchetti ETL** | BIML (BimlScript) — un file `.biml` master |
| **Compilazione BIML** | BimlExpress (Visual Studio) / BimlStudio |
| **Hashing (ETL)** | SHA-256 in Script Component C# all'interno del pacchetto SSIS |
| **Schedulazione ETL** | SQL Server Agent Jobs |
| **Autenticazione** | ASP.NET Core Identity + JWT (utenti locali) |

---

## 4. Struttura della Solution

```
dw-builder.sln
│
├── src/
│   ├── DwBuilder.Api/            # ASP.NET Core 10 Web API + hosting SPA
│   ├── DwBuilder.Core/           # Modelli di dominio, interfacce, DTOs
│   ├── DwBuilder.Infrastructure/ # EF Core, connettività SQL, generazione DDL
│   ├── DwBuilder.Biml/           # Template .biml master + generatore BimlScript
│   └── DwBuilder.Web/            # Frontend React (Vite)
│
└── tests/
    ├── DwBuilder.Core.Tests/
    └── DwBuilder.Infrastructure.Tests/
```

### 4.1 Progetto `DwBuilder.Biml`

Contiene:
- `MasterTemplate.biml` — file BIML con BimlScript che si connette a `_meta` e genera un pacchetto `.dtsx` per ciascuna tabella attiva.
- `BimlUtilities.cs` — classi helper richiamate dallo script BIML per la generazione del codice C# degli Script Component (calcolo hash).
- Il progetto espone un servizio `IBimlGenerator` usato dall'API per rigenerare e scaricare il file `.biml` aggiornato.

---

## 5. Modello dei Metadati

Tutti i metadati sono persistiti nello schema `_meta` del database DW.

### 5.1 `_meta.Sources` — Sorgenti applicative

| Colonna | Tipo | Note |
|---|---|---|
| `Id` | INT IDENTITY PK | |
| `Name` | NVARCHAR(100) | Nome descrittivo (es. "ERP Aziendale") |
| `ServerName` | NVARCHAR(200) | Nome server o IP |
| `InstanceName` | NVARCHAR(100) | Istanza SQL Server (null = default) |
| `DatabaseName` | NVARCHAR(200) | Database sorgente |
| `LandingSchema` | NVARCHAR(100) | Schema nel DW (es. `ERP`) |
| `ConnectionUser` | NVARCHAR(200) | Utente SQL (null = Windows Auth) |
| `ConnectionPasswordEncrypted` | NVARCHAR(500) | Password cifrata con AES-256 |
| `IsActive` | BIT | |
| `CreatedAt` | DATETIME2 | |
| `UpdatedAt` | DATETIME2 | |

> **Nota sicurezza**: le password sono cifrate con AES-256 usando una chiave applicativa (configuration secret). Non vengono mai esposte in chiaro tramite API.

### 5.2 `_meta.SourceTables` — Tabelle sorgente selezionate

| Colonna | Tipo | Note |
|---|---|---|
| `Id` | INT IDENTITY PK | |
| `SourceId` | INT FK → Sources | |
| `SchemaName` | NVARCHAR(100) | Schema sorgente (es. `dbo`) |
| `TableName` | NVARCHAR(200) | Nome tabella sorgente (es. `Customer`) |
| `LandingTableName` | NVARCHAR(200) | Nome tabella nel DW (es. `Customer`); default = `TableName` |
| `IsActive` | BIT | |
| `LastSyncAt` | DATETIME2 | Data/ora ultima sincronizzazione |
| `LastSyncStatus` | NVARCHAR(50) | `Success` / `Error` / `Running` |
| `LastSyncMessage` | NVARCHAR(MAX) | Messaggio di errore, se presente |
| `CreatedAt` | DATETIME2 | |
| `UpdatedAt` | DATETIME2 | |

### 5.3 `_meta.SourceFields` — Campi selezionati per ciascuna tabella

| Colonna | Tipo | Note |
|---|---|---|
| `Id` | INT IDENTITY PK | |
| `SourceTableId` | INT FK → SourceTables | |
| `SourceColumnName` | NVARCHAR(200) | Nome colonna sorgente |
| `LandingColumnName` | NVARCHAR(200) | Nome colonna nel DW (rinominabile) |
| `SqlDataType` | NVARCHAR(100) | Tipo dati rilevato dalla sorgente (es. `nvarchar(50)`) |
| `IsBusinessKey` | BIT | Se `1`, fa parte della chiave di business |
| `IsNullable` | BIT | Rilevato dalla sorgente |
| `OrdinalPosition` | INT | Ordine nel SELECT e nella tabella di landing |
| `CreatedAt` | DATETIME2 | |
| `UpdatedAt` | DATETIME2 | |

---

## 6. Struttura delle Tabelle di Landing

Per ogni tabella sorgente configurata viene generata (o aggiornata) una tabella nel DW con la seguente struttura:

```sql
CREATE TABLE [<LandingSchema>].[<LandingTableName>] (

    -- Chiave/i di business (esempio)
    [DataAreaId]      NVARCHAR(10)   NOT NULL,
    [AccountCode]     NVARCHAR(20)   NOT NULL,

    -- Colonna tecnica: hash dei campi non-chiave
    [ChangeHashKey]   CHAR(64)       NOT NULL,

    -- Colonne tecniche temporali
    [InsertDatetime]  DATETIME2      NOT NULL,
    [UpdateDatetime]  DATETIME2      NOT NULL,

    -- Soft delete
    [IsDeleted]       BIT            NOT NULL DEFAULT 0,

    -- Campi non-chiave selezionati (rinominati se necessario)
    [FieldA]          <tipo>         NULL,
    [FieldB]          <tipo>         NULL,
    ...

    CONSTRAINT [PK_<LandingSchema>_<LandingTableName>]
        PRIMARY KEY CLUSTERED (<chiavi di business>)
);
```

### 6.1 Regole dei campi tecnici

| Campo | Comportamento INSERT | Comportamento UPDATE | Comportamento soft-delete |
|---|---|---|---|
| `ChangeHashKey` | SHA-256 dei valori non-chiave | Ricalcolato ad ogni sync | Invariato |
| `InsertDatetime` | `GETUTCDATE()` | Invariato | Invariato |
| `UpdateDatetime` | `GETUTCDATE()` | `GETUTCDATE()` | `GETUTCDATE()` |
| `IsDeleted` | `0` | `0` (se record esistente) | `1` |

### 6.2 Calcolo del ChangeHashKey

Il `ChangeHashKey` è calcolato **all'interno del pacchetto SSIS** tramite uno Script Component C# (tipo: Transformation) nel Data Flow.

Logica di calcolo:
- I valori `NULL` sono normalizzati alla stringa `"NULL"`.
- I campi non-chiave sono concatenati con separatore `|`, ordinati per `OrdinalPosition`.
- L'hash SHA-256 della stringa risultante viene convertito in rappresentazione esadecimale lowercase (64 caratteri).

```csharp
// Esempio codice Script Component SSIS (C#)
var parts = new[] { Row.FieldA_IsNull ? "NULL" : Row.FieldA,
                    Row.FieldB_IsNull ? "NULL" : Row.FieldB.ToString() };
var raw = string.Join("|", parts);
var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
Row.ChangeHashKey = Convert.ToHexString(bytes).ToLowerInvariant();
```

Il codice C# dello Script Component è generato automaticamente dal file `.biml` in base ai campi configurati nei metadati.

---

## 7. Struttura dei Pacchetti SSIS e Logica MERGE

### 7.1 Struttura del pacchetto `.dtsx` (generato da BIML)

Ogni pacchetto ha il nome `<LandingSchema>_<LandingTableName>.dtsx` e contiene:

**Control Flow**
```
[Sequence Container: Sync <Schema>.<Table>]
  ├── Data Flow Task: "Load <LandingSchema>.<LandingTableName>"
  └── Execute SQL Task: "Update _meta.SourceTables" (LastSyncAt, LastSyncStatus)
```

**Data Flow** (all'interno del Data Flow Task)
```
OLE DB Source
  └── [legge SELECT <campi configurati> FROM [<SourceDb>].[<Schema>].[<Table>]]
       │
       ▼
Script Component (Transformation) — calcolo ChangeHashKey
       │
       ▼
OLE DB Destination → tabella staging [<LandingSchema>].[stg_<LandingTableName>]
```

Successivamente, un **Execute SQL Task** nel Control Flow esegue lo statement `MERGE` dalla staging table alla landing table.

### 7.2 Logica MERGE

```sql
MERGE [<LandingSchema>].[<LandingTableName>] AS tgt
USING [<LandingSchema>].[stg_<LandingTableName>] AS src
  ON  tgt.[<Key1>] = src.[<Key1>]
  AND tgt.[<Key2>] = src.[<Key2>]
WHEN MATCHED AND tgt.ChangeHashKey <> src.ChangeHashKey THEN
    UPDATE SET
        tgt.ChangeHashKey  = src.ChangeHashKey,
        tgt.UpdateDatetime = GETUTCDATE(),
        tgt.IsDeleted      = 0,
        tgt.[Field1]       = src.[Field1], ...
WHEN NOT MATCHED BY TARGET THEN
    INSERT (<chiavi>, ChangeHashKey, InsertDatetime, UpdateDatetime, IsDeleted, <altri campi>)
    VALUES (src.<chiavi>, src.ChangeHashKey, GETUTCDATE(), GETUTCDATE(), 0, src.<altri campi>)
WHEN NOT MATCHED BY SOURCE AND tgt.IsDeleted = 0 THEN
    UPDATE SET
        tgt.IsDeleted      = 1,
        tgt.UpdateDatetime = GETUTCDATE();
```

La tabella staging `stg_<LandingTableName>` viene troncata (`TRUNCATE`) all'inizio di ogni esecuzione.

### 7.3 File BIML Master

Il file `MasterTemplate.biml` usa **BimlScript** (C# embedded) per:
1. Connettersi a `_meta` e leggere l'elenco di sorgenti, tabelle e campi attivi.
2. Per ciascuna tabella attiva, generare:
   - Le `<Connection>` OLE DB verso sorgente e DW.
   - Il pacchetto `.dtsx` con Data Flow, Script Component (con codice C# del calcolo hash iniettato) e Execute SQL Task per il MERGE.
3. Generare uno **Sequence Package** per sorgente che esegue in sequenza tutti i pacchetti della sorgente.

### 7.4 Rigenerazione BIML

Ogni volta che la configurazione dei metadati cambia (nuova tabella, modifica campi), l'utente può:
- Premere **"Genera BIML"** nell'interfaccia web → download del file `.biml` aggiornato.
- Compilare il `.biml` in SSDT/BimlStudio per rigenerare i pacchetti `.dtsx`.
- Ridistribuire il progetto SSIS (`.ispac`) nel SSIS Catalog.

---

## 8. Web Application — Funzionalità

### 8.1 Dashboard

- Elenco sorgenti con stato (attiva/inattiva), numero tabelle configurate, data/ora ultima sincronizzazione, stato ultimo sync (icona verde/rosso/warning).
- Pulsante "Sync Now" per avviare manualmente la sincronizzazione di una sorgente.
- Log degli ultimi sync con dettaglio degli errori.

### 8.2 Gestione Sorgenti (`/sources`)

**Lista sorgenti**
- Tabella con: Nome, Server\Istanza, Database, Schema Landing, Stato, Azioni (Modifica, Elimina, Test connessione).

**Creazione / Modifica sorgente** — form con:
- Nome descrittivo (obbligatorio)
- Server (obbligatorio)
- Istanza SQL Server (opzionale; se vuoto usa connessione `SERVER`)
- Database sorgente (obbligatorio)
- Schema Landing nel DW (obbligatorio; validazione: solo caratteri alfanumerici e `_`)
- Autenticazione: Windows Auth (default) oppure SQL Auth (user + password)
- Pulsante **"Testa connessione"** che verifica la raggiungibilità prima del salvataggio

**Eliminazione sorgente**
- Richiede conferma esplicita
- Disattiva sorgente e relativa configurazione (soft-delete logico); non elimina dati fisici nel DW

### 8.3 Selezione Tabelle (`/sources/:id/tables`)

- Carica l'elenco di tabelle e viste disponibili nel database sorgente (da `INFORMATION_SCHEMA.TABLES`).
- Mostra checkbox per selezionare/deselezionare le tabelle da includere nel DW.
- Le tabelle già configurate sono pre-selezionate.
- Il campo **"Nome tabella nel DW"** è editabile inline (default: nome sorgente).
- Salvataggio bulk della selezione.

### 8.4 Configurazione Campi (`/sources/:id/tables/:tableId/fields`)

- Carica le colonne della tabella sorgente (da `INFORMATION_SCHEMA.COLUMNS`).
- Per ciascuna colonna mostra:
  - Checkbox di inclusione
  - Nome colonna sorgente (read-only)
  - Tipo dato sorgente (read-only)
  - Checkbox **"Chiave di Business"** (almeno una obbligatoria)
  - Campo testo **"Nome nel DW"** (editabile; default: nome sorgente)
- I campi tecnici (`ChangeHashKey`, `InsertDatetime`, `UpdateDatetime`, `IsDeleted`) sono mostrati come informativi, non configurabili.
- Anteprima DDL generato (drawer/modal a destra).

### 8.5 Generazione e Applicazione DDL (`/sources/:id/tables/:tableId/ddl`)

- Mostra lo script SQL `CREATE TABLE` / `ALTER TABLE` generato.
- Pulsante **"Applica al DW"**: esegue lo script sul database DW (con conferma utente).
- Pulsante **"Scarica SQL"**: scarica il file `.sql`.
- Gestione `ALTER TABLE` incrementale in caso di modifica campi (aggiunta colonne; rimozione non automatica per sicurezza — solo warning).

### 8.6 Impostazioni (`/settings`)

- Configurazione stringa di connessione al database DW (server, istanza, db, auth).
- Configurazione schedulazione ETL (cron expression con helper visuale).
- Gestione utenti dell'applicazione.

---

## 9. API REST

Base path: `/api/v1`

| Metodo | Path | Descrizione |
|---|---|---|
| GET | `/sources` | Lista sorgenti |
| POST | `/sources` | Crea sorgente |
| GET | `/sources/:id` | Dettaglio sorgente |
| PUT | `/sources/:id` | Modifica sorgente |
| DELETE | `/sources/:id` | Disattiva sorgente |
| POST | `/sources/:id/test-connection` | Test connessione |
| GET | `/sources/:id/available-tables` | Tabelle disponibili sulla sorgente |
| GET | `/sources/:id/tables` | Tabelle configurate |
| PUT | `/sources/:id/tables` | Salva selezione tabelle (bulk) |
| GET | `/sources/:id/tables/:tableId/available-fields` | Colonne disponibili sulla sorgente |
| GET | `/sources/:id/tables/:tableId/fields` | Campi configurati |
| PUT | `/sources/:id/tables/:tableId/fields` | Salva configurazione campi (bulk) |
| GET | `/sources/:id/tables/:tableId/ddl` | Genera DDL |
| POST | `/sources/:id/tables/:tableId/apply-ddl` | Applica DDL al DW |
| GET | `/biml` | Genera e scarica il file `.biml` master aggiornato |
| GET | `/sync/logs` | Log sincronizzazioni |

---

## 10. Sicurezza

- Autenticazione tramite ASP.NET Core Identity + JWT Bearer Token.
- Le password delle sorgenti sono cifrate con AES-256 (chiave da `appsettings.json` / environment variable).
- Tutte le query verso i database sorgente usano parametri (no SQL injection).
- La connessione al DW usa un utente SQL con permessi limitati agli schemi di landing e `_meta`.
- Rate limiting sulle API di sync manuale (max 1 sync per sorgente ogni 60 secondi).
- CORS configurato esplicitamente (no wildcard in produzione).

---

## 11. Requisiti Non Funzionali

- **Portabilità**: l'applicazione deve girare su Windows Server con IIS o come servizio Windows (Kestrel); i pacchetti SSIS richiedono SQL Server Integration Services installato sul server DW.
- **Logging**: Serilog con sink su file e su tabella `_meta.Logs` nel DW; i pacchetti SSIS scrivono su SSIS Catalog log e aggiornano `_meta.SourceTables`.
- **Configurazione**: tutte le impostazioni sensibili tramite environment variables o `appsettings.Production.json` (escluso da git); la connection string al DW è parametrizzata anche nel file `.biml`.
- **Migrazione DB**: gestita tramite EF Core Migrations; eseguita automaticamente all'avvio.
- **Performance ETL**: la gestione di grandi volumi è delegata a SSIS (pipeline nativa ottimizzata); la tabella staging viene troncata e ricaricata ad ogni sync.
- **Idempotenza**: il processo ETL è idempotente: una doppia esecuzione sullo stesso set di dati non produce effetti collaterali (il MERGE è deterministico).

---

## 12. Fasi di Sviluppo (Roadmap)

| Fase | Contenuto |
|---|---|
| **Fase 1** | Setup solution, modello metadati, EF Core 10 Migrations, API CRUD sorgenti |
| **Fase 2** | Connettività sorgenti (test connessione, lettura schema), API tabelle e campi |
| **Fase 3** | Generatore DDL landing tables e applicazione al DW |
| **Fase 4** | Motore BIML: template master, generazione Script Component C# per hash, download `.biml` |
| **Fase 5** | Struttura pacchetti SSIS (staging table, MERGE, aggiornamento `_meta`), test compilazione BIML |
| **Fase 6** | Frontend React (dashboard, gestione sorgenti, configurazione tabelle/campi, pulsante Genera BIML) |
| **Fase 7** | Configurazione SQL Server Agent Jobs, logging, notifiche errori |
| **Fase 8** | Test, hardening sicurezza, packaging |
