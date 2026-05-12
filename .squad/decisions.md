# Squad Decisions

## Active Decisions

### ADR-001: Architecture Style — Modular Monolith
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

Adopt a **Modular Monolith** as primary architecture. Single deployable unit per BFF concern; bounded contexts as separate C# projects communicating only via injected interfaces. Decomposition-ready: extracting a module to a standalone service requires no interface redesign—only an operational deployment decision.

**Rejected:** Microservices (premature for team size/maturity), simultaneous Hybrid model (introduces two operational models before business value delivered).

---

### ADR-002: BFF Pattern — One BFF Per Client
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

One dedicated BFF per client: `MekaPosBff` (Flutter POS staff app), `MekaPromosBff` (Flutter consumer promotions app), `M2PortalBff` (Blazor manager/admin portal). Shared infrastructure (auth middleware, health checks, logging) in `Platform.SharedBff`, referenced by all three.

---

### ADR-003: Technology Stack
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

| Layer | Decision |
|-------|----------|
| Language / Runtime | C# 13 / .NET 9 LTS |
| Web Framework | ASP.NET Core 9 (Minimal APIs) |
| ORM | Entity Framework Core 9 |
| Validation | FluentValidation 11 |
| Mapping | Mapperly (source-generated, zero-reflection) |
| Mediator / CQRS | MediatR 12 |
| Resiliency | Polly 8 (retry, circuit breaker, timeout) |
| Logging | Serilog 4 → Azure Application Insights |
| Observability | OpenTelemetry .NET 1.x |
| Background Jobs | Hangfire 1.8 (SAP outbox, scheduled jobs) |
| Auth | Microsoft.Identity.Web 3.x (Entra ID) |
| Real-time | ASP.NET Core SignalR 9 (Blazor portal) |
| Push notifications | Firebase Admin SDK (FCM + APNs) |
| Database | PostgreSQL 16 on Azure Database for PostgreSQL Flexible Server *(SQL Server acceptable if org-mandated)* |
| API Gateway | Azure API Management (Consumption dev / Standard v2 prod) |
| Deployment | Azure Container Apps (ACA) with KEDA autoscaling |
| Flutter state management | Riverpod (with code generation) |
| Blazor code pattern | Code-behind partial classes (.razor.cs) |

---

### ADR-004: Authorization — In-Process Module with SAP Auth Object Model
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

Authorization is an in-process module (`Platform.Authorization`) using the SAP authorization object model (authorization objects with field-level values). In-process cache with 5-minute TTL; exposes `IAuthorizationService` to all modules and BFFs. Evolution trigger: complex ABAC requirements or auth data size exceeding memory comfort threshold.

---

### ADR-005: Cross-Cutting Service Delivery Modes
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

| Service | Mode | Rationale |
|---------|------|-----------|
| Authorization | In-process module | Hot path; cache-able; no distributed overhead justified |
| Approval | In-process module | Low-frequency; full transactional consistency needed with domain writes |
| Notification | In-process → SignalR + FCM | SignalR hub co-located with BFF; FCM via Admin SDK |
| SAP Adapter | In-process anti-corruption layer | Isolation via interface; Polly retry/circuit-breaker in-process |

---

### ADR-006: SAP Integration — OData REST Primary, NCo RFC/BAPI Fallback
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

SAP OData REST APIs are the primary integration channel; SAP NCo RFC/BAPI as fallback for functions not exposed via OData. Critical writes (goods receipt, sales sync) use the Outbox Pattern (Hangfire). Polly: 3-attempt exponential backoff; circuit breaker (5 failures / 30s window, 60s half-open); outbox worker retries every 30 seconds.

---

### ADR-007: Flutter State Management — Riverpod
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

All Flutter apps use Riverpod (with code generation) as the sole state management approach. `Provider` and `GetX` are forbidden. `Bloc` may be introduced for a specific complex flow only with Lead approval and an ADR update.

---

### ADR-008: Blazor Code Pattern — Code-Behind (.razor.cs)
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

All Blazor components with business logic use code-behind partial classes (`.razor.cs`). Inline `@code {}` blocks limited to trivial property declarations (≤ 3 lines).

---

### DB-001: Database — PostgreSQL + TenantId Multi-Tenancy
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Edie

PostgreSQL 16 selected. Shared database with `TenantId` column on all tables (single shared DB multi-tenancy). All tables include audit columns, soft delete (`IsDeleted` + `DeletedAt` + `DeletedBy`), and strategic indexes. EF Core migrations with timestamped naming convention.

---

### TEST-001: Test Strategy — Pyramid and Toolchain
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Verbal

Test pyramid: 70% unit / 20% integration / 10% e2e. Standard tools: xUnit, flutter_test, bUnit, Playwright, Pact, k6, Restler, OWASP ZAP. SAP and auth mocked in lower environments; contract and security tests enforced in CI pipeline. Test data must be synthetic or anonymized. All test environments must support data reset and isolation.

---

### BE-REC-001: Backend Recommendations (Accepted Pending Objection)
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** McManus

| # | Decision |
|---|----------|
| R1 | Idempotency keys on all mutating Sales API endpoints |
| R2 | SAP sync is pull-based (platform polls SAP — avoids inbound firewall requirements) |
| R3 | Approval engine is document-agnostic (`document_type` + `document_id` contract) |
| R4 | API keys use SHA-256 hash storage; plaintext never persisted; shown once on creation |
| R5 | Coupon QR codes use short-lived signed JWTs (5-minute TTL) to prevent screenshot replay |

---

## Open Questions (Pending Decision)
> All questions resolved as of 2026-05-12. See ADR-009 onwards.


> These items require team or stakeholder input before dependent epics can enter sprint planning.

| ID | Question | Blocks | Raised By |
|----|----------|--------|-----------|
| OQ-01 | ECR vendor and integration protocol (REST / proprietary SDK / out of scope for MVP)? | Epic 6, Epic 2.3 | McManus, Fenster |
| OQ-02 | SMS gateway provider for OTP delivery (Twilio / AWS SNS / local telco)? | Epic 4 | McManus |
| OQ-03 | SAP auth object schema ownership: business-provided specs or backend-designed? | Epic 1 | McManus |
| OQ-04 | Multi-store vs single-store MVP? (`location_id` first-class or deferred) | Epics 4, 6, 7, 8 | McManus |
| OQ-05 | Coupon issuance: pre-issued on activation or on-demand at first browse? | Epics 4, 5 | McManus |
| OQ-06 | Approval escalation target: SAP org hierarchy parent or configurable per workflow step? | Epic 2 | McManus |
| OQ-07 | Offline POS support required (local queue + sync on reconnect)? | Epic 6 | McManus |
| OQ-08 | Return refund method: original payment method only, or store credit also supported? | Epic 6 | McManus |
| OQ-09 | Data residency / sovereignty requirements (affects Azure region selection)? | Infrastructure | McManus |
| OQ-10 | Entra ID auth strategy for shared POS tablet (broker account-switch / staff PIN + token vending)? | Epic 2.1 | Fenster |
| OQ-11 | Member QR token lifetime and validation approach (server-side vs. locally verifiable JWT)? | Epic 1.2 | Fenster |
| OQ-12 | Promotion discount stacking rules (mutual exclusion / best-deal-wins / additive)? | Epics 1.3, 2.3, 3.2 | Fenster |
| OQ-13 | Approval chain depth: fixed 2-level or configurable N-level? | Epics 3.2, 3.3 | Fenster |
| OQ-14 | API localisation strategy for EN/BM dynamic content (header-driven / bilingual object / separate endpoints)? | All three apps | Fenster |

---

### ADR-009: ECR Integration Protocol (MVP)
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

ECR (Electronic Cash Register) REST API integration is **deferred post-MVP** and is out of scope for the initial release. No ECR vendor or protocol will be implemented until after MVP delivery.

**Rationale:** Avoids premature integration and reduces MVP complexity. Allows focus on core POS flows.

**Rejected:** Early ECR integration, proprietary SDKs.

---

### ADR-010: SMS Gateway Abstraction
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

SMS gateway provider is **TBD**. All SMS sending is abstracted behind an `ISmsGateway` interface, allowing the provider to be swapped without code changes. Implementation must support Twilio, AWS SNS, or local telco with minimal effort.

**Rationale:** Enables late binding of provider and easy future replacement.

---

### ADR-011: SAP Auth Object Schema Ownership
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

SAP authorization object schema is **collaboratively designed**: backend (McManus) proposes, business approves. Backend team drafts initial schema, business reviews and signs off.

**Rationale:** Ensures technical feasibility and business alignment.

---

### ADR-012: Multi-Store Support
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

**Multi-store is supported from day one**. All relevant entities include a `shop_id` (or equivalent) as a first-class field. No single-store shortcuts in schema or logic.

**Rationale:** Avoids costly refactor later; supports future growth.

---

### ADR-013: Coupon Issuance Timing
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

Coupons are **pre-issued on activation**: batch-generated for eligible members when a promotion goes live, not on-demand at first browse.

**Rationale:** Simplifies eligibility logic and enables proactive communication.

**Rejected:** On-demand issuance at first browse.

---

### ADR-014: Approval Escalation Modes
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

Both escalation modes are supported:
- (A) SAP HCM org hierarchy with configurable number of levels
- (B) Step-by-step, each step defined by SAP position (not specific user)
Configurable per event/workflow type.

**Rationale:** Flexibility for different business processes.

---

### ADR-015: Offline POS Support (MVP)
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

**No offline POS support for MVP**. POS is online-only; no local queue or sync on reconnect.

**Rationale:** Reduces complexity and risk for MVP. Can be revisited post-MVP.

---

### ADR-016: Return Refund Method
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

Refunds are processed to the **original payment method only**. Store credit is not supported.

**Rationale:** Simpler reconciliation and compliance.

---

### ADR-017: Data Residency
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

**No data residency requirement**. Use the nearest/cheapest Azure region (Southeast Asia).

**Rationale:** Minimizes cost and latency. No legal constraint identified.

---

### ADR-018: Shared Tablet Authentication
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

**Broker account-switch** is used for shared POS tablets: MSAL shared device mode, each staff logs in/out of their own Entra ID account.

**Rationale:** Aligns with Microsoft best practices and security requirements.

---

### ADR-019: Member QR Token Validation
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

**Server-side lookup** for member QR tokens: QR contains a reference ID, POS calls API to validate. No locally verifiable JWT. Aligns with online-only POS decision.

**Rationale:** Simpler, more secure, and consistent with online-only architecture.

---

### ADR-020: Discount Stacking Rules
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

Discount stacking is **configurable per promotion**. Each promotion has a `stackable` flag to control stacking behavior.

**Rationale:** Supports both exclusive and combinable promotions as needed.

---

### ADR-021: Approval Chain Depth
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

Approval chain depth is **configurable (N-level)**. Admin defines the number of levels per workflow.

**Rationale:** Flexibility for different approval processes.

---

### ADR-022: Localisation and Language Support
**Date:** 2026-05-12 | **Status:** Accepted | **Author:** Keyser

- **No Bahasa Malaysia**. Supported languages: ZHT (Traditional Chinese) and EN.
- API always returns bilingual object `{ en, zht }`.
- POS app: ZHT only.
- Member app UI: ZHT, ZHS, EN.
- SAP master data: EN primary, ZHT optional, no ZHS in SAP. ZHS requires a separate translation layer for member app display.

**Rationale:** Aligns with business requirements and SAP data constraints.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
- Open Questions must be resolved and promoted to Active Decisions before dependent epics enter sprint planning

---

# Decision: Sprint 3 Schema — Promotions, Sales, Attendance

**Date:** 2026-05-12T22:22:36+08:00
**Author:** Edie (Database)
**Status:** Delivered
**Sprint:** 3

---

## Summary

Delivered EF Core entity configurations, migration `20260512020000_Sprint3_PromotionsSalesAttendance`, updated model snapshot, and `DATA-DESIGN.md` Sprint 3 section for Promotions, Sales, and Attendance domains. 7 new tables created.

---

## Files Created / Modified

| File | Action |
|------|--------|
| `src/M2.Infrastructure/Persistence/Configurations/PromotionConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/CouponConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/PromotionProductConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/SalesTransactionConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/SalesLineItemConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/ReturnTransactionConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/AttendanceRecordConfiguration.cs` | Created |
| `src/M2.Infrastructure/M2DbContext.cs` | Updated — 7 new DbSet properties added |
| `src/M2.Infrastructure/Migrations/20260512020000_Sprint3_PromotionsSalesAttendance.cs` | Created |
| `src/M2.Infrastructure/Migrations/20260512020000_Sprint3_PromotionsSalesAttendance.Designer.cs` | Created |
| `src/M2.Infrastructure/Migrations/M2DbContextModelSnapshot.cs` | Updated — Sprint 3 entities added |
| `docs/data/DATA-DESIGN.md` | Updated — Sprint 3 tables section added |

---

## Key Decisions

### D1 — Followed domain model over spec
McManus had pre-authored domain entities with richer models than the task spec. All configurations were written to match the actual domain code, not the spec. Deviations are documented below.

### D2 — Enums stored as varchar(50) strings
`PromotionType`, `PromotionStatus`, `PaymentMethod`, `SalesStatus`, `AttendanceSource` all stored as strings via `HasConversion<string>()`. Consistent with Sprint 2 precedent (ADR-003).

### D3 — SalesLineItem extends BaseEntity
Spec implied a lightweight entity. McManus's domain has `SalesLineItem : BaseEntity` — full audit trail preserved. This adds TenantId/ShopId to line items, enabling direct queries without joining to the parent transaction.

### D4 — SalesLineItem uses plain bilingual strings, not BilingualText
The domain uses `ProductNameEn`/`ProductNameZht` string properties (not a `BilingualText` owned type). Mapped to columns `ProductName_en` / `ProductName_zht` to match convention. This is a name snapshot at point-of-sale, not a live bilingual entity.

### D5 — ReturnTransaction has IsComplete + nullable ProcessedAt
Spec described only `processed_at`. McManus's domain adds `IsComplete bool` (default false) and makes `ProcessedAt` nullable — set only on completion. Both mapped in the configuration and migration.

### D6 — PromotionProduct has DiscountValue
Spec had only `(promotion_id, product_id)` composite PK. Domain adds `DiscountValue decimal` for per-product discount override. Included in configuration, migration, and snapshot.

### D7 — Cross-module references stored without FK constraints (ADR-001)
`coupons.MemberId`, `sales_transactions.MemberId`, and `promotions.ApprovalRequestId` are IDs referencing other bounded contexts. No DB-level FK constraints created — consistent with ADR-001 (no cross-module navigation). Data integrity enforced at application layer.

### D8 — ReturnTransaction FK uses RESTRICT
`return_transactions.OriginalTransactionId → sales_transactions` uses `OnDelete(Restrict)` to prevent accidental deletion of source transactions with outstanding returns. Same rationale as `notification_logs → notification_templates` in Sprint 2.

### D9 — Down() order
Strict reverse FK dependency order: `return_transactions` → `sales_line_items` → `promotion_products` → `coupons` → `sales_transactions` → `promotions` → `attendance_records`.

---

## Build Verification

`dotnet build src/M2.Infrastructure/M2.Infrastructure.csproj` — **0 errors, 0 warnings**.

---

## Open Questions / Recommendations

- **OQ-SPRINT3-01:** `SalesTransaction` doesn't have a navigation property to `ReturnTransaction` in the domain. `ReturnTransactionConfiguration` uses `WithMany()` (no collection on principal). If backend needs to eager-load returns from a transaction, a navigation property should be added to `SalesTransaction`.
- **OQ-SPRINT3-02:** Recommend `dotnet ef migrations add` regeneration once McManus finalises all domain factories — the hand-written snapshot may drift if any property ordering differs from EF Core's auto-generation.
- **OQ-SPRINT3-03:** `promotions.ApprovalRequestId` stores the cross-module reference but no FK constraint. If promotions are deleted while an approval is in-flight, the approval will have an orphan `EntityId`. Workflow guard should be added at application layer.

---

### Edie — 2026-05-12 — Sprint 1 DB Foundation

# Edie — Sprint 1 DB Decisions

**Date:** 2026-05-12  
**Author:** Edie (Database)  
**Sprint:** 1 — Database Foundation

---

## Schema Conventions

| Convention | Decision |
|------------|----------|
| Default schema | `m2` |
| PK type | `Guid` (sequential GUIDs — aligns with Coding Standards from ADR-003) |
| Column naming | PascalCase (EF Core default — aligns with rest of codebase) |
| Timestamp columns | `timestamptz` (DateTimeOffset) — timezone-aware, required on all entities |
| Soft delete | `IsDeleted bool DEFAULT false` on all entities — no hard deletes |
| Migrations history | `m2.__EFMigrationsHistory` (scoped to schema) |

---

## Multi-Tenancy (DB-001 + ADR-012)

- **Approach:** Shared database, `TenantId` (Guid, NOT NULL) column on every table
- **Multi-store:** `ShopId` (Guid, NOT NULL) first-class on every entity — no nullable shop_id allowed (ADR-013 resolved OQ-04)
- Both columns are set by the application layer; no DB default — forcing the caller to be explicit

---

## Bilingual Text (ADR-022)

- `BilingualText` is mapped as an EF Core **owned entity** (not a separate table, not JSONB)
- Column naming pattern: `{propertyName}_en`, `{propertyName}_zht`
- Both `_en` and `_zht` are `IsRequired()` — storing only one language is a schema violation
- Extension method `OwnsOneBilingual<TEntity>()` enforces this consistently across all configurations

**Rejected alternatives:**
- JSONB column: harder to query/index individual languages
- Separate `LocalizedText` table: join overhead, more complex migrations
- Single column with delimiter: unacceptable — no type safety

---

## Base Configuration Pattern

All entity configurations extend `BaseEntityConfiguration<TEntity>` which applies shared column mappings. Concrete configurations call `base.Configure(builder)` then add entity-specific mappings (indexes, relationships, query filters for soft delete).

---

## Migration Strategy

- Migrations written manually when EF CLI cannot connect to a live DB (CI/design-time)
- Timestamp format: `yyyyMMddHHmmss` (e.g., `20260512000000_InitialCreate`)
- `InitialCreate` migration only creates the `m2` schema — no tables (Sprint 1 scope)
- Per-module migrations will be added in Sprints 2–4 as domain tables are built

---

## Key Files

| File | Purpose |
|------|---------|
| `M2DbContext.cs` | EF Core DbContext; calls `ApplyConfigurationsFromAssembly` |
| `M2DbContextFactory.cs` | IDesignTimeDbContextFactory for `dotnet ef` CLI tooling |
| `BaseEntityConfiguration.cs` | Abstract base — TenantId, ShopId, audit, soft-delete columns |
| `BilingualTextConfiguration.cs` | `OwnsOneBilingual` extension — `{prop}_en` / `{prop}_zht` columns |
| `DatabaseOptions.cs` | Strongly-typed config for connection string and EF options |
| `20260512000000_InitialCreate.cs` | Creates `m2` schema; stub for future table migrations |
| `M2DbContextModelSnapshot.cs` | Empty model snapshot (no tables yet) |

---

### Fenster — 2026-05-12 — Sprint 1 Shells

# Fenster — Sprint 1 Shell Decisions (Inbox)

**Date:** 2026-05-12  
**Author:** Fenster  
**Status:** Pending team review

---

## Decision 1: Component Library for m2-portal — MudBlazor

**Chose:** MudBlazor 7.15.0 (upgraded to 3.8.3 of Microsoft.Identity.Web for security)  
**Rejected:** Radzen Blazor Components

### Rationale

| Factor | MudBlazor | Radzen |
|--------|-----------|--------|
| Licence | MIT (fully free) | Free tier has limitations on some components |
| Component breadth | ~80+ components including DataGrid, Charts | Comparable but some premium-gated |
| Theming | MudThemeProvider — simple, Material Design 3 aligned | OK but less Material-native |
| Community & GitHub stars | 8k+ stars, very active | 3k+ stars |
| Blazor Server support | First-class | First-class |
| SignalR / real-time readiness | Compatible | Compatible |

MudBlazor aligns with the `initial_request.md` spec ("ASP.NET Blazor Web App with **Material Design** UI") and is MIT-licensed with no component feature gates.

---

## Decision 2: `msal_auth` Package for Flutter MSAL

**Chose:** `msal_auth ^1.0.8`  
**Rejected:** `flutter_appauth`, `aad_oauth`

### Rationale

`msal_auth` wraps the native MSAL SDK (Microsoft Authentication Library) directly on both Android and iOS, exposing:
- `SingleAccountPca` — POS shared-device mode (maps 1:1 to ADR-018)
- `MultipleAccountPca` — standard personal device mode (Promos app)

`flutter_appauth` is an OAuth2 AppAuth wrapper that works with any OIDC provider but does not expose the native MSAL account-switch broker API needed for ADR-018 shared-device mode. `msal_auth` gives us the right abstraction at the right level for our two distinct auth patterns.

---

## Decision 3: Flutter Localisation Toolchain — `flutter gen-l10n`

**Chose:** Flutter built-in `flutter gen-l10n` with ARB files  
**Rejected:** `easy_localization`, `intl_utils`, custom JSON loader

### Rationale

`flutter gen-l10n` is the Flutter SDK's official localisation pipeline. It generates type-safe `AppLocalizations` classes from `.arb` files at build time. Zero runtime overhead, no third-party dependency, IDE completion support, and aligns with the project's generated-code approach (Riverpod codegen, Mapperly on backend).

ARB files are placed in `lib/core/l10n/` per the sprint task directory layout.

---

## Decision 4: Locale Switching via Riverpod `StateProvider`

**Chose:** `localeProvider` as a Riverpod `StateProvider<Locale?>` passed to `MaterialApp.locale`  
**Deferred:** Persistence via `shared_preferences`

### Rationale

For Sprint 1, locale switch is in-memory only. A `StateProvider` wired to `MaterialApp.locale` delivers instant live locale switching with no rebuild overhead. Persistence will be added in Sprint 2 when `shared_preferences` is added to the meka-promos dependency tree.

---

## Items Needing Team Input

1. **Azure App Registration IDs** — Placeholder client/tenant IDs used across all three apps. Real IDs needed before any auth flow can be tested. These should be injected via `--dart-define` (Flutter) and `appsettings.{env}.json` (Blazor), not hardcoded.
2. **`msal_config.json` for Android** — Both Flutter apps need a valid MSAL config JSON in `android/app/src/main/res/raw/msal_config.json` for the Android broker auth flow. Template path referenced in `msal_auth` plugin docs. DevOps/Backend to provide the config once App Registrations exist.
3. **Entra ID App Registration redirect URIs** — For m2-portal Blazor Server, the redirect URI must be registered: `https://{host}/signin-oidc`. For Flutter apps, the Android/iOS redirect URI format required by MSAL.

---

### Keyser — 2026-05-12 — Sprint 1 Plan

- Sequenced backend platform, approval, notification, member, promotions, sales, attendance, goods receipt, and SAP integration into 4 sprints.
- Critical path is backend-first: platform → approval → notification → member → promotions → sales → attendance → goods receipt → SAP.
- Frontend app foundations and UI/UX polish are parallelizable after Sprint 1.
- All open questions resolved; scope is stable for MVP.
- SAP and ECR integration risks noted; ECR deferred post-MVP.

---

### McManus — 2026-05-12 — Sprint 1 Platform

# McManus Sprint 1 — Platform Architecture Decisions

**Date:** 2026-05-12T20:02:06+08:00  
**Author:** McManus  
**Sprint:** 1 — Platform Foundation & Infrastructure

---

## Decision: SharedKernel enforces both TenantId AND ShopId at BaseEntity level

**Context:** ADR-013 (multi-store from Day 1) and DB-001 (TenantId on all tables).  
**Decision:** `BaseEntity` implements both `ITenanted` and `IShopScoped`, making both Guid properties non-optional on every entity. There is no "single-store" shortcut available.  
**Consequence:** Every entity creation requires explicit TenantId + ShopId. Application layer (BFF endpoints) must extract these from the JWT claims before calling domain constructors. Sprint 2 auth middleware must expose a `ITenantContext` / `IShopContext` abstraction.

---

## Decision: BilingualText as a record (value object) with EF owned entity convention

**Context:** ADR-022 requires `{en, zht}` bilingual responses on all API output.  
**Decision:** `BilingualText` is a C# `record` (immutable, value semantics). EF Core maps it as an owned entity. Column naming convention: `{navigationPropertyName}_en` and `{navigationPropertyName}_zht`. Helper method `OwnsOneBilingual<T>()` on `EntityTypeBuilder<T>` enforces this convention.  
**Consequence:** Domain entities with displayable names declare `BilingualText Name { get; }` etc. Direct string columns for localised text are forbidden.

---

## Decision: SapConnector project is an anti-corruption layer with no framework dependencies

**Context:** ADR-006 (OData REST primary, NCo RFC fallback), ADR-001 (modular monolith).  
**Decision:** `M2.SapConnector` references only `M2.SharedKernel` and `Microsoft.Extensions.*`. No EF Core, no BFF-specific packages. It exposes interfaces only; no-op implementations live in the same project behind `internal` visibility. BFFs and Infrastructure consume interfaces via DI.  
**Consequence:** SAP implementation can be replaced in Epic 9 with zero interface changes. Polly policies will be added in `SapConnectorServiceExtensions` during Sprint 4.

---

## Decision: Outbox deferred to Sprint 4; IOutboxService interface locked Sprint 1

**Context:** ADR-017 (Outbox pattern for SAP writes). Hangfire is not needed until Goods Receipt (Sprint 4).  
**Decision:** `IOutboxService` interface (`EnqueueAsync<TMessage>` + `ProcessPendingAsync`) is defined and registered as a no-op in Sprint 1. The interface contract is intentionally minimal — message serialisation format TBD when Hangfire is wired.  
**Consequence:** Any module that needs reliable SAP writes must inject `IOutboxService`, not call SAP directly. This ensures the pattern is enforced from first use.

---

## Open items for Sprint 2

1. `ApiKeyMiddleware` needs SHA-256 hash comparison logic and config-driven key store (BE-REC-001 R4).
2. `ITenantContext` / `IShopContext` service abstractions needed — BFF endpoints must extract TenantId + ShopId from Entra ID JWT claims.
3. CORS `AllowAll` policy is a dev placeholder — tighten to allowed origins per environment.
4. Serilog Azure Application Insights sink not configured — add when ACA deployment pipeline is ready.
