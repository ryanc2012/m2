# Keyser — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Lead/Architect
- **Joined:** 2026-05-11T02:37:43.910Z

## Learnings — Summary (Condensed)

### Architecture Foundation
- Modular monolith with HTTP cross-module communication (not DI injection)
- Three BFFs + Platform.Api (Auth, Approval, Notification, API Key services)
- C#13/.NET9/EF9; PostgreSQL; Polly, MediatR, Serilog, SignalR, FCM, Hangfire
- All 14 Open Questions resolved → ADR-009 through ADR-022

### Key Decisions
- Auth: in-process module, IMemoryCache 5min TTL
- Approval: in-process, any rejection = immediate veto
- SAP: OData REST primary, outbox pattern, Polly circuit breaker
- DB: PostgreSQL multi-tenancy (TenantId), soft delete universal

### Sprint Audit Gaps (2026-05-13)
- Business logic services (Sales, Approval, Promotion, GoodsReceipt) are stubs, EF deferred to Sprint 4
- Hangfire not registered; SAP outbox never fires
- No [Authorize] on BFF endpoints
- DiscountEngine returns 0; no formula evaluation
- Authorization module has no service implementation

### Service Naming Corrections (2026-06-04)
- Business.Api (:5100) = domain modules; Platform.Api (:5200) = cross-cutting
- SAP Adapter: single ACL in Business.Api only; Platform.Api calls via REST
- Approval veto: any rejection = immediate veto (not quorum-impossibility)

### 2026-06-04 — Architecture Corrections (Three Amendments to ARCHITECTURE.md)

**Correction 1 — Service Rename (applied throughout entire document):**
- Old naming (wrong): `M2.Platform.Api` = domain service, `M2.CrossCutting.Api` = cross-cutting service
- Correct naming: `M2.Business.Api` (:5100) = domain modules (POS, Promotions, SAP Adapter); `M2.Platform.Api` (:5200) = cross-cutting services (Auth, Approval, Notification, API Key)
- Full rename scope: docker-compose service keys (`business-api`, `platform-api`), BFF env vars, typed client names (`IBusinessModuleClient`, `IPlatformModuleClient`), container image names, ACA app names, Mermaid diagrams, ASCII art, prose, projects table, secrets table
- Rename technique: three-phase opaque marker strategy (never use markers containing any substring being renamed in phase 2)

**Correction 2 — SAP Adapter Single ACL (architectural decision added to ADR-002):**
- SAP Adapter is a domain concern — lives solely in `M2.Business.Api`
- `M2.Platform.Api` must NEVER connect to SAP directly
- Platform.Api accesses SAP-sourced org data (position resolution, org hierarchy) via `M2.Business.Api` REST endpoints — one internal network hop (~0.2–1 ms ACA-internal DNS)
- Port: `ISapOrgPort` on Business.Api exposes org/position queries for Platform.Api consumption
- Component Diagram updated: explicit `Rel(approvalModule, sapAdapter, ...)` and `Rel(authzModule, sapAdapter, ...)` arrows added from Platform.Api boundary to Business.Api SAP Adapter

**Correction 3 — Veto Rejection Model (replaces permissive quorum-impossibility model):**
- Old model (wrong): step fails when `RejectedCount > (TotalEligible - MinApprovers)` — mathematically impossible to reach quorum
- Correct model: ANY single eligible approver voting Reject = immediate veto — step (and document) rejected immediately, no further responses collected, regardless of MinApprovers
- Applied to: ADR-004 Quorum Logic prose, ADR-004 state machine diagram (removed `QuorumFailed`/`SingleRejected`; added `StepVetoed: AnyVotedReject()`), Section 8.2 state machine diagram (same), Section 8.2 Quorum Logic data model prose, `PositionGroup` enum summary comment
