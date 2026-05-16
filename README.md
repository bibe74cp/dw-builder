# dw-builder
Data warehouse builder

---

## Agent Activity Log

### AgentForge — 2026-05-16
- **Area:** Agent builder — meta-agente per la creazione di prompt
- **Azione:** Prima attivazione. AgentForge è operativo nel workspace. Gestisce la costruzione guidata (7 fasi) di agenti AI specializzati nel perimetro del progetto DW-Builder.

### ProjectManager — 2026-05-16
- **Area:** Pianificazione e gestione del prodotto
- **Azione:** Agente creato. Gestisce il ciclo di vita delle issue GitHub per DW-Builder: creazione, scomposizione in sub-issue, assegnazione a utenti e agenti, tracciamento e chiusura. Supporta orchestrazione multi-agente con payload strutturati JSON/Markdown.

### db-developer — 2026-05-16
- **Area:** Database design & architecture, data modeling
- **Azione:** Agente creato. Copre l'intero ciclo di sviluppo del database SQL Server per DW-Builder: DDL, schema evolution, indexing, stored procedure, view, MERGE, migration script, query tuning, schema `_meta`, landing zone, staging, integrazione SSIS/BIML, SQL Server Agent, sicurezza e linked server. Gestito tramite SSDT `.sqlproj`.

### ProjectManager — 2026-05-16
- **Area:** Pianificazione e gestione del prodotto
- **Azione:** Creazione struttura completa del backlog GitHub: 8 issue parent (FASE 1-8) e 55 sub-issue dettagliate. Label create: db, backend, frontend, ssis-biml, infra, test. Ogni issue parent include task list con collegamenti alle sub-issue. Backlog pronto per orchestrazione multi-agente.

### web-developer — 2026-05-16
- **Area:** Full-stack — DwBuilder.Api / DwBuilder.Core / DwBuilder.Infrastructure / DwBuilder.Biml / DwBuilder.Web
- **Azione:** Agente creato. Copre l'intero stack applicativo di DW-Builder: API REST ASP.NET Core 10, componenti React 18/TypeScript/Ant Design, integrazione BIML, autenticazione JWT, test xUnit. Applica OWASP Top 10 in autonomia. Delega DDL e operazioni DB a `db-developer`, backlog a `ProjectManager`. Documenta le scelte architetturali in `Documentation-web.md`.
