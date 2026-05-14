# Keyser — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Lead
- **Joined:** 2026-05-11T02:37:43.910Z

## Learnings

### 2026-05-13 — GitHub Migration, Sprint 7 Deferral, Multi-tenancy Assessment

**Q1 (GitHub → Azure DevOps):**
- The entire Squad automation layer (squad-triage.yml, squad-heartbeat.yml, squad-issue-assign.yml) is GitHub-native and requires a full rebuild on Azure DevOps (Work Items API, Azure Pipelines triggers instead of Issues events). Estimated 2–3 days to port Ralph's triage logic.
- Copilot coding agent (`copilot-swe-agent[bot]`) is GitHub-only and will be lost on migration. The CLI-based Squad system is platform-independent and survives.
- S7.1 should target Azure Pipelines YAML from day one — writing GitHub Actions CI now wastes a rewrite.
- ACR is a better container registry target than GHCR for ACA deployment anyway (Managed Identity integration).
- GitHub MCP server and `gh` CLI are GitHub-specific and should not be dependencies for permanent tooling.

**Q3 (Sprint 7 deferral):**
- S7.1 (CI pipeline) is the highest-risk deferral in the entire plan — pull it into Sprint 5 by trading S5.8 (rate limiting, 2pts) to Sprint 6. Net Sprint 5: ~40pts.
- S7.2 (Docker Compose) belongs in Sprint 6 — reduces 4-process dev friction ahead of frontend sprint.
- S7.3–S7.10 all depend on cloud being provisioned. "Sprint 7" should be retimed as "Cloud Readiness Sprint" triggered by Azure subscription readiness, not calendar. Hard-deferring ACA bicep, Key Vault, k6, ZAP, OpenTelemetry until environment exists is rational.

**Q4 (Multi-tenancy):**
- Multi-tenancy is structurally present at schema level (TenantId on all 22+ tables via existing migrations) and application level (tenantId parameter on all service methods).
- NO automatic tenant resolution exists — no ITenantContext middleware, no JWT claim extraction, no EF global query filters on TenantId. It is "pass by hand" everywhere.
- Recommended: Add `WellKnownTenants.Default` constant to SharedKernel. Zero schema changes, zero migrations, zero service signature changes. ~0.5 day.
- TenantId columns are not a liability — they're always the same value, 16 bytes per row. Optionality preserved at near-zero cost.
- ShopId (multi-store) is the genuinely active discriminator for this deployment — multiple shops under one tenant is the actual target.

### 2026-05-13 — Backlog Refinement & Sprint 4–7 Planning

**Backlog state found:**
- All 8 domain module services are scaffolded and partially implemented; GoodsReceipt is the most complete (has SAP outbox, migration, Flutter screen).
- 4 EF migrations exist through Sprint 4 schema (GoodsReceipt + SapOutbox); Authorization module schema does not yet exist.
- Platform.Api 4-process topology is fully extracted and working (Sprint 3 complete).
- BFF endpoints exist for all major modules but auth enforcement is absent.
- **Critical gaps discovered:** No CI/CD pipeline (only squad-related workflows), no Docker Compose, Authorization module is unimplemented (IAuthorizationService has no backing service), M2PortalBff has no Entra ID JWT wiring, Hangfire not registered (SAP outbox worker never fires), no Blazor MSAL auth.
- Blazor portal pages are stubs (App.razor, 5 page skeletons); Flutter apps are minimal (meka-pos: GoodsReceipt only; meka-promos: Notifications only).

**Sprint themes chosen:**
- Sprint 4: Business Logic Completion — Sales, Approvals, Promotions engine, Hangfire, SignalR, FCM
- Sprint 5: Auth, Security & Infrastructure Cross-cuts — AuthZ module, JWT wiring, API keys, health checks, rate limiting, versioning
- Sprint 6: Frontend Depth — Blazor portal real pages, Flutter POS sales flow, Flutter Promos member/coupon flow
- Sprint 7: CI/CD, Observability & Production Readiness — GitHub Actions, Docker, OpenTelemetry, ACA bicep, Pact, k6, ZAP

**Key capacity decisions:**
- Authorization guard (Sprint 5) is a hard gate on Sprint 6 frontend work — Fenster cannot build admin pages against unguarded endpoints.
- CI/CD pipeline (S7.1) is high-risk being deferred to Sprint 7; consider pulling to Sprint 5 if McManus has slack after auth wiring.
- SAP Outbox (Hangfire, Sprint 4) is a critical fix — GoodsReceiptService explicitly defers to outbox that never processes.
- API versioning (`/api/v1/`) applied in Sprint 5 while no external consumers exist — zero migration cost.

### 2026-05-13 — Sprint 1–4 Outstanding Items Audit

**Verified actual code state vs sprint plan "Done" claims:**

- **Sprint plan overstated "Done":** The sprint plan claims all 8 service implementations are complete but all 4 inspected services (SalesService, ApprovalService, PromotionService/DiscountEngine, GoodsReceiptService) are explicit in-memory stubs with no EF persistence. Every file has a doc comment saying "EF wiring deferred to Sprint 4."
- **IApprovalService has no EscalateAsync** — `ApprovalStatus.Escalated` exists as a domain enum value but the interface and implementation have no escalation method. Sprint 4 story S4.2 lists it as a deliverable.
- **DiscountEngine always returns 0 discount** (`DiscountEngine.cs` line 33: "Stub: no formula evaluation — return 0 discount"). This is not a valid Sprint 3 deliverable — it was listed as "Done."
- **Hangfire: zero registration** — `NoOpOutboxService` is DI-registered; no Hangfire package, no `AddHangfire`, no `RecurringJob`. `GoodsReceiptService.PostToSapAsync` logs "enqueued via outbox" but writes nothing.
- **WellKnownTenants.Default not added** — grep confirms no match anywhere in `/src`. Assigned to McManus in S4.7 but the constant is a SharedKernel prerequisite that should precede seed data.
- **No idempotency key anywhere** — `SalesTransaction` entity and `SalesService.CreateTransactionAsync` have no idempotency key parameter or column. Sprint 4 S4.1 must add this from scratch.
- **No `[Authorize]` on any BFF endpoint** — M2PortalBff, MekaPosBff, MekaPromosBff all have `AddMicrosoftIdentityWebApi` wired and `UseAuthentication()`/`UseAuthorization()` in the pipeline, but zero endpoint-level enforcement. Every route is publicly accessible.
- **M2PortalBff Entra ID wiring is present** (contradicts sprint plan Discovered Gap) — `Program.cs` line 27 has `AddMicrosoftIdentityWebApi`. Gap was documented inaccurately; the real gap is endpoint-level `[Authorize]` enforcement.
- **Blazor pages are genuinely mixed:** Promotions pages (PromotionList, PromotionCreate, PromotionDetail, PromotionEdit) have real data binding and service calls. Attendance.razor, Sales.razor, Settings.razor are pure stubs (Construction icon + placeholder text). Approvals pages have real data-bound tables and action handlers.
- **Flutter apps more complete than sprint plan states:** meka-pos has sales/cart/payment/receipt/attendance/returns/login features (not just GoodsReceipt). meka-promos has coupons/promotions/registration/profile/login (not just Notifications). Both are functional shells calling BFF endpoints.
- **API versioning gap confirmed** — all Platform.Api module groups are at `/modules/{name}/` (no `/api/v1/` prefix). All BFF endpoints also lack `/api/v1/` prefix.
- **No Authorization module class exists anywhere** — no `AuthorizationService.cs`, no `IAuthorizationService` beyond any that might exist in the domain. Sprint 5 S5.1 starts from zero.

**Key architectural risks going into Sprint 4:**
- All business logic work in Sprint 4 (S4.1–S4.3) requires replacing in-memory stubs with real EF queries — this is not additive work, it's replacement. Estimate risk: scope creep on each story.
- SAP outbox worker (S4.4) requires both Hangfire registration AND an actual outbox table writer replacing `NoOpOutboxService`. Two tasks in one story.
- Sprint 4 seed data (S4.7) will use `Guid.Empty` for TenantId until `WellKnownTenants.Default` is added — this must be the first task of S4.7.

## Sprint Planning (2026-05-12)

- Produced initial 4-sprint plan sequencing backend platform, approval, notification, member, promotions, sales, attendance, goods receipt, and SAP integration.
- Critical path and parallelizable work identified; all open questions resolved.

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
