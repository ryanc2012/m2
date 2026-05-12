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
