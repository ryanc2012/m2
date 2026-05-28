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

### 2026-05-27 — VariablePosition Trimmed + Code-Change Constraint Surfaced

- Removed `branch_manager_of_requester` and `department_head_of_requester` from ADR-004 built-in variable list
- Only `superior_of_requester` remains as the single built-in `PositionVariable`
- Removed the "extensible via configuration" claim (was incorrect — position variables require code changes)
- Added explicit ⚠️ blockquote note: `PositionVariable` values are code-defined constants, each mapping to an `IPositionResolver` implementation; new variables require a code change
- Applied in two places: the ADR-004 variable table (Section ~5) and the Section 8.2 `IPositionResolver` prose


### 2026-05-27 — Operation Behavior Pipeline: Marker Interface Decisions Confirmed

- **Task:** Updated ARCHITECTURE.md Section 8.4 to reflect confirmed ADR-004 implementation decisions
- **Changes applied:**
  1. Replaced `[RequiresApproval]`/`[SendNotification]` attribute examples with C# marker interface examples (`IRequiresAuthorization`, `IRequiresApproval`, `INotifiable`)
  2. Added enforced vs. optional behaviors table — `IRequiresAuthorization` is the only unconditionally enforced behavior; others skip via `is not` if marker absent
  3. Updated `ApprovalBehavior` code snippet — behavior calls service unconditionally when marker present; service owns on/off via `IFeatureFlagService`
  4. Updated "Enable / Disable at Runtime" table — all three columns now reflect that the decision point is inside the cross-cutting service, not the pipeline behavior
  5. Demoted Option B (config-driven profiles) as "not chosen" note
  6. Added ⚠️ Pipeline contract callout: behaviors = enforcement mechanism; services = policy authority
  7. Updated Code-Change Constraint to reference `IPipelineBehavior<TRequest, TResponse>` and marker interface contract (not attributes)
- **Decision inbox:** `.squad/decisions/inbox/keyser-mediatr-marker-interfaces.md` created


- **Patch 1 — Local Development Authentication** (new subsection in Section 6):
  - Added `### Local Development Authentication` under Section 6
  - Documents two auth paths: `[Dev]` via local identity stub + `DevelopmentAuthenticationHandler`; `[Prod]` via APIM + `JwtBearerHandler`
  - Services validate claim shape only — not token origin; environment-agnostic
  - APIM URLs injected via `appsettings.{Environment}.json` — never hardcoded
  - Docker Compose `override.yml` pattern for Keycloak local stub
  - Zero-code-change deploy principle documented with ✅ callout
### 2026-05-27 — Three-Flow Pipeline Architecture: GET single, GET list, CUD

- **Task:** Full pipeline architecture analysis for three distinct operation flows using the MediatR Marker Interface pattern.

**Key decisions reached:**

1. **ApprovalBehavior repositioned as POST-handler for CUD** — current ARCHITECTURE.md shows it as pre-handler gate; corrected to call `await next()` first so handler can save entity as `status=Pending` before approval is evaluated. This is a material amendment to Section 8.4.

2. **Fourth behavior added: `QueryAuthorizationBehavior`** — handles GET list post-fetch auth filtering. Registered 3rd in DI (between ApprovalBehavior and NotificationBehavior). New marker interface: `IFilterableByAuthorization`. This is a code-change per the pipeline code-change constraint.

3. **Two-command split for CUD** — initial CUD command saves as Pending; a separate `CommitCommand` (dispatched when approved OR when approval not required) handles SAP upload / DB status update + fires notification. Notification belongs to CommitCommand, NOT to the initial CUD command.

4. **IRequiresAuthorization is NOT used for GET list** — GET list uses `IFilterableByAuthorization` on the response type (post-handler filter), not a pre-handler gate. GET single uses `IRequiresAuthorization` (pre-handler gate). The two patterns are distinct and non-interchangeable.

5. **Commit trigger mechanism** — when approval not required: `ApprovalBehavior` calls `_approvalService.TriggerCommitAsync()` which enqueues `CommitCommand` via Hangfire (durability-consistent with outbox pattern). When approved: Platform.Api approval webhook calls Business.Api commit endpoint → dispatches `CommitCommand`.

6. **DB config table `feature_flags`** — keyed by `(flag_key, tenant_id)`. Read via `IFeatureFlagService` cached at 5-min TTL (consistent with auth cache). Checked inside `IApprovalService` and `INotificationService`, never inside behaviors.

7. **Registration order** (1st=outermost): `AuthorizationBehavior` → `ApprovalBehavior` → `QueryAuthorizationBehavior` → `NotificationBehavior`. Each conditional behavior checks its marker via `is not` and calls `next()` to skip.

- **Patch 2 — Operation Behavior Pipeline** (new subsection 8.4 in Section 8):
  - Documents the problem: per-operation composition of AuthorizationBehavior, ApprovalBehavior, NotificationBehavior was undocumented
  - Pattern: MediatR `IPipelineBehavior` pipeline — ordered, optional, conditional per operation
  - Operation metadata declaration via attributes (`[RequiresApproval]`, `[SendNotification]`) or JSON config override
  - Behavior registration snippet; per-behavior, per-operation, per-tenant feature-flag toggling
  - Integration table: each behavior maps to a specific Platform.Api REST endpoint
  - ⚠️ Code-change constraint: new behavior types require a code change; toggling existing behaviors does not

### 2026-05-27 — Business-Domain Config Table Decision

- **Rejected:** Generic `feature_flags (flag_key TEXT, tenant_id, is_enabled)` with dot-notation keys (`approval.Order.Create`)
- **Adopted:** Structured `entity_activity_config (entity_type TEXT, activity TEXT, tenant_id UUID, approval_enabled BOOL, notification_enabled BOOL)` table
- **Activity enum:** Full words `Create / Update / Delete` — not CUD shorthand — DB admins configure this directly; readability beats brevity
- **Primary key:** `(tenant_id, entity_type, activity)` — tenant first for partition locality
- **IRequiresApproval marker interface:** Upgraded to strongly-typed `EntityType` (string) + `Activity` (enum) properties replacing the freeform `ApprovalPolicy` string — enables compile-time traceability
- **IFeatureFlagService:** Two methods `IsApprovalEnabled(entityType, activity, tenantId)` and `IsNotificationEnabled(entityType, activity, tenantId)` — both async, cached at 5-min TTL
- **What you lose vs dot-notation:** Cannot add arbitrary new flag dimensions (e.g., `approval.Order.Create.highValue`) without a schema migration
- **What you gain vs dot-notation:** Table is self-documenting; DB admin can read and configure without knowing the key contract; SQL queries are trivial joins; no key typo bugs at runtime

### 2026-05-28 — Section 8.4 Amended: All Eight Pipeline Corrections Applied

- **ApprovalBehavior repositioned as POST-handler:** Corrected from pre-gate to post-handler wrapper in diagram, prose, and code sketch. Handler always saves entity as `Pending` before `ApprovalBehavior` evaluates.
- **QueryAuthorizationBehavior added (4th behavior):** Registered 3rd in DI (between Approval and Notification). Fires only when response implements `IFilterableByAuthorization`. In-process — no HTTP hop for GET list filtering.
- **IFilterableByAuthorization:** New marker interface on **response types** (not request). GET list responses implement it; the behavior checks post-fetch.
- **IRequiresApproval strongly typed:** Replaced `ApprovalPolicy` string with `EntityType` (string) + `Activity` (EntityActivity enum). Compile-time traceability, no key typos.
- **EntityActivity enum:** `Create / Update / Delete` full words — DB admins configure this directly.
- **entity_activity_config replaces feature_flags:** Structured table with `(tenant_id, entity_type, activity)` PK, `approval_enabled` + `notification_enabled` boolean columns. All `feature_flags` references removed from Section 8.4.
- **IFeatureFlagService updated:** Two methods `IsApprovalEnabledAsync` / `IsNotificationEnabledAsync` both taking `(entityType, activity, tenantId)` — consistent with structured table.
- **ApprovalBehavior flag check is in-behavior (not delegated):** `IApprovalService.EvaluateAsync` removed from the design; `ApprovalBehavior` calls `IFeatureFlagService` directly, consistent with pipeline contract (service = policy authority, but for the flag lookup the behavior is authorised to call the flag service directly).
- **Two-Command CUD Split documented:** `Create{Entity}Command` (saves Pending, triggers approval) and `Commit{Entity}Command` (Pending→Active, SAP outbox, notification). `INotifiable` on Commit only — not on Create.
- **Three flow patterns table added:** GET single, GET list, CUD Phase 1, CUD Phase 2 — each row shows markers, pre-handler, post-handler.
- **Behavior registration code updated:** All four behaviors with inline comments.
- **Trade-off confirmed:** Hangfire for commit dispatch (not inline MediatR Send) — avoids nested pipeline coupling, provides durability parity with SAP outbox.

### 2026-05-28 — Section 8.4 & ADR-004 Targeted Additions (Q&A Session)

- **Pending-First, Always principle added (Section 8.4):** Explicit boxed design principle added before Phase 1 description — every CUD saves as `Pending` in Phase 1 with no bypass path; the approval/no-approval split happens only in what `ApprovalBehavior` does post-handler.
- **Handler Dispatch Mechanics note added (Section 8.4):** Clarifies that MediatR calls each handler exactly once per `Send()`; the two-phase commit uses two separate dispatches — the handler cannot be re-invoked from within a behavior.
- **IPositionResolver built-in variables clarified (ADR-004):** Replaced vague "see IPositionResolver" with explicit statement that adding a new variable requires a code change (new resolver implementation + registration) — no configuration-only path.
- **Document header date updated:** `2026-05-12` → `2026-05-28`.

### 2026-05-28 — Mandatory Behavior Pipeline: Marker Interfaces Retired, IOperationCommand Adopted

- **Correction by Ryan:** Marker interface–based skipping (`is not IRequiresApproval → return next()`) is the wrong pattern. The correct pattern: behaviors ALWAYS execute within their pipeline type; `IOperationBehaviorConfig` (not marker presence) decides no-op.
- **IOperationCommand:** Single required base interface on ALL commands — `AppId`, `ObjectType`, `Activity`. No optional markers needed as gates. Four pipeline sub-interfaces (`IGetSingleCommand`, `IGetListCommand`, `ICudPhase1Command`, `ICudPhase2Command`) carry MediatR generic type constraints — behaviors activate only for their pipeline type at resolution time.
- **IFeatureFlagService retired:** Replaced by `IOperationBehaviorConfig` backed by `operation_behavior_config` table. Key insight: `app_id` leads the PK — same `object_type + activity` can behave differently per application (MekaPOS vs. MekaPromos vs. M2Portal).
- **entity_activity_config retired:** Replaced by `operation_behavior_config (app_id, object_type, activity)`. Tenant-keyed approach dropped; app-keyed approach adopted. Fail-secure default: no row → all behaviors enabled.
- **Behavior registration:** Generic type constraints on behavior classes replace runtime `is not` checks. MediatR resolves behaviors per-command-type — clean, no runtime branching inside behaviors.
- **Trade-off named:** Losing per-tenant granularity (tenant was the old PK leader). Gaining per-app granularity — more aligned with the actual multi-app deployment model (three BFFs, three app identities).
- **Applied in:** `docs/architecture/ARCHITECTURE.md` Section 8.4 rewritten. Decision inbox: `.squad/decisions/inbox/keyser-mandatory-behaviors.md`.
