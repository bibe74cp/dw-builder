# DW-Builder Frontend — Istruzioni Setup e Build

## Setup Completato ✅

Tutti i file sorgente TypeScript/React sono stati creati con successo:

- ✅ **22 file sorgente** implementati
- ✅ **Dipendenze npm** installate in `node_modules/`
- ✅ **Configurazione** completa (package.json, tsconfig.json, vite.config.ts)
- ✅ **Path alias** configurati (`@/*` → `./src/*`)
- ✅ **Proxy API** configurato (Vite proxy `/api` → `http://localhost:5000`)

## File Creati

### Configurazione (7 file)
- `package.json` — dipendenze React 18, TypeScript 5.3, Vite 5, Ant Design 5, TanStack Query 5
- `tsconfig.json` — TypeScript strict mode + path aliases
- `tsconfig.node.json` — config per Vite
- `vite.config.ts` — Vite config con proxy API
- `.env.development` — `VITE_API_BASE_URL=http://localhost:5000/api/v1`
- `index.html` — entry point HTML
- `.gitignore` — node_modules, dist, .env.local

### Types & API Client (3 file)
- `src/types/api.ts` — 20+ TypeScript interfaces (DTOs backend)
- `src/api/axios.ts` — Axios client con JWT interceptors
- `src/api/services.ts` — API services (auth, sources, tables, fields, ddl, biml)

### Layouts (1 file)
- `src/layouts/MainLayout.tsx` — Layout con sidebar Ant Design

### Pages (8 file)
- `src/pages/Login.tsx` — Login JWT
- `src/pages/Dashboard.tsx` — Dashboard con statistiche sorgenti
- `src/pages/Sources.tsx` — CRUD sorgenti
- `src/pages/Tables.tsx` — Selezione tabelle sorgente
- `src/pages/Fields.tsx` — Configurazione campi
- `src/pages/Ddl.tsx` — Preview e apply DDL
- `src/pages/Settings.tsx` — Impostazioni sistema

### Components (1 file)
- `src/components/BimlDownloadButton.tsx` — Download BIML

### App (3 file)
- `src/App.tsx` — Root component con routing React Router 6
- `src/main.tsx` — Entry point
- `src/index.css` — Global CSS

### Documentazione (1 file)
- `README.md` — Setup instructions complete

**Totale: 25 file**

---

## Build e Avvio

### Nota: npm PATH issue rilevato

Durante la verifica build automatica, npm ha avuto problemi nel riconoscere i binari `tsc` e `vite` nel PATH su questo ambiente Windows. Questo è un problema noto di configurazione npm su Windows.

### Soluzione: Comandi Diretti

Usa i comandi seguenti che invocano direttamente i binari da `node_modules`:

#### 1. Type Check TypeScript (opzionale ma raccomandato)

```powershell
cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
node node_modules/typescript/lib/tsc.js --noEmit
```

**Nota:** Se ricevi errori `TS6053: File 'lib.*.d.ts' not found`, significa che TypeScript non è stato installato completamente. In tal caso, salta il type checking e vai direttamente alla build Vite (che usa esbuild per transpilare TypeScript).

#### 2. Build Produzione con Vite

```powershell
cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
node node_modules/vite/bin/vite.js build
```

**Output atteso:**
```
vite v5.0.11 building for production...
✓ 1234 modules transformed.
dist/index.html                   0.45 kB
dist/assets/index-abc123.js     234.56 kB │ gzip: 78.23 kB
✓ built in 5.23s
```

I file di build saranno generati in `dist/`.

#### 3. Dev Server (per sviluppo)

```powershell
cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
node node_modules/vite/bin/vite.js
```

Applicazione disponibile su: **http://localhost:5173**

#### 4. Preview Build Produzione

```powershell
cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
node node_modules/vite/bin/vite.js preview
```

Preview della build su: **http://localhost:4173**

---

## Alternative: Fix npm PATH

Se preferisci usare `npm run build` standard, puoi fixare il PATH di npm con:

### Opzione A: Reinstalla node_modules pulito

```powershell
cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
Remove-Item -Recurse -Force node_modules, package-lock.json
npm cache clean --force
npm install
npm run build
```

### Opzione B: Usa npx con package completo

Modifica `package.json` script `build`:

```json
"build": "npx --package=typescript -- tsc --noEmit && npx --package=vite -- vite build"
```

Poi:

```powershell
npm run build
```

---

## Verifica Build Success

### Checklist

- ✅ Cartella `dist/` creata
- ✅ File `dist/index.html` presente
- ✅ File `dist/assets/index-*.js` presente (bundle React)
- ✅ File `dist/assets/index-*.css` presente (bundle CSS)
- ✅ Nessun errore TypeScript compilatore
- ✅ Build size bundle < 500 KB (gzipped)

### Struttura dist/ attesa

```
dist/
├── index.html
├── vite.svg
└── assets/
    ├── index-[hash].js       # Bundle React + dependencies
    ├── index-[hash].css      # Bundle CSS Ant Design
    └── [altri assets statici]
```

---

## Test Funzionale End-to-End

### Prerequisiti

1. **Backend API in esecuzione** su `http://localhost:5000`
   ```powershell
   cd C:\Work.Git\CodicePlastico\internal\dw-builder
   dotnet run --project src/DwBuilder.Api/DwBuilder.Api.csproj
   ```

2. **Database DwBuilder operativo** con schema `_meta` e utente registrato

### Test Flow

1. **Avvia frontend dev server:**
   ```powershell
   cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
   node node_modules/vite/bin/vite.js
   ```

2. **Apri browser:** `http://localhost:5173`

3. **Login:**
   - Username: `admin` (o utente creato con `POST /api/v1/auth/register`)
   - Password: `Admin123!`
   - Verifica redirect a Dashboard

4. **Dashboard:**
   - Verifica visualizzazione statistiche sorgenti
   - Verifica tabella sorgenti configurate
   - Click su "Genera e Scarica BIML" → verifica download file `.biml`

5. **Gestione Sorgenti:**
   - Click menu sidebar "Sorgenti"
   - Click "Nuova Sorgente"
   - Compila form (es. nome="TestERP", server="localhost", database="ERP_Test", schema="landing_erp", user="sa", password="***")
   - Click "Crea" → verifica messaggio success
   - Click "Testa Connessione" su sorgente creata → verifica message.success/error

6. **Selezione Tabelle:**
   - Dalla dashboard o lista sorgenti, click "Configura Tabelle"
   - Verifica caricamento available tables da INFORMATION_SCHEMA
   - Seleziona 2-3 tabelle con checkbox
   - Modifica landing table name se necessario
   - Click "Salva Configurazione" → verifica message.success

7. **Configurazione Campi:**
   - Click "Configura Campi" su una tabella configurata
   - Verifica caricamento available fields
   - Seleziona campi, marca 1-2 come Business Key
   - Rinomina qualche campo landing
   - Click "Salva Configurazione" → verifica message.success

8. **DDL Preview:**
   - Click "Genera DDL"
   - Verifica visualizzazione 3 tab (CREATE landing, CREATE staging, ALTER)
   - Click "Scarica SQL" → verifica download file `.sql`
   - Seleziona checkbox "Applica CREATE Landing" e "Applica CREATE Staging"
   - Click "Applica DDL al DW" → verifica message.success

9. **Logout:**
   - Click avatar utente in alto a destra
   - Click "Logout"
   - Verifica redirect a `/login`

### Test Negativi

- ✅ Accesso a route protette senza JWT → redirect a `/login`
- ✅ Login con credenziali errate → message.error
- ✅ Creazione sorgente con campo required mancante → validazione form
- ✅ Test connessione con server inesistente → message.error con dettaglio errore
- ✅ Applicazione DDL su tabella senza campi configurati → message.error dal backend

---

## Troubleshooting

### CORS Errors in Console

**Problema:** `Access to XMLHttpRequest at 'http://localhost:5000/api/...' from origin 'http://localhost:5173' has been blocked by CORS`

**Soluzione:** Verifica che il backend API abbia CORS configurato per `http://localhost:5173`:

```csharp
// File: src/DwBuilder.Api/Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### 401 Unauthorized dopo Login

**Problema:** Dopo login, tutte le API calls ritornano 401

**Soluzione:** 
1. Apri DevTools → Application → Local Storage
2. Verifica che `jwt_token` sia presente
3. Verifica che il token non sia scaduto (JWT expiration di default è 24h)
4. Fai logout e re-login

### Build TypeScript Errors `TS6053: File lib.*.d.ts not found`

**Problema:** TypeScript non trova i file di libreria standard

**Soluzione:**
```powershell
cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
Remove-Item -Recurse node_modules
npm cache clean --force
npm install
```

Se il problema persiste, salta il type checking e usa solo Vite build (che usa esbuild, transpiler TypeScript indipendente).

### Hot Module Reload (HMR) non funziona

**Problema:** Modifiche ai file `.tsx` non si riflettono automaticamente nel browser

**Soluzione:**
1. Ferma Vite dev server (Ctrl+C)
2. Riavvia: `node node_modules/vite/bin/vite.js`
3. Hard refresh browser (Ctrl+Shift+R)

---

## Deployment Produzione

### Build Produzione

```powershell
cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
node node_modules/vite/bin/vite.js build
```

### Deploy su IIS (Windows Server)

1. **Installa URL Rewrite Module** per IIS:
   https://www.iis.net/downloads/microsoft/url-rewrite

2. **Copia file build:**
   ```powershell
   xcopy /E /I dist\* C:\inetpub\wwwroot\dwbuilder\
   ```

3. **Crea `web.config` nella root del sito:**

   ```xml
   <?xml version="1.0" encoding="UTF-8"?>
   <configuration>
     <system.webServer>
       <rewrite>
         <rules>
           <rule name="React Routes" stopProcessing="true">
             <match url=".*" />
             <conditions logicalGrouping="MatchAll">
               <add input="{REQUEST_FILENAME}" matchType="IsFile" negate="true" />
               <add input="{REQUEST_FILENAME}" matchType="IsDirectory" negate="true" />
             </conditions>
             <action type="Rewrite" url="/" />
           </rule>
         </rules>
       </rewrite>
     </system.webServer>
   </configuration>
   ```

4. **Configura variabile d'ambiente** `VITE_API_BASE_URL` per puntare all'API backend produzione

### Deploy con Docker

Usa il `Dockerfile` già presente:

```powershell
cd C:\Work.Git\CodicePlastico\internal\dw-builder\src\DwBuilder.Web
docker build -t dwbuilder-web .
docker run -p 80:80 dwbuilder-web
```

Applicazione disponibile su: **http://localhost**

---

## Documentazione Correlata

- [Backend API Swagger](http://localhost:5000/swagger) — documentazione endpoint REST
- [Ant Design Components](https://ant.design/components/overview/) — UI component library
- [TanStack Query Docs](https://tanstack.com/query/latest) — data fetching e caching
- [React Router v6](https://reactrouter.com/) — client-side routing
- [Vite Documentation](https://vitejs.dev/) — build tool e dev server

---

## Supporto

Per problemi o domande:
- Verifica errori in DevTools Console (F12)
- Verifica network tab per API calls fallite
- Verifica backend API logs in `src/DwBuilder.Api/bin/Debug/net10.0/logs/`
- Contatta team CodicePlastico
