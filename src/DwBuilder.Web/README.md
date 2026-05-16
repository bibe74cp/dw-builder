# DW-Builder Frontend - React Web Application

Applicazione web frontend per la configurazione e gestione del Data Warehouse Builder.

## Stack Tecnologico

- **Framework**: React 18
- **Language**: TypeScript
- **Build Tool**: Vite
- **UI Library**: Ant Design 5
- **Routing**: React Router 6
- **State Management**: TanStack Query (React Query)
- **HTTP Client**: Axios
- **Authentication**: JWT (Bearer Token)

## Struttura del Progetto

```
src/
├── api/              # API client e services
│   ├── axios.ts      # Axios instance con interceptors JWT
│   └── services.ts   # API service functions (auth, sources, tables, fields, ddl, biml)
├── components/       # Componenti riutilizzabili
│   └── BimlDownloadButton.tsx
├── layouts/          # Layout applicazione
│   └── MainLayout.tsx
├── pages/            # Pagine principali
│   ├── Login.tsx
│   ├── Dashboard.tsx
│   ├── Sources.tsx
│   ├── Tables.tsx
│   ├── Fields.tsx
│   ├── Ddl.tsx
│   └── Settings.tsx
├── types/            # TypeScript type definitions
│   └── api.ts
├── App.tsx           # Root component con routing
├── main.tsx          # Entry point
└── index.css         # Global styles
```

## Funzionalità Implementate

### Autenticazione
- ✅ Login con JWT authentication
- ✅ Protected routes (redirect a /login se non autenticato)
- ✅ Logout con pulizia token
- ✅ Intercettore Axios per gestione automatica 401 Unauthorized

### Dashboard
- ✅ Statistiche sorgenti (totali, attive, disattivate)
- ✅ Tabella riepilogativa sorgenti configurate
- ✅ Navigazione rapida a configurazione tabelle
- ✅ Pulsante download BIML master template

### Gestione Sorgenti
- ✅ CRUD completo sorgenti (Create, Read, Update, Delete)
- ✅ Form modale con validazione
- ✅ Test connessione sorgente (POST /api/v1/sources/{id}/test-connection)
- ✅ Stato attivo/disattivato tramite switch

### Selezione Tabelle
- ✅ Lista tabelle disponibili dalla sorgente (GET /api/v1/sources/{id}/available-tables)
- ✅ Selezione multipla tramite checkbox
- ✅ Rename inline nome tabella landing
- ✅ Attivazione/disattivazione tabelle
- ✅ Bulk upsert configurazione (PUT /api/v1/source-tables/sources/{sourceId}/tables)

### Configurazione Campi
- ✅ Lista campi disponibili per tabella (GET /api/v1/source-tables/{tableId}/available-fields)
- ✅ Selezione campi da sincronizzare
- ✅ Rename colonne landing
- ✅ Marcatura Business Key tramite switch
- ✅ Display tipo dati SQL
- ✅ Ordinamento per ordinal position
- ✅ Bulk upsert campi (PUT /api/v1/source-tables/{tableId}/fields)

### Generazione e Applicazione DDL
- ✅ Preview DDL in tabs (CREATE Landing, CREATE Staging, ALTER Landing)
- ✅ Download script SQL
- ✅ Applicazione selettiva DDL al Data Warehouse
- ✅ Checkbox per scegliere quali script applicare

### Generazione BIML
- ✅ Pulsante download BIML master template (GET /api/v1/biml)
- ✅ Download automatico file .biml

### Impostazioni
- ✅ Form configurazione DW connection string
- ✅ Gestione encryption key
- ✅ Configurazione JWT secret
- ✅ CORS origins
- ✅ Livello logging (placeholder per implementazione futura)

## Setup e Installazione

### Prerequisiti
- Node.js 18+ e npm
- Backend API DW-Builder in esecuzione su `http://localhost:5000`

### Installazione dipendenze

```bash
cd src/DwBuilder.Web
npm install
```

### Variabili d'ambiente

File `.env.development`:
```
VITE_API_BASE_URL=http://localhost:5000/api/v1
```

### Avvio Development Server

```bash
npm run dev
```

Applicazione disponibile su: **http://localhost:5173**

### Build Production

```bash
npm run build
```

Gli artefatti di build saranno generati in `dist/`.

### Preview Build Locale

```bash
npm run preview
```

## Integrazione API Backend

Tutti gli endpoint API sono documentati in Swagger all'indirizzo `http://localhost:5000/swagger` quando il backend è in esecuzione.

### Autenticazione JWT

1. Login tramite `POST /api/v1/auth/login` ritorna un token JWT
2. Il token viene salvato in `localStorage` con chiave `jwt_token`
3. Axios interceptor aggiunge automaticamente header `Authorization: Bearer {token}` a ogni richiesta
4. Su 401 Unauthorized, l'interceptor pulisce il token e redirige a `/login`

### Gestione Errori

- Messaggi di errore visualizzati tramite `message.error()` di Ant Design
- Log errori in console per debugging
- Validazione form client-side prima dell'invio

## Best Practices Implementate

- ✅ **TypeScript strict mode** per type safety
- ✅ **Path aliases** (`@/*`) per import puliti
- ✅ **TanStack Query** per caching e sincronizzazione stato server
- ✅ **React Router 6** per routing dichiarativo
- ✅ **Ant Design** per UI consistency e accessibilità
- ✅ **Axios interceptors** per gestione centralizzata autenticazione ed errori
- ✅ **Code splitting** automatico tramite Vite
- ✅ **ESLint** per linting (configurazione standard Vite + React)

## Deployment

### Build Produzione

```bash
npm run build
```

### Deployment su IIS (Windows)

1. Installare [URL Rewrite Module](https://www.iis.net/downloads/microsoft/url-rewrite)
2. Copiare contenuto cartella `dist/` in `C:\inetpub\wwwroot\dwbuilder`
3. Creare file `web.config`:

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

4. Configurare variabile d'ambiente `VITE_API_BASE_URL` per puntare all'API backend in produzione

### Deployment con Nginx

1. Build produzione: `npm run build`
2. Copiare `dist/` su server
3. Configurare nginx (vedi `nginx.conf` già presente nella cartella DwBuilder.Web)

### Deployment Docker

Utilizzare il `Dockerfile` già presente nella cartella `DwBuilder.Web`.

```bash
docker build -t dwbuilder-web .
docker run -p 80:80 dwbuilder-web
```

## Testing Funzionale

### Scenario End-to-End

1. **Login**: accedi con credenziali valide (es. admin/Admin123!)
2. **Dashboard**: visualizza statistiche sorgenti
3. **Crea Sorgente**: vai a "Sorgenti" → "Nuova Sorgente", compila form, salva
4. **Test Connessione**: apri sorgente in modifica, clicca "Testa Connessione"
5. **Seleziona Tabelle**: dalla dashboard o lista sorgenti, clicca "Configura Tabelle" → seleziona tabelle, rinomina landing table, salva
6. **Configura Campi**: da una tabella configurata, clicca "Configura Campi" → seleziona campi, marca business key, rinomina, salva
7. **Genera DDL**: da campi configurati, clicca "Genera DDL" → visualizza preview, seleziona script, applica o scarica
8. **Download BIML**: dalla dashboard, clicca "Genera e Scarica BIML"
9. **Logout**: clicca avatar utente → Logout

### Checklist Test Manuali

- [ ] Login funzionante (credenziali corrette + errore su credenziali errate)
- [ ] Protected routes funzionanti (redirect a /login se non autenticato)
- [ ] Dashboard mostra statistiche corrette
- [ ] CRUD sorgenti completo
- [ ] Test connessione sorgente
- [ ] Selezione tabelle e bulk upsert
- [ ] Configurazione campi con business key
- [ ] Preview DDL con 3 tab
- [ ] Applicazione DDL selettiva
- [ ] Download script SQL
- [ ] Download BIML
- [ ] Logout pulisce token e redirige a login
- [ ] Navigazione breadcrumb/back button funzionante
- [ ] Responsive design (desktop + tablet)

## Troubleshooting

### CORS Errors

Se ricevi errori CORS, verifica che il backend API abbia configurato correttamente CORS per `http://localhost:5173` in `Program.cs`:

```csharp
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

### 401 Unauthorized dopo login

Verifica che il token JWT sia salvato correttamente in localStorage e che l'interceptor Axios stia aggiungendo l'header `Authorization`.

### Build TypeScript Errors

Esegui `npm run build` e verifica gli errori TypeScript. Assicurati che tutti i tipi in `src/types/api.ts` corrispondano ai DTOs backend.

## Documentazione Correlata

- [Swagger API Backend](http://localhost:5000/swagger)
- [Ant Design Components](https://ant.design/components/overview/)
- [TanStack Query Docs](https://tanstack.com/query/latest)
- [React Router Docs](https://reactrouter.com/)
- [Vite Docs](https://vitejs.dev/)

## Licenza

Internal CodicePlastico Project - All Rights Reserved
