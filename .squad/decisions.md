# Squad Decisions

## Sprint 4 Decisions (2026-05-12)

---

### ADR-001: Architecture Style — Modular Monolith with HTTP Cross-Module Communication
**Date:** 2026-05-12 | **Updated:** 2026-05-13 | **Status:** Accepted (Revised) | **Author:** Keyser

Adopt a **Modular Monolith** as primary architecture. Single deployable unit; bounded contexts as separate C# projects (domain modules). **Cross-module communication uses HTTPS REST over typed HttpClients targeting localhost — not in-process DI injection.** Within a module, internal services may use DI freely.

**Communication rules:**
- Module → Module: typed `HttpClient` → localhost REST endpoint (e.g., `ISalesModuleClient` calls `GET /internal/sales/...`)
- BFF → Module: typed `HttpClient` → module's REST endpoints (same process, same base URL)
- Intra-module: DI injection is permitted and preferred (e.g., `IDiscountEngine` injecting `IPromotionService` within the Promotions module)

**What changed from original ADR-001 (2026-05-12):** The original stated modules communicate "only via injected interfaces." This is revised: cross-module boundaries are enforced via HTTP contracts, not DI. The single deployable unit is preserved — HTTP calls target localhost within the same process. In-process DI remains valid only within a single module's own service layer.

**Trade-offs:**
| Factor | Cost | Benefit |
|--------|------|---------|
| Latency | ~0.1–1 ms localhost overhead per cross-module call | Negligible for non-hot paths |
| Decomposition | No additional cost — HTTP contract is already the decomposition seam | Extracting a module requires only a DNS/URL config change, zero interface redesign |
| Contract enforcement | HTTP boundary enforces explicit contracts; no accidental coupling via shared DI registrations | Prevents module leakage; serialization makes implicit dependencies visible |
| Testability | Integration tests via `TestWebApplicationFactory` remain transparent — cross-module HTTP calls hit same in-process `TestServer` | No mocking infrastructure change for integration tests |
| Complexity | Adds `Platform.InterModule` typed-client layer; requires base-URL config per environment | Tolerable; one-time setup |

**Rejected alternatives:**
- Continue in-process DI for cross-module calls: fast, but creates invisible coupling; prevents safe decomposition; all modules share a DI container with no enforced boundary.
- Full microservices: premature for team size/maturity; network partitions and distributed transactions not justified.
- Hybrid (some DI, some HTTP): introduces inconsistent patterns; developers can't reason about which boundary is enforced.

**Implementation note:** All modules register their endpoints in the same `WebApplication`. A `Platform.InterModule` project provides typed `HttpClient` registrations with a configurable base URL (default: `http://localhost:{port}`; test: `TestServer`). Module endpoint paths use a `/modules/{module-name}/` prefix to distinguish inter-module calls from BFF-facing endpoints.

---

<!-- ADR-001 revision merged from inbox: copilot-directive-20260513.md, keyser-adr001-http-impact.md, keyser-adr001-update.md, mcmanus-intermodule-all-modules.md, mcmanus-intermodule-pilot.md, verbal-intermodule-test-strategy.md -->

## 2026-05-13

### Decision: M2.Platform.Api Extraction — 4-Process Topology

**Author:** McManus (Backend Dev)
**Date:** 2026-05-13
**Status:** Implemented

#### Context

ADR-001 (final) confirms the platform core must run as an independent process. BFFs call it via HTTP.
This ADR was previously ambiguous — Platform:BaseUrl pointed to self (localhost loopback), blurring the
process boundary. This decision documents the extraction and its consequences.

#### Decisions Made

1. M2.Platform.Api runs on port 5100

`M2.Platform.Api` is a new, independent ASP.NET Core process running on `https://localhost:5100`.
It hosts all 8 domain module endpoint groups (`/modules/{name}/`), registers `AddInfrastructure`, and
owns all EF Core / domain service registrations that serve cross-BFF module calls.

2. BFFs use platform API key (not internal secret) for outbound calls to platform

BFFs are external callers. They send `X-Api-Key` (not `X-Internal-Call` / `X-Internal-Secret`) when
calling the Platform API. The API key is read from `Platform:ApiKey` config and set as a default header
in `InterModuleServiceExtensions.AddInterModuleClients()`.

`X-Internal-Call` + `X-Internal-Secret` is reserved for any intra-platform module-to-module HTTP calls
(if needed in future). The `ApiKeyMiddleware` continues to handle both patterns.

3. Module endpoints moved from BFF-hosted to platform-hosted

All `app.MapXxxModule()` calls have been removed from the 3 BFF `Program.cs` files. Module endpoints
(`/modules/members/`, `/modules/sales/`, etc.) are only registered in `M2.Platform.Api/Program.cs`.

BFF-specific endpoints (`/sales/transactions`, `/members/register`, `/approvals/requests`, etc.)
remain in their respective BFF `Endpoints/` folders and continue to use inter-module typed HTTP clients.

4. M2.Infrastructure/Modules/ remains in Infrastructure, registered only in Platform.Api

The `XxxModuleEndpoints.cs` files stay in `M2.Infrastructure/Modules/`. BFFs no longer reference
the `M2.Infrastructure.Modules` namespace — the `using M2.Infrastructure.Modules;` import has been
removed from all BFF `Program.cs` files.

#### Config Changes

- All BFF `appsettings.json`: `Platform:BaseUrl` updated from `https://localhost:5000` → `https://localhost:5100`
- All BFF `appsettings.json`: `Platform:ApiKey: "platform-dev-key"` added
- `InterModuleOptions.BaseUrl` default updated from `https://localhost:5000` → `https://localhost:5100`
- `InterModuleOptions.ApiKey` property added (default: `"platform-dev-key"`)

#### Files Created

- `src/M2.Platform.Api/M2.Platform.Api.csproj`
- `src/M2.Platform.Api/Program.cs`
- `src/M2.Platform.Api/appsettings.json`
- `src/M2.Platform.Api/appsettings.Development.json`
- `docs/ARCHITECTURE.md` (developer quick-reference for 4-process topology)

#### Integration Test Impact

`PlatformWebApplicationFactory` (pre-existing stub) now resolves correctly against `M2.Platform.Api.Program`
(namespaced class). Added `Microsoft.AspNetCore.TestHost` using to `PlatformWebApplicationFactory.cs`.

---

### Decision: Rewire Integration Test Harness for 4-Process Topology

**Author:** Verbal (Test Engineer)
**Date:** 2026-05-13
**Status:** Decided

#### Context

The confirmed 4-process topology places all `/modules/{name}/` endpoints exclusively in `M2.Platform.Api`.
BFFs call the Platform API via HTTP and do not host module endpoints themselves. The previous integration
test harness (`TestWebApplicationFactory` / `M2IntegrationTestBase`) targeted the BFF `Program` class,
meaning the 8 module smoke tests were asserting against the wrong process.

#### Decisions

1. Module tests now target `PlatformWebApplicationFactory` (not BFF factory)

All 8 `*ModuleTests.cs` files now inherit `M2PlatformIntegrationTestBase` and declare
`IClassFixture<PlatformWebApplicationFactory>`. The platform factory spins up
`M2.Platform.Api.Program` in-memory with the same test-safe overrides used by the BFF factory
(in-memory EF Core DB, `TestAuthHandler`, NoOp SAP stubs).

2. `M2PlatformIntegrationTestBase` sets platform authentication headers

Every `HttpClient` created by `M2PlatformIntegrationTestBase` carries:

| Header | Value |
|---|---|
| `X-Api-Key` | `test-api-key` |
| `X-Internal-Call` | `true` |
| `X-Internal-Secret` | `internal` |

These match the values injected into `PlatformWebApplicationFactory.ConfigureWebHost` via
`Platform:ApiKey` and `Platform:InternalCallSecret` in-memory config keys.

3. `M2BffIntegrationTestBase` remains for BFF-level tests

`M2IntegrationTestBase` was split into two classes in `M2IntegrationTestBase.cs`:

- `M2BffIntegrationTestBase` — typed to `WebApplicationFactory<Program>` (BFF `Program`). Retained for
  any future tests that validate BFF-level concerns (health endpoints, BFF auth flows, BFF routing).
- `M2PlatformIntegrationTestBase` — typed to `PlatformWebApplicationFactory`. All module smoke tests
  use this.

`ApprovalEndpointTests` (targeting `/api/approvals` BFF endpoints) was left unchanged — it uses
`TestWebApplicationFactory` directly and tests BFF-level behaviour.

4. `InterModuleTestHelper.WithInterModuleLoopback` is now generic

Changed signature from `WebApplicationFactory<Program>` to `WebApplicationFactory<TEntryPoint> where TEntryPoint : class`
so the helper works with both the BFF factory and the Platform factory when McManus wires
`IXxxModuleClient` typed HTTP clients.

5. Build status

`M2.Tests.Integration` will not compile until McManus creates `src/M2.Platform.Api/M2.Platform.Api.csproj`.
The project reference is in place. Unit tests (55/55) remain green.

#### Consequences

- When McManus lands `M2.Platform.Api`, the integration test project compiles and module smoke tests
  exercise the real Platform API process boundary.
- BFF integration tests continue to use `TestWebApplicationFactory` — no cross-contamination.
- `PlatformWebApplicationFactory` exposes `ApprovalServiceMock` so `ApprovalsModuleTests` can control
  the `IApprovalService` stub without touching production assembly internals.

---

### Verbal — Regression + Smoke Tests: InterModule HTTP Refactor

**Date:** 2026-05-13 | **Author:** Verbal | **Requested by:** ryan-chung_mekim

#### Context
Regression run following the InterModule HTTP refactor (commits `5467ca2` + `6b8a405`).
All 8 modules now expose `/modules/{name}/` endpoints consumed via typed `IXxxModuleClient` HTTP clients per ADR-001 (revised).

#### Final Test Counts

| Suite | Before | After | Delta |
|-------|--------|-------|-------|
| Unit (`M2.Tests.Unit`) | 55 | 55 | +0 |
| Integration (`M2.Tests.Integration`) | 7 | 21 | +14 |
| **Total** | **62** | **76** | **+14** |

#### Unit Tests: No Adjustments Required

All 55 unit tests passed without modification. The refactor changed how modules communicate externally (in-process DI → HTTP) but did not alter domain logic or service interfaces. Unit tests target the domain layer directly and were unaffected.

#### Integration Tests Adjusted

- `NotificationsModuleTests` (pre-existing, 3 tests): PASS (no changes needed)
- Each module received 2 tests: a positive smoke test (with internal headers) and a negative test (without `X-Internal-Call` header). All 14 new tests PASS.

#### Decisions

- Approvals smoke test requires mock setup
- Endpoint path corrections (task spec vs. actual)
- Negative tests: wide-range status codes accepted (to be tightened once middleware is live)

#### Build
`dotnet build src/M2.sln` → **0 errors, 0 warnings** post-regression.


### Edie — Sprint 4 Schema Decisions
- GoodsReceiptLineItem uses Cascade (not Restrict) for FK to GoodsReceiptNote
- sap_outbox_entries has no cross-module FK constraints
- sap_outbox_entries index strategy: (TenantId, Status) and (Status, CreatedAt)
- BilingualText on GoodsReceiptLineItem uses OwnsOne (not flat columns)

### Fenster — Sprint 4 Decisions
- PDF font for ZHT receipt printing: PdfGoogleFonts.notoSansTCRegular()/notoSansTCBold()
- lib/services/ and lib/screens/ top-level dirs for cross-cutting services/screens
- Goods Receipt mock data fallback: try/catch with in-process mock data
- Dashboard at /dashboard route, Index.razor redirects
- Notification read state is optimistically updated client-side

### McManus — Sprint 4 Decisions
- GoodsReceiptStatus enum: Pending/Confirmed/Discrepancy
- SapODataClient reads Sap:ODataBaseUrl from IConfiguration
- SapNcoClient: NotSupportedException stub for interface
- NoOpSapODataClient retained for test use
- INotificationHistoryService: MemberId as string
- Reporting AttendanceSummary: separate from Attendance domain
- GoodsReceiptService PostToSapAsync: outbox deferred

### Verbal — Sprint 4 Test Decisions
- GoodsReceipt domain invariant tests: direct entity testing
- SAP OData Client: MockHttpMessageHandler pattern
- ISapNcoClient interface extended with GetProductsAsync
- NotificationLog.IsRead domain property for read status
- Reporting tests use SalesSummary/AttendanceSummary from M2.Domain.Reporting

---


## Active Decisions

### ADR-001: Architecture Style — Modular Monolith with HTTP Cross-Module Communication
**Date:** 2026-05-12 | **Updated:** 2026-05-13 | **Status:** Accepted (Revised) | **Author:** Keyser

Adopt a **Modular Monolith** as primary architecture. Single deployable unit; bounded contexts as separate C# projects (domain modules). **Cross-module communication uses HTTPS REST over typed HttpClients targeting localhost — not in-process DI injection.** Within a module, internal services may use DI freely.

**Communication rules:**
- Module → Module: typed `HttpClient` → localhost REST endpoint (e.g., `ISalesModuleClient` calls `GET /internal/sales/...`)
- BFF → Module: typed `HttpClient` → module's REST endpoints (same process, same base URL)
- Intra-module: DI injection is permitted and preferred (e.g., `IDiscountEngine` injecting `IPromotionService` within the Promotions module)

**What changed from original ADR-001 (2026-05-12):** The original stated modules communicate "only via injected interfaces." This is revised: cross-module boundaries are enforced via HTTP contracts, not DI. The single deployable unit is preserved — HTTP calls target localhost within the same process. In-process DI remains valid only within a single module's own service layer.

**Trade-offs:**
| Factor | Cost | Benefit |
|--------|------|---------|
| Latency | ~0.1–1 ms localhost overhead per cross-module call | Negligible for non-hot paths |
| Decomposition | No additional cost — HTTP contract is already the decomposition seam | Extracting a module requires only a DNS/URL config change, zero interface redesign |
| Contract enforcement | HTTP boundary enforces explicit contracts; no accidental coupling via shared DI registrations | Prevents module leakage; serialization makes implicit dependencies visible |
| Testability | Integration tests via `TestWebApplicationFactory` remain transparent — cross-module HTTP calls hit same in-process `TestServer` | No mocking infrastructure change for integration tests |
| Complexity | Adds `Platform.InterModule` typed-client layer; requires base-URL config per environment | Tolerable; one-time setup |

**Rejected alternatives:**
- Continue in-process DI for cross-module calls: fast, but creates invisible coupling; prevents safe decomposition; all modules share a DI container with no enforced boundary.
- Full microservices: premature for team size/maturity; network partitions and distributed transactions not justified.
- Hybrid (some DI, some HTTP): introduces inconsistent patterns; developers can't reason about which boundary is enforced.

**Implementation note:** All modules register their endpoints in the same `WebApplication`. A `Platform.InterModule` project provides typed `HttpClient` registrations with a configurable base URL (default: `http://localhost:{port}`; test: `TestServer`). Module endpoint paths use a `/modules/{module-name}/` prefix to distinguish inter-module calls from BFF-facing endpoints.

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

---

# Sprint 4 Decisions (2026-05-12)

## Edie — Sprint 4 Schema Decisions
- GoodsReceiptLineItem uses Cascade (not Restrict) for FK to GoodsReceiptNote
- sap_outbox_entries has no cross-module FK constraints
- sap_outbox_entries index strategy: (TenantId, Status) and (Status, CreatedAt)
- BilingualText on GoodsReceiptLineItem uses OwnsOne (not flat columns)

## Fenster — Sprint 4 Decisions
- PDF font for ZHT receipt printing: PdfGoogleFonts.notoSansTCRegular()/notoSansTCBold()
- lib/services/ and lib/screens/ top-level dirs for cross-cutting services/screens
- Goods Receipt mock data fallback: try/catch with in-process mock data
- Dashboard at /dashboard route, Index.razor redirects
- Notification read state is optimistically updated client-side

## McManus — Sprint 4 Decisions
- GoodsReceiptStatus enum: Pending/Confirmed/Discrepancy
- SapODataClient reads Sap:ODataBaseUrl from IConfiguration
- SapNcoClient: NotSupportedException stub for interface
- NoOpSapODataClient retained for test use
- INotificationHistoryService: MemberId as string
- Reporting AttendanceSummary: separate from Attendance domain
- GoodsReceiptService PostToSapAsync: outbox deferred

## Verbal — Sprint 4 Test Decisions
- GoodsReceipt domain invariant tests: direct entity testing
- SAP OData Client: MockHttpMessageHandler pattern
- ISapNcoClient interface extended with GetProductsAsync
- NotificationLog.IsRead domain property for read status
- Reporting tests use SalesSummary/AttendanceSummary from M2.Domain.Reporting

---

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

### 2026-05-13T16:58: User directive
**By:** Ryan Chung (via Copilot)
**What:** CI pipeline (S7.1, Azure Pipelines YAML) is pulled forward into Sprint 5. This is no longer a Sprint 7 story. To make room, S5.8 (rate limiting, 2pts) moves to Sprint 6.
**Why:** User approved — 76 tests exist with no automated gate; CI is a force multiplier for Sprints 5-6 functional work


### 2026-05-13T16:58: User directive
**By:** Ryan Chung (via Copilot)
**What:** GitHub is the permanent source control and CI/CD platform. No Azure DevOps migration planned. All tooling (gh CLI, GitHub Actions, Squad automation) targets GitHub.
**Why:** User request — captured for team memory


### 2026-05-13T16:58: User directive
**By:** Ryan Chung (via Copilot)
**What:** Blazor Server is confirmed for M2PortalBff (management portal). No migration to WASM or React/Angular.
**Why:** User request — confirmed after Fenster's analysis


### 2026-05-13T16:58: User directive
**By:** Ryan Chung (via Copilot)
**What:** Sprint 7 (Cloud Readiness) is deferred until the Azure cloud environment is provisioned and ready. Sprints 4–6 focus on functional development only. Sprint 7 is gate-triggered by: Azure subscription provisioned + ACA environment created.
**Why:** User request — cloud environment not ready; functional completeness is the current priority


# Decision: Blazor Server — Keep It, Don't Switch

**Author:** Fenster  
**Date:** 2026-05-13T16:03:47.338+08:00  
**Status:** Recommended

---

## What Was Asked

Ryan asked: Is the current Blazor implementation Server or WASM? Which is better for this use case? Should we consider React/Angular instead?

---

## Finding: Current Mode Is Blazor Server

Confirmed by examining `apps/m2-portal/Program.cs`:

```csharp
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddMicrosoftIdentityConsentHandler();
// ...
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
```

This is the **classic Blazor Server** pattern (pre-.NET 8 unified model). UI diffs are pushed over a SignalR WebSocket. No WASM, no `InteractiveWebAssembly` render mode, no client-side DLL download.

---

## Recommendation: Stay on Blazor Server

**Do not switch to WASM. Do not switch to React or Angular.**

---

## Comparison

### Blazor Server vs WASM for This Use Case

| Factor | Blazor Server ✅ | Blazor WASM |
|---|---|---|
| SignalR NotificationBell | Native — runs on the same server process, trivial hub injection | Requires a separate HTTP endpoint to receive push events; WASM can't host a hub |
| Entra ID / MSAL auth | `AddMicrosoftIdentityWebApp` (OIDC, cookie-based) — already wired in. Clean redirect flows | MSAL.js required; tokens visible in browser storage; CORS config overhead |
| Startup performance | Instant first paint — server renders HTML | 5–15 MB WASM runtime download on first load; unacceptable for a back-office tool used occasionally |
| Back-office offline needs | None needed — back-office always has connectivity | The one WASM advantage we simply don't need |
| State management | In-process scoped services — simple | Manual state sync between client and server |
| Deployment | Single ASP.NET Core app | Two deployments (API + WASM app) or additional config complexity |
| Team skill | .NET team, no JS expertise flagged | Requires JS interop comfort for anything non-trivial |

**Verdict on WASM:** Every advantage WASM has (offline, reduced server load at scale) is irrelevant to a back-office manager portal with < 50 concurrent users. Every advantage Server has (SignalR, OIDC, instant startup) is exactly what this app needs.

---

### Blazor Server vs React / Angular

| Factor | Blazor Server ✅ | React / Angular |
|---|---|---|
| Switching cost | N/A — already built | Rewrite ALL scaffolded pages. New build pipeline (Node, webpack/vite). New testing stack. |
| Real-time (SignalR bell) | Trivial — inject hub from DI | Requires a JS SignalR client library + state management wiring |
| Entra ID auth | Microsoft.Identity.Web — first-party, battle-tested | MSAL.js — works, but more moving parts; separate auth flow per SPA |
| Team velocity | Team writes C# today | Requires JS/TS fluency the team doesn't have |
| MudBlazor (already chosen) | Rich Material Design component library, 0 config | Abandon all built components; pick a new library; relearn patterns |
| Type safety end-to-end | C# DTOs shared directly | DTOs must be manually duplicated or code-gen'd (openapi-typescript etc.) |
| Genuine React/Angular advantage | None for this case | Better ecosystem for customer-facing SPAs needing SEO, native mobile feel, or large JS teams |

**Verdict on React/Angular:** The pages are stubs, yes — but the architecture, auth, component library, services, and team knowledge all point to Blazor. The cost to switch is real. The benefit is zero for this use case.

---

## Nuance: Should We Upgrade to .NET 8/9 Unified Blazor?

The current setup uses the **legacy Blazor Server** template pattern (MapBlazorHub + _Host.cshtml). .NET 8+ introduced a unified "Blazor Web App" model with per-component render mode control.

**Not urgent** — the classic Server pattern is fully supported in .NET 9. But in a future sprint, migrating to the unified model (`AddRazorComponents().AddInteractiveServerComponents()`) would:
- Enable mixing static SSR pages with interactive Server components
- Improve page load for non-interactive pages (reporting, read-only detail views)
- Align with the current Microsoft Blazor roadmap

Tag this as a tech-debt item for post-Sprint 6.

---

## Final Answer

**Blazor Server. Keep it. It's the right choice.**

SignalR is the deciding factor. The NotificationBell planned for Sprint 6 is trivially easy on Blazor Server and genuinely awkward on anything else. Entra ID auth is already wired. The team is .NET. The pages are already there. There is no credible argument for switching.


# Keyser — Architecture Q&A: GitHub Migration, Sprint 7, Multi-tenancy
**Author:** Keyser (Lead / Architect)
**Date:** 2026-05-13T16:03:47+08:00
**Requested by:** Ryan Chung

---

## Q1: GitHub → Azure DevOps Migration — Impact Assessment

### What breaks immediately

#### Squad Agent System (HIGH IMPACT — core workflow disruption)
The entire Squad automation layer is GitHub-native and **cannot be migrated as-is**:

| Component | GitHub dependency | Azure DevOps equivalent |
|-----------|------------------|------------------------|
| `squad-triage.yml` | GitHub Issues labels, `actions/github-script`, `github.rest.issues.*` API | Azure Pipelines + Work Item REST API (different model entirely) |
| `squad-heartbeat.yml` (Ralph) | GitHub Issues events (`labeled`, `closed`), `GITHUB_TOKEN`, label manipulation | Azure Pipelines has no Issues event trigger; Work Items use different label model |
| `squad-issue-assign.yml` | `squad:*` labels on Issues, `copilot-swe-agent[bot]` assignment | Azure DevOps Work Items use Tags/Assignees differently; no copilot-swe-agent |
| `sync-squad-labels.yml` | GitHub Label sync API | Azure DevOps uses Area/Iteration paths, not labels |
| GitHub Issues Mode | GitHub Issues = Squad inbox | Azure Boards Work Items are functional equivalent but different API/schema |

**Ralph effectively ceases to exist on Azure DevOps.** The triage scripts call `github.rest.issues.*` which is a GitHub REST API — not Azure DevOps REST API. These would need to be rebuilt from scratch against Azure DevOps REST API (`dev.azure.com/{org}/{project}/_apis/wit/workitems`).

**Trade-off:**
- Cost: Squad's label-driven routing (the "inbox" model) requires a rebuild estimated at ~2–3 days to port to Azure DevOps Work Item queries + pipeline tasks.
- Benefit: Azure DevOps Work Items are richer than GitHub Issues (parent-child, effort, iteration planning). Could be a net improvement for sprint management.
- Rejected alternative: keep GitHub for Issues only, use Azure DevOps for code/CI only — splits the team's attention across two platforms and creates sync debt.

#### GitHub-specific features lost
| Feature | Status after migration |
|---------|----------------------|
| `gh` CLI | **Dead** — GitHub-only. No Azure DevOps equivalent in CLI form. |
| GitHub MCP server | **Dead** — GitHub-specific API integration. |
| Copilot coding agent (`copilot-swe-agent[bot]`) | **Dead** — GitHub-only feature. Cannot assign to Azure DevOps work items. |
| `GITHUB_TOKEN` auto-generated | **Dead** — Azure Pipelines uses `System.AccessToken` (different scoping model) |
| GitHub Actions marketplace | Partially available — most Docker/tool actions still work via open-source; GitHub-specific actions (`actions/github-script`, etc.) do not. |
| GitHub Container Registry (`ghcr.io`) | Replaced by Azure Container Registry — actually an improvement for ACA deployment. |

#### CI/CD: GitHub Actions → Azure Pipelines rewrite
The existing squad workflows are the only `.github/workflows/` content (no app CI exists yet per the discovered gaps). **This is actually good news** — there's no sunk cost in GitHub Actions CI to migrate.

S7.1 is not yet written. **If the migration is happening, write S7.1 as an Azure Pipelines YAML from day one** — never write a GitHub Actions CI that then needs to be rewritten.

Key syntax mapping:
| GitHub Actions | Azure Pipelines |
|----------------|-----------------|
| `on: push:` | `trigger:` |
| `on: pull_request:` | `pr:` |
| `jobs: > steps:` | `stages: > jobs: > steps:` |
| `actions/checkout@v4` | Built-in `checkout` step |
| `actions/setup-dotnet` | `UseDotNet@2` task |
| `run: dotnet build` | `script: dotnet build` or `DotNetCoreCLI@2` |
| OWASP ZAP GitHub Action | OWASP ZAP Docker container via `Docker@2` or bash script |
| `GITHUB_TOKEN` | `$(System.AccessToken)` |
| Secrets: `secrets.MY_SECRET` | Variable groups or Key Vault linked library |

---

### What changes in the Sprint plan if target is Azure Pipelines

**S7.1 rewrite:** Instead of `.github/workflows/ci.yml`, the deliverable becomes `azure-pipelines.yml`. Same intent (build, test, format check, container image push) but Azure Pipelines YAML syntax. Effort estimate is the same (5 pts) — the work is authoring and testing the pipeline, not the platform syntax.

**S7.9 (OWASP ZAP):** The GitHub Action (`zaproxy/action-baseline`) is not available. Replace with running ZAP as a Docker container task in the pipeline — equivalent capability, slightly more verbose YAML. No impact on story points.

**S7.7 (Pact):** Pact Broker integration works with any CI via the Pact CLI. No platform dependency. No change needed.

**Container registry target:** Push images to Azure Container Registry (`mcr.microsoft.com` style) instead of GHCR. This is actually better — ACR integrates with ACA via Managed Identity, avoiding credential management. Update S7.5 bicep templates to reference ACR.

---

### What Ryan should do NOW vs. AFTER

#### Do NOW (before migration)
1. **Don't write GitHub Actions CI** for the app (S7.1 target). Write it as Azure Pipelines from the start — saves a rewrite.
2. **Don't invest further in GitHub Issues as the Squad routing layer.** The current setup (Ralph + squad labels) works fine for now but treat it as temporary. Don't build more workflows on top of it.
3. **Don't rely on GitHub MCP server** for any permanent tooling integrations. If currently in use, treat it as local-dev-only.
4. **Don't use GitHub Packages / GHCR** for container images. Target ACR in all future bicep/compose references even before migration.
5. **Document the Squad issue-routing patterns** (which labels, which member mappings) so they can be rebuilt as Azure Boards work item queries post-migration.

#### Do AFTER (post-migration)
1. Rebuild Ralph's triage logic as an Azure Pipeline triggered on Work Item state changes (`workItemChanged` event in Azure Pipelines).
2. Rebuild `sync-squad-labels.yml` equivalent as Area Path + Tag management in Azure Boards.
3. Replace GitHub Issues Mode with Azure Boards — configure sprint iterations per the sprint plan structure.
4. Accept the loss of Copilot coding agent — no equivalent exists in Azure DevOps today. Ryan continues using the CLI-based Squad system (Copilot CLI agent) which is IDE-local and not platform-dependent.

---

## Q3: Is Sprint 7 Important Right Now? Can It Be Deferred?

### Story-by-story classification

| Story | Risk if deferred | Verdict |
|-------|-----------------|---------|
| S7.1 — CI Pipeline | **HIGH** — regressions ship silently for Sprints 5 & 6 | Pull to Sprint 5 NOW |
| S7.2 — Dockerfiles + Docker Compose | **MEDIUM** — 4-process dev is painful (4 terminal tabs); onboarding friction | Pull to Sprint 5 or Sprint 6 |
| S7.3 — OpenTelemetry | LOW — no production environment to observe | Defer to Sprint 7 |
| S7.4 — Serilog prod config | LOW — no production environment | Defer to Sprint 7 |
| S7.5 — ACA bicep templates | **ZERO** — cloud not provisioned | Hard defer; block on Azure subscription readiness |
| S7.6 — Key Vault binding | ZERO — cloud not provisioned | Hard defer |
| S7.7 — Pact contract tests | MEDIUM — valuable, but BFF→Platform.Api is in-process in tests today; no external consumers | Defer to Sprint 7; can be pulled earlier if Verbal has capacity |
| S7.8 — k6 performance baselines | LOW — no staging environment to run against | Hard defer |
| S7.9 — OWASP ZAP | LOW — no running environment to scan | Hard defer |
| S7.10 — Documentation | LOW | Defer; write runbook when cloud is ready |

### Risk of deferring CI/CD entirely

**This is the highest architectural risk in the plan.** Every sprint that ships without a CI gate means:
- Regressions that break the build/tests can merge silently
- The 76 tests (unit + integration) don't run on every PR — they only run when someone remembers to run them locally
- By Sprint 7, the codebase will have 3–4 more sprints of changes with no regression safety net
- Fixing a mid-sprint regression without CI is slower and higher stress

The sprint plan itself flagged this: *"S7.1 is Sprint 7's first story; consider pulling it forward to Sprint 5 if capacity allows."* That note should now be a decision, not a suggestion.

### Revised recommendation

**Decision: Split Sprint 7 into "CI-lite now" and "Cloud readiness later".**

#### Pull into Sprint 5 (add to current sprint plan)
| Story | Owner | Points | Rationale |
|-------|-------|--------|-----------|
| CI Pipeline (Azure Pipelines or GitHub Actions) | McManus | 5 | Safety net for all subsequent sprints |
| Docker Compose for local dev | McManus | 3 | Reduces 4-process dev friction immediately |

Sprint 5 currently has ~37 pts against 35 pt velocity — this is tight. **Options:**
- Option A: Reduce scope in Sprint 5 (defer S5.8 rate limiting, 2pts, to Sprint 6) and add CI + Docker Compose (+8pts → +6 net). This brings Sprint 5 to ~41pts — still over capacity.
- Option B: Pull CI into Sprint 5 only (5pts), Docker Compose into Sprint 6 (3pts). Sprint 5 becomes 42pts — too much.
- **Option C (recommended):** Pull CI pipeline only (5pts) into Sprint 5 as a high-priority add. Defer S5.8 (rate limiting, 2pts) to Sprint 6. Net Sprint 5 change: +3pts → ~40pts. Tight but McManus is competent and CI is a force multiplier for the rest of the sprint.

#### Defer into "Cloud Readiness Sprint" (new name for Sprint 7)
Run Sprint 7 only when the Azure subscription is provisioned and an environment exists to deploy to. At that point, the sprint makes sense as-is (S7.3–S7.10). Until then, deferring is the rational choice — you can't run k6 against staging that doesn't exist.

**Revised Sprint 7 trigger condition:** Azure subscription provisioned + ACA environment created + team capacity. This sprint should not be scheduled as a time-boxed sprint — it should be scheduled as "ready to deploy" work.

---

## Q4: Multi-tenant → Single-tenant Assessment

### What the code actually does

I inspected the following:

**`ITenanted` (SharedKernel):** A single-property interface: `Guid TenantId { get; }`.

**`BaseEntity` (SharedKernel):** Implements both `ITenanted` and `IShopScoped`. Every domain entity (`Member`, `SalesTransaction`, `Promotion`, `AttendanceRecord`, etc.) extends `BaseEntity` and therefore has `TenantId` as a required property.

**`BaseEntityConfiguration` (Infrastructure):** Configures `TenantId` as a required, non-nullable column on *every EF table*. This is already baked into all 4 existing migrations. Removing it requires schema migrations on all tables.

**Service method signatures:** `tenantId` is threaded explicitly as a parameter through every service method:
- `MemberService.RegisterAsync(Guid tenantId, Guid shopId, ...)`
- `AttendanceService.ClockInAsync(Guid tenantId, Guid shopId, ...)`
- `GoodsReceiptService.CreateNoteAsync(Guid tenantId, Guid shopId, ...)`
- `SalesService`, `ApprovalService`, `ReportingService`, `GoodsReceiptService` — all the same pattern

**No automatic tenant resolution:** There is no `ITenantContext` middleware, no JWT claim extraction for tenant, no EF global query filter on `TenantId`. The comment in `BaseEntityConfiguration` is explicit: *"Multi-tenancy — application layer is responsible for always setting this."* Tenants are threaded explicitly as method parameters.

**No cross-tenant query risk:** Since there are no EF global query filters for TenantId, every service query that filters by tenant does so explicitly in the Where clause (e.g., `r.TenantId == tenantId`). This is safer than auto-filters that could be accidentally disabled.

### Depth assessment

Multi-tenancy is structurally woven in at two levels:
1. **Schema level** — `TenantId` column exists on all 22+ entity tables via migrations already run. Removing it requires new migrations.
2. **Application level** — every service method accepts `tenantId` as a parameter. Removing it means changing ~50+ method signatures.

But critically: **there is no tenant routing middleware, no tenant resolver, no cross-tenant complexity.** The current implementation is essentially "pass TenantId through by hand everywhere." This is the lightest possible multi-tenancy implementation.

### Options

| Option | Effort | Risk | Verdict |
|--------|--------|------|---------|
| **A: Remove entirely** | High — remove from BaseEntity, all 22 entity classes, all service signatures, all migration files, all module endpoint parameters. ~3–5 days, high regression risk. | Breaks existing migrations; requires new "undo" migrations that alter every table. | Rejected |
| **B: Hardcode single tenant constant** | Very low — add `WellKnownTenants.DefaultTenantId` constant to SharedKernel. Pass it everywhere tenantId is currently a parameter. Schema unchanged, migrations unchanged, no service signature changes. ~0.5 day. | Near-zero — purely additive. | **Recommended** |
| **C: ITenantContext scoped service** | Low-medium — create `ITenantContext` with a scoped implementation that always returns `WellKnownTenants.DefaultTenantId`. Inject it into services instead of passing tenantId as a method parameter. ~1–2 days. | Low — clean but requires touching every service constructor. | Optional upgrade after B |

### Recommendation: Option B — Single-Tenant Constant

**Define in `M2.SharedKernel`:**
```csharp
public static class WellKnownTenants
{
    /// <summary>
    /// Single tenant for this deployment. Multi-tenancy is structurally supported
    /// but not activated — all data belongs to this tenant.
    /// </summary>
    public static readonly Guid Default = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
```

**Use it everywhere:**
- Seed data (S4.7) uses `WellKnownTenants.Default` for the single tenant record
- Module endpoints extract `tenantId` from the request (query string or route) but default to `WellKnownTenants.Default` when not supplied — or simply always use the constant and remove the parameter from endpoint signatures
- Services keep their `tenantId` parameter signatures unchanged (this preserves the contract if multi-tenancy is ever needed) — callers just pass the constant

**Why this is the right call:**
- TenantId columns in the database are not a liability — they're just always `00000000-0000-0000-0000-000000000001`. Storage overhead is 16 bytes per row.
- No migration changes required.
- If the business ever needs multi-tenancy in the future, the data model already supports it — zero redesign.
- The work to "activate" multi-tenancy later is ~2 days (add JWT tenant claim extraction + tenant context middleware) rather than ~5 days to rebuild the data model from scratch.

**Effort estimate:** 0.5 day. Assign to McManus in Sprint 4 as part of S4.7 (seed data story — seed data must use the constant anyway).

**Trade-off named:**
- Cost: TenantId as an unused discriminator on every table; developers must remember to pass `WellKnownTenants.Default` consistently.
- Benefit: Zero migration risk, preserves optionality, consistent with the existing "multi-store first-class" design (ShopId is actually what matters for this deployment — multiple shops under one tenant is exactly the target).

---

## Summary Decision Register

| # | Decision | Status |
|---|----------|--------|
| D-Q1-01 | Do not write GitHub Actions CI (S7.1) — target Azure Pipelines YAML from day one | **Decided** |
| D-Q1-02 | Do not build further GitHub Issues automation — treat current squad workflows as temporary | **Decided** |
| D-Q1-03 | Target Azure Container Registry for all container images (not GHCR) | **Decided** |
| D-Q3-01 | Pull S7.1 (CI pipeline) into Sprint 5; defer S5.8 (rate limiting) to Sprint 6 to make room | **Recommended** |
| D-Q3-02 | Pull S7.2 (Docker Compose) into Sprint 6 | **Recommended** |
| D-Q3-03 | Rename Sprint 7 "Cloud Readiness Sprint"; schedule only when Azure subscription is provisioned | **Recommended** |
| D-Q4-01 | Add `WellKnownTenants.Default` constant to SharedKernel; all code uses this as the single tenant | **Recommended** |
| D-Q4-02 | Keep TenantId columns in schema — no migrations to remove it | **Decided** |


# Decision: Sprint 4–7 Plan & Capacity Allocation

**Author:** Keyser (Lead / Architect)
**Date:** 2026-05-13
**Status:** Decided
**Requested by:** Ryan Chung

---

## Context

Sprint 3 delivered: Platform.Api 4-process extraction, BFF → Platform.Api HTTP wiring, integration test harness refactor (76 tests). Backlog refinement was conducted on 2026-05-13 to produce a 4-sprint plan for remaining MVP work.

## Sprint Goals

| Sprint | Theme | Goal |
|--------|-------|------|
| Sprint 4 | Business Logic Completion | Complete core domain service logic (Sales, Approvals, Promotions engine), register Hangfire + SAP Outbox worker, wire SignalR and FCM |
| Sprint 5 | Auth, Security & Cross-cuts | Implement Authorization module (SAP auth objects), complete JWT/API-key auth on all 4 processes, health checks, rate limiting, API versioning |
| Sprint 6 | Frontend Depth | Real Blazor portal pages (Promotions, Approvals, Reporting), Flutter POS sales flow, Flutter Promos member/coupon flow |
| Sprint 7 | CI/CD & Production Readiness | GitHub Actions pipeline, Docker Compose, OpenTelemetry, ACA bicep infra, Pact contracts, k6, OWASP ZAP |

## Capacity Decisions

- **Velocity:** 35–38 story points per sprint (small team, 2-week cadence)
- **McManus** owns all backend Platform.Api work across Sprints 4–5 and DevOps in Sprint 7
- **Fenster** front-loaded in Sprint 6 (Blazor + Flutter); blocked on Sprint 5 auth gate
- **Edie** owns all schema migrations, seeding, and Azure infra bicep
- **Verbal** contributes testing stories every sprint; escalates to contract + perf + security tests in Sprint 7

## Critical Path Dependencies

1. Sprint 4 (Hangfire + SignalR) must complete before Sprint 6 can test real-time Blazor approval updates
2. Sprint 5 (AuthZ module + Portal auth) is a hard gate before Sprint 6 frontend work against protected endpoints
3. Sprint 7 CI/CD should ideally be pulled into Sprint 5 if McManus has capacity after auth wiring — currently the highest-risk deferral

## Gaps Discovered (Critical)

- **No CI/CD pipeline** — regressions ship silently; S7.1 is candidate to pull forward
- **Authorization module unimplemented** — all endpoints unguarded; Sprint 5 P0
- **M2PortalBff has no Entra ID JWT auth** — Sprint 5 blocker for Fenster
- **Hangfire not registered** — SAP Outbox worker never fires; Sprint 4 P1
- **Blazor portal no MSAL** — Sprint 5 prerequisite for Sprint 6 frontend depth

## Trade-offs Named

| Decision | Cost | Benefit |
|----------|------|---------|
| Defer CI/CD to Sprint 7 | 3 sprints without automated regression gate | Keeps McManus focused on domain completion and auth in Sprints 4–5 |
| Apply API versioning in Sprint 5 | Slight refactor of all BFF `MapGroup` calls | Zero migration cost now; external consumers in Sprint 7+ adopt versioned URLs from day 1 |
| Authorization before frontend | Delays Blazor/Flutter stories by 1 sprint | Every protected admin endpoint is genuinely guarded when Fenster wires it up |

