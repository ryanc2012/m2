# M2 Sprint Plan — Sprints 4–7
> **Author:** Keyser (Lead / Architect)
> **Date:** 2026-05-13
> **Status:** Approved for execution

---

## Backlog Assessment (as of Sprint 3 completion)

### Done ✅
- Solution structure (8 projects): Domain, Infrastructure, SharedKernel, SapConnector, Platform.Api, 3 BFFs
- All 8 domain modules scaffolded: Members, Promotions, Sales, Attendance, GoodsReceipt, Approvals, Notifications, Reporting
- All 8 module service implementations (MemberService, SalesService, ApprovalService, etc.)
- All 8 InterModule typed HTTP clients (IMembersModuleClient, ISalesModuleClient, etc.) + `InterModuleServiceExtensions`
- All 8 module endpoint registrations on Platform.Api (`/modules/{name}/`)
- EF Core migrations through Sprint 4 schema (GoodsReceipt + SapOutbox)
- EF entity configurations for all domain entities (22 configs)
- Platform.Api extracted as independent process (:5100), X-Api-Key middleware
- BFFs converted to call Platform.Api via typed HTTP clients
- MekaPosBff: Entra ID JWT bearer wired (`AddMicrosoftIdentityWebApi`)
- BFF endpoint files: MekaPosBff (Sales, Attendance, GoodsReceipt), MekaPromosBff (Members, Coupons, NotificationHistory), M2PortalBff (Approvals, Promotions, Reporting, Members, Attendance, GoodsReceipt, Notifications)
- ADRs 001–022 finalized
- `ARCHITECTURE.md` and `DEV-SETUP.md`
- Integration test harness refactored for 4-process topology (76 tests: 55 unit + 21 integration)
- Blazor portal app scaffolded: App.razor, Index, Attendance, Promotions, Sales, Settings, MainLayout, NavMenu pages

### In Progress / Partial ⚠️
- Service implementations are scaffolded — business logic in some services needs completion (SAP outbox deferred in `GoodsReceiptService.PostToSapAsync`)
- Flutter apps partial: meka-pos has GoodsReceipt screen + print service only; meka-promos has Notifications screen only
- Blazor portal pages are stubs — no real data binding or component logic

### Backlog 📋 (planned, not started)
- Hangfire registration + SAP Outbox background worker
- SignalR NotificationHub (Blazor real-time)
- Firebase FCM push dispatch
- Authorization module (SAP auth object model + `IAuthorizationService` implementation)
- Entra ID wiring on M2PortalBff; API key validation on MekaPromosBff
- API key management CRUD endpoints + admin UI
- Database dev seed data
- Health check endpoints on all 4 BFFs
- Global exception handler middleware (RFC 9457 Problem Details)
- Rate limiting on MekaPromosBff (public-facing)
- Idempotency keys on Sales POST endpoints (BE-REC-001 R1)
- OpenTelemetry traces + metrics → Azure Monitor
- Docker Compose for local multi-process dev
- GitHub Actions CI/CD pipeline (build, test, security scan)
- Azure Container Apps bicep templates + Key Vault binding
- bUnit Blazor component tests
- Flutter widget + integration tests
- Contract tests (Pact) for BFF → Platform.Api
- k6 performance baselines
- OWASP ZAP security scan in CI

### Discovered Gaps 🔍
- **No CI/CD build pipeline** — zero GitHub Actions workflows for build/test. Highest risk: regressions ship silently.
- **No Docker Compose** — developers run 4 processes manually; onboarding friction.
- **Authorization module unimplemented** — `IAuthorizationService` is injected in BFFs (per architecture doc) but no SAP auth object data model or service implementation exists. Every protected endpoint is unguarded.
- **M2PortalBff has no Entra ID auth** — the BFF processes requests without validating manager JWTs (MekaPosBff is wired; Portal is not).
- **SAP Outbox worker never fires** — `GoodsReceiptService.PostToSapAsync` explicitly defers to outbox, but Hangfire is not registered anywhere in Platform.Api or Infrastructure.
- **Blazor portal has no MSAL/auth integration** — `RedirectToLogin.razor` exists but no `Microsoft.Identity.Web.UI` or MSAL wiring in the portal project.
- **No API versioning applied** — architecture doc mandates `/api/v1/` prefix; BFF endpoints lack it.

---

## Capacity Model
- Team: McManus (backend), Fenster (frontend/Blazor/Flutter), Edie (database/infra), Verbal (testing)
- Velocity: **35 story points per sprint** (2-week cadence, small team)
- Story sizes: 1 (trivial), 2 (small), 3 (medium), 5 (large), 8 (extra-large — split if possible)

---

## Sprint 4 — Business Logic & Cross-cutting Core
**Goal:** Complete the core domain service logic (Sales transactions, Promotions engine, Approval workflow), wire Hangfire for the SAP Outbox, and deliver SignalR + FCM so notifications work end-to-end.
**Duration:** 2 weeks

### Stories
| # | Story | Owner | Points | Notes |
|---|-------|-------|--------|-------|
| S4.1 | Implement Sales transaction service: create sale, void, return — full business rules, idempotency key validation (BE-REC-001 R1), line-item calculation | McManus | 5 | Idempotency key stored in `SalesTransactions` table; duplicate key returns 200 with original response |
| S4.2 | Implement Approval workflow state machine: submit, approve, reject, escalate — sequential position-based steps per ADR-014 | McManus | 5 | MediatR events on state change; `ApprovalStepConfiguration` already seeded in schema |
| S4.3 | Implement Promotions discount engine: eligibility check, discount formula evaluation, coupon pre-issuance batch (ADR-013) | McManus | 5 | `IDiscountEngine` internal to Promotions module; coupon batch triggered on promotion activation |
| S4.4 | Register Hangfire in Platform.Api; implement `SapOutboxWorker` (recurring job, 30s interval, Polly retry per ADR-006) | McManus | 3 | `GoodsReceiptService.PostToSapAsync` already writes to outbox; worker reads and posts via `ISapODataClient` |
| S4.5 | Wire SignalR `NotificationHub` in Platform.Api; connect `NotificationService.SendAsync` to hub dispatch | McManus | 3 | Hub path: `/hubs/notifications`; authenticate via X-Api-Key query param for SignalR WebSocket |
| S4.6 | Implement Firebase FCM dispatch in NotificationService for mobile push (meka-pos + meka-promos device tokens) | McManus | 3 | Use `FirebaseAdmin` SDK; `DeviceRegistration` table already has FCM token column |
| S4.7 | Add dev seed data: 1 tenant, 5 members, 3 promotions, sample products, 2 approval policies | Edie | 3 | `IHostedService` seed on startup in Development env only; idempotent (check before insert) |
| S4.8 | Add EF migration: Authorization module schema (AuthorizationRole, RoleAuthorizationObject, ObjectFieldValue, UserRoleAssignment) | Edie | 3 | See auth object model in ARCHITECTURE.md §6; time-bounded assignments (ValidFrom/ValidTo) |
| S4.9 | Unit tests: SalesService (transaction, void, return, idempotency), PromotionService (eligibility, discount calc, coupon batch) | Verbal | 5 | Target 100% line coverage on domain invariants; parameterized xUnit theories |
| S4.10 | Integration tests: Approval workflow end-to-end (submit → approve → approved state), SAP Outbox worker with mock SAP client | Verbal | 3 | Use `PlatformWebApplicationFactory`; assert state transitions in EF in-memory DB |

**Sprint Total: 38 pts**

### Exit Criteria
- [ ] A POS sale (create → void → return) round-trips via `POST /modules/sales/transactions` with idempotency
- [ ] An approval request progresses from `Pending → Approved` via two step approvals
- [ ] A promotion activation triggers coupon pre-issuance for eligible members
- [ ] Hangfire dashboard accessible at `/hangfire` in development; outbox worker processes a queued SAP entry
- [ ] SignalR hub accepts connections from authenticated clients; notification dispatched via `NotificationService` appears on hub
- [ ] Dev seed data loads on `dotnet run` in Development; tenant + members + promotions present
- [ ] Auth schema migration runs cleanly: `dotnet ef database update`
- [ ] All existing 76 tests green; ≥ 15 new unit + integration tests added

---

## Sprint 5 — Auth, Security & Infrastructure Cross-cuts
**Goal:** Lock down the authorization module, complete auth wiring on all 4 processes, harden the platform with health checks, rate limiting, global error handling, and establish the API versioning convention across all BFF endpoints.
**Duration:** 2 weeks

### Stories
| # | Story | Owner | Points | Notes |
|---|-------|-------|--------|-------|
| S5.1 | Implement Authorization module: `AuthorizationService` evaluates SAP auth objects against user role assignments; `IMemoryCache` with 5-minute TTL | McManus | 5 | See auth object model ARCHITECTURE.md §6; `CheckAsync(principal, authObject, fields)` API |
| S5.2 | Wire Entra ID JWT bearer on M2PortalBff (`AddMicrosoftIdentityWebApi`); add API key validation middleware on MekaPromosBff | McManus | 3 | Mirrors existing MekaPosBff pattern; `AzureAd` config section needed for Portal |
| S5.3 | Apply `[Authorize]` / auth object enforcement on all BFF endpoints using `IAuthorizationService`; document required auth objects per endpoint | McManus | 3 | Promotions write requires `M_PROMOTION_MANAGE`; Sales void requires `M_SALES_VOID`; etc. |
| S5.4 | Implement API key management CRUD: `POST/GET/DELETE /api/v1/apikeys` on Platform.Api; SHA-256 hash storage; plaintext shown once (BE-REC-001 R4) | McManus | 5 | Admin-only endpoint guarded by `M_APIKEY_MANAGE` auth object; key scopes as string array |
| S5.5 | Add global exception handler middleware (RFC 9457 Problem Details) to all 4 processes; replace any naked `throw` / 500 responses | McManus | 3 | Use `app.UseExceptionHandler`; `ProblemDetails` shape: type, title, status, detail, instance |
| S5.6 | Apply `/api/v1/` URL prefix to all BFF endpoints (ADR path versioning); update OpenAPI docs and InterModule client base paths | McManus | 3 | Non-breaking in this phase — no external consumers yet; update all BFF `MapGroup` calls |
| S5.7 | Health check endpoints on all 4 processes (`/health`, `/health/ready`, `/health/live`); include DB connectivity check on Platform.Api | Edie | 3 | Use `AddHealthChecks().AddNpgsql(...)`; BFFs check Platform.Api reachability |
| S5.8 | Rate limiting on MekaPromosBff: sliding window 60 req/min per IP; fixed window 10 req/min per API key on coupon endpoints | Edie | 2 | ASP.NET Core 9 built-in rate limiter; `RateLimiterOptions` in `AddRateLimiter` |
| S5.9 | Blazor portal auth: wire `Microsoft.Identity.Web.UI` + MSAL; `RedirectToLogin.razor` flows to Entra ID; protected routes via `AuthorizeView` | Fenster | 5 | `AddMicrosoftIdentityWebApp` in portal `Program.cs`; `CascadingAuthenticationState` in `App.razor` |
| S5.10 | Security + auth tests: unit tests for `AuthorizationService` (permit/deny by auth object), integration tests for 401/403 on all protected BFF endpoints, rate limit boundary test | Verbal | 5 | Use `TestAuthHandler` with role claims; test 401 (no token), 403 (wrong auth object), 200 (correct) |

**Sprint Total: 37 pts**

### Exit Criteria
- [ ] `AuthorizationService.CheckAsync` returns Permit/Deny based on user role assignments; cache TTL confirmed via unit test
- [ ] M2PortalBff rejects requests without valid Entra ID JWT (returns 401)
- [ ] MekaPromosBff rejects requests without valid X-Api-Key (returns 401)
- [ ] API key CRUD: create key → hash stored → plaintext returned once → key validates on subsequent requests
- [ ] All 4 processes return RFC 9457 Problem Details for unhandled exceptions (no raw stack traces)
- [ ] All BFF endpoints respond at `/api/v1/...`
- [ ] `/health/ready` on all 4 processes returns 200; DB check on Platform.Api confirms PostgreSQL reachable
- [ ] Rate limiter returns 429 after 60 requests/min from single IP on MekaPromosBff
- [ ] Blazor portal login flow completes: unauthenticated → redirect → Entra ID → return with claims
- [ ] ≥ 20 new security/auth tests; all existing tests green

---

## Sprint 6 — Frontend Depth: Blazor Portal + Flutter Apps
**Goal:** Transform stub Blazor pages into working admin screens (Promotions management, Approval workflow, Reporting), and bring Flutter meka-pos and meka-promos apps to a demo-able state with their core user journeys.
**Duration:** 2 weeks

### Stories
| # | Story | Owner | Points | Notes |
|---|-------|-------|--------|-------|
| S6.1 | Blazor Promotions page: list promotions, create/edit promotion form, activate/deactivate, approval trigger button | Fenster | 5 | Code-behind pattern (ADR-008); calls `IPromotionsModuleClient` via M2PortalBff; FluentValidation on form |
| S6.2 | Blazor Approvals page: pending approvals list, approve/reject action with comments, approval history log | Fenster | 5 | Real-time update via SignalR when approval state changes; `NotificationBell` component displays pending count |
| S6.3 | Blazor Reporting pages: Sales summary table (date range filter, export CSV), Attendance summary (staff + date) | Fenster | 3 | Call `IReportingModuleClient`; charts via lightweight JS interop (Chart.js); code-behind |
| S6.4 | Blazor `NotificationBell` component + SignalR client: real-time unread badge, dropdown panel, mark-as-read | Fenster | 3 | `HubConnection` in code-behind; `@implements IAsyncDisposable`; optimistic read-state (Fenster S4 decision) |
| S6.5 | Flutter meka-pos: Sales transaction flow (product scan/search → cart → checkout → receipt print via `PrintService`) | Fenster | 5 | Riverpod `CartNotifier`; calls `SalesEndpoints` on MekaPosBff; PrintService already scaffolded |
| S6.6 | Flutter meka-pos: Attendance clock-in/out screen with current status display and history list | Fenster | 3 | Riverpod provider; calls `AttendanceEndpoints`; staff photo from Entra ID token claims |
| S6.7 | Flutter meka-promos: Member registration + login (API key auth), profile screen | Fenster | 3 | MSAL Flutter or API-key flow; calls `MemberEndpoints` on MekaPromosBff; Riverpod `AuthNotifier` |
| S6.8 | Flutter meka-promos: Promotions browse, coupon detail with QR code display, coupon redemption status | Fenster | 3 | Calls `CouponEndpoints`; QR via `qr_flutter` package; signed JWT coupon (BE-REC-001 R5) |
| S6.9 | bUnit tests: Promotions page (render, form submit, validation errors), Approvals page (approve action, SignalR update) | Verbal | 3 | Mock `IPromotionsModuleClient` and `IApprovalsModuleClient`; assert rendered HTML |
| S6.10 | Flutter widget tests: meka-pos cart + checkout flow, meka-promos coupon display | Verbal | 3 | `flutter_test`; mock HTTP responses; golden tests for QR and receipt screens |

**Sprint Total: 36 pts**

### Exit Criteria
- [ ] Blazor portal: authenticated manager can create a promotion, submit for approval, see approval pending in real-time via SignalR
- [ ] Blazor portal: manager can approve/reject a pending approval; status updates without page refresh
- [ ] Blazor Reporting: Sales summary renders with date range filter; CSV export downloads correctly
- [ ] Flutter meka-pos: staff can scan products, complete a sale, and print a receipt
- [ ] Flutter meka-pos: staff can clock in and view their attendance status
- [ ] Flutter meka-promos: customer can register, view available promotions, display a coupon QR code
- [ ] ≥ 12 bUnit + Flutter widget tests added
- [ ] All existing tests remain green

---

## Sprint 7 — CI/CD, Observability & Production Readiness
**Goal:** Ship the CI/CD pipeline, containerize all 4 processes for production deployment, wire OpenTelemetry observability, and complete security/performance validation gates — making the platform deployable to Azure Container Apps.
**Duration:** 2 weeks

### Stories
| # | Story | Owner | Points | Notes |
|---|-------|-------|--------|-------|
| S7.1 | GitHub Actions CI pipeline: on PR → `dotnet build`, `dotnet test`, format check, OpenAPI diff check; on push to main → build & tag container images | McManus | 5 | `.github/workflows/ci.yml`; matrix build across 4 .NET projects; test results as PR check |
| S7.2 | Dockerfiles for all 4 .NET processes (multi-stage Alpine builds); Docker Compose for local dev (4 processes + PostgreSQL + Hangfire dashboard) | McManus | 3 | `docker-compose.yml` at repo root; named network; volume for PG data; `depends_on` health checks |
| S7.3 | OpenTelemetry: traces + metrics wired in all 4 processes → Azure Monitor / Application Insights OTLP exporter | McManus | 3 | `AddOpenTelemetry().WithTracing().WithMetrics()`; activity source per module; use `APPLICATIONINSIGHTS_CONNECTION_STRING` env var |
| S7.4 | Serilog production config: structured JSON → Azure Application Insights sink; correlation ID enricher; request logging filter (exclude `/health`) | Edie | 2 | `appsettings.Production.json` per process; `Serilog.Sinks.ApplicationInsights` package |
| S7.5 | Azure Container Apps bicep templates: 4 container apps, managed PostgreSQL Flexible Server, Key Vault, APIM Consumption tier, Managed Identity bindings | Edie | 8 | `infra/` directory; `main.bicep` + module files per resource; parameterized for dev/staging/prod |
| S7.6 | Azure Key Vault secret binding: all connection strings and API keys sourced from Key Vault via `AddAzureKeyVault` + Managed Identity (no secrets in appsettings.json in prod) | Edie | 3 | `KeyVaultName` app setting drives URI; `DefaultAzureCredential`; local dev retains user-secrets |
| S7.7 | Contract tests (Pact): MekaPosBff → Platform.Api for Sales and Attendance module contracts; MekaPromosBff → Platform.Api for Members and Coupons | Verbal | 5 | `PactNet` provider verification; consumer pact files published to Pact Broker in CI |
| S7.8 | k6 performance baselines: `POST /modules/sales/transactions` ≥ 100 req/s at p95 < 200ms; `GET /modules/members/{id}` p95 < 100ms | Verbal | 3 | `tests/perf/` scripts; baseline thresholds fail CI if exceeded; run nightly against staging |
| S7.9 | OWASP ZAP passive scan in CI on staging deployment; fail build on High-severity findings | Verbal | 3 | ZAP GitHub Action; scan MekaPromosBff (public-facing highest risk) and Platform.Api |
| S7.10 | Documentation completion: deployment runbook (`docs/DEPLOYMENT.md`), ADR-023 container strategy decision, update DEV-SETUP.md with Docker Compose instructions | McManus | 2 | Runbook covers ACA deploy, rollback, Key Vault rotation, Hangfire dashboard access |

**Sprint Total: 37 pts**

### Exit Criteria
- [ ] CI pipeline runs on every PR; failing tests or build errors block merge
- [ ] `docker-compose up` brings up all 4 processes + PostgreSQL; Swagger UI accessible on all BFF ports
- [ ] OpenTelemetry traces visible in Azure Application Insights with request-scoped correlation IDs
- [ ] `infra/main.bicep` deploys cleanly to a target subscription: 4 ACA containers, PostgreSQL, Key Vault, APIM
- [ ] No plaintext secrets in any `appsettings*.json` after Key Vault binding is applied
- [ ] Pact contract tests pass in CI: provider verification confirms Sales + Attendance + Members + Coupons contracts
- [ ] k6 baselines meet thresholds; results artifact published in CI
- [ ] OWASP ZAP reports zero High-severity findings on MekaPromosBff
- [ ] `docs/DEPLOYMENT.md` covers full deploy + rollback procedure

---

## Risks & Decision Register

| Risk | Severity | Mitigation |
|------|----------|------------|
| **Authorization module unguarded** — all endpoints accessible without auth object checks until S5.3 | 🔴 High | Treat S5.1–S5.3 as hard blockers; no Sprint 6 frontend stories until S5 auth gate is green |
| **No CI pipeline** — regressions ship silently across sprints 4–6 | 🔴 High | S7.1 is Sprint 7's first story; consider pulling it forward to Sprint 5 if capacity allows |
| **SAP outbox never fires** — GoodsReceipt data not reaching SAP | 🟠 Medium | S4.4 (Hangfire) is Sprint 4 P1; outbox worker must be validated against mock SAP before sprint end |
| **Blazor portal has no auth** — M2PortalBff unauthenticated until Sprint 5 | 🟠 Medium | Sprint 6 frontend work blocked on Sprint 5 Blazor auth (S5.9); Fenster cannot start S6.1 until S5.9 ships |
| **ECR integration deferred (ADR-009)** — print receipt depends on `PrintService` stub | 🟡 Low | Flutter POS receipt uses mock/PDF until post-MVP; `PrintService` interface exists and is mockable |
| **SMS gateway TBD (ADR-010)** — OTP flow cannot complete | 🟡 Low | `ISmsGateway` interface exists; any OTP-dependent member onboarding blocked until vendor selected |

---

## Team Assignment Summary

| Owner | Sprint 4 | Sprint 5 | Sprint 6 | Sprint 7 |
|-------|----------|----------|----------|----------|
| **McManus** | Sales, Approvals, Promotions engine, Hangfire, SignalR, FCM | AuthZ module, JWT/API-key wiring, auth enforcement, API keys CRUD, error handler, versioning | — | CI pipeline, Dockerfiles, OpenTelemetry, deployment docs |
| **Edie** | Dev seed data, AuthZ schema migration | Health checks, Rate limiting | — | Serilog prod config, ACA bicep, Key Vault binding |
| **Fenster** | — | Blazor MSAL auth | Blazor pages (Promotions, Approvals, Reporting, NotificationBell), Flutter POS + Promos | — |
| **Verbal** | Unit tests (Sales, Promotions), Integration tests (Approval, Outbox) | Security/auth tests | bUnit + Flutter widget tests | Contract tests (Pact), k6, OWASP ZAP |
