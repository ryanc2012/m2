# Keyser — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Lead
- **Joined:** 2026-05-11T02:37:43.910Z

## Learnings

<!-- Append learnings below -->

### 2026-05-12 — Open Questions Resolved

All 14 Open Questions from the initial planning phase have been promoted to formal ADRs (ADR-009 through ADR-022) in `.squad/decisions.md`. The Open Questions table was removed and replaced with a resolution note. This closes the loop on all pending architectural and business decisions for MVP scope, ensuring clarity and traceability for future work.


### 2026-05-12 — Architecture & Standards Session

**Architecture Style Chosen: Modular Monolith**
- Rejected microservices (premature for team size/maturity) and pure shared-BFF (violates client independence)
- Three dedicated BFFs (one per client): MekaPosBff, MekaPromosBff, M2PortalBff — each an ASP.NET Core 9 project
- Platform Core is a modular monolith: Platform.Core, Platform.Infrastructure, Platform.Authorization, Platform.Approval, Platform.Notification as C# projects (not microservices)
- Cross-module communication via injected interfaces (never direct class instantiation across boundaries)
- EF Core per-module DbContext (or ModelBuilder partition) — no cross-module entity navigation
- Decomposition-ready: each module has an interface boundary; extraction to standalone service is an operational decision, not a redesign

**Technology Stack Summary:**
- Backend: C# 13 / .NET 9 LTS / ASP.NET Core 9 / EF Core 9
- Database: PostgreSQL 16 (SQL Server acceptable if org-mandated)
- ORM/Mapping: EF Core + Mapperly (source-generated)
- Validation: FluentValidation
- Mediator/CQRS: MediatR
- Resiliency: Polly 8
- Logging: Serilog → Azure Application Insights
- Observability: OpenTelemetry
- Background jobs: Hangfire (SAP outbox worker)
- Real-time (Blazor): ASP.NET Core SignalR
- Push notifications (mobile): Firebase FCM + APNs
- Auth: Microsoft.Identity.Web (Entra ID JWT)
- API Gateway: Azure API Management
- Deployment: Azure Container Apps (ACA) — recommended over AKS for team's current ops maturity

**Key Cross-Cutting Decisions:**
- Authorization: in-process module (SAP auth object model); cached in IMemoryCache; extracted to PDP only when complexity demands
- Approval: in-process module with explicit state machine; sequential position-based steps; MediatR events notify Notification module
- Notification: SignalR for Blazor web + Firebase FCM for Flutter mobile; direct FCM (not Azure Notification Hubs) for this scale
- SAP Integration: OData REST APIs primary; RFC/BAPI via NCo as fallback; Polly circuit breaker + outbox pattern for critical writes
- API keys: SHA-256 hashed, scoped, APIM-validated; raw key shown once on issuance, never stored
- Container strategy: Multi-stage Alpine builds; Azure Container Apps production; Docker Compose local dev
- Secret management: Azure Key Vault via Managed Identity in production; user-secrets + .env in development

**Coding Standards Decisions:**
- Flutter state management: Riverpod (compile-time safe, testable without BuildContext)
- Blazor code pattern: code-behind (.razor.cs partial class) — no inline @code blocks for logic
- Git workflow: GitHub Flow (short-lived branches); Conventional Commits; Squash merge
- DB PKs: Sequential GUIDs (NEWSEQUENTIALID / gen_random_uuid); PascalCase table/column names
- Soft delete mandatory on all domain entities (IsDeleted + DeletedAt + DeletedBy)

### 2026-05-12 — Cross-Agent Context (from Initial Planning Session)

**From Edie (Database):**
- PostgreSQL 16 confirmed as primary database, aligning with ADR-003. TenantId multi-tenancy approach adopted. Soft delete and audit columns are universal schema conventions—these are database-enforced, not just application-layer.

**From McManus (Backend Dev):**
- 10 open questions filed. Notably, ADR-006 (OData REST primary for SAP) resolves McManus's OQ-03 (SAP connectivity). ADR-004 (in-process auth module, backend-designed auth objects) effectively answers OQ (auth object ownership: backend team designs from scratch).
- ECR vendor (OQ-01) and SMS gateway (OQ-02) remain unresolved stakeholder decisions — flagged as sprint-planning blockers.

**From Fenster (Frontend Dev):**
- D7 (SignalR for M2 Portal notifications) is resolved by ADR-005 — SignalR is confirmed for Blazor. Fenster can proceed with `NotificationBell` and `NotificationDropdownPanel` implementation against the SignalR hub.
- Remaining open questions (OQ-10 through OQ-14) require Lead/Auth input before corresponding epics can start.

**From Verbal (Tester):**
- Test strategy confirmed modular monolith enables in-process integration testing (no service mesh overhead at this stage). SAP module mocked at OData interface boundary.
