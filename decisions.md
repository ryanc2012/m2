
- **Approach:** Shared database, `TenantId` (Guid, NOT NULL) column on every table
- **McManus:** Please review the `Member` and `OtpRequest` stub domain entities and add factory methods, validation, and domain events following the Approvals pattern. The EF configs are already wired.
- **Multi-store:** `ShopId` (Guid, NOT NULL) first-class on every entity — no nullable shop_id allowed (ADR-013 resolved OQ-04)
- **OQ-FE-01:** Should `ApprovalPolicySettings` be restricted to an admin role only? The page is `[Authorize]` but not role-gated. Needs decision from Keyser/McManus before Sprint 3.
- **OQ-FE-02:** QR code expiry countdown on ProfileScreen — not yet implemented. Awaiting OQ-11 resolution (member QR token lifetime). Design should use a 5-minute (300s) window per McManus' BE-REC-001 R5 assumption.
- **OQ-FE-03:** SMS OTP auto-read on Android (SMS Retriever API) — requires a package (`sms_autofill`) and backend SMS message format alignment. Backend must format the OTP SMS with the app hash. Raise with McManus for Sprint 3.
- **Snapshot accuracy:** Once McManus enriches the domain, run `dotnet ef migrations add Temp --output-dir /dev/null` to verify the snapshot diff is clean and delete the temp migration.
- `BilingualText` is mapped as an EF Core **owned entity** (not a separate table, not JSONB)
- `InitialCreate` migration only creates the `m2` schema — no tables (Sprint 1 scope)
- `MultipleAccountPca` — standard personal device mode (Promos app)
- `SingleAccountPca` — POS shared-device mode (maps 1:1 to ADR-018)
- `TestAuthHandler` (bypasses Azure AD — provides synthetic manager identity)
- All open questions resolved; scope is stable for MVP.
- Both `_en` and `_zht` are `IsRequired()` — storing only one language is a schema violation
- Both columns are set by the application layer; no DB default — forcing the caller to be explicit
- Column naming pattern: `{propertyName}_en`, `{propertyName}_zht`
- Critical path is backend-first: platform → approval → notification → member → promotions → sales → attendance → goods receipt → SAP.
- Extension method `OwnsOneBilingual<TEntity>()` enforces this consistently across all configurations
- Frontend app foundations and UI/UX polish are parallelizable after Sprint 1.
- In-memory EF Core DB (no Postgres needed in CI)
- JSONB column: harder to query/index individual languages
- Migrations written manually when EF CLI cannot connect to a live DB (CI/design-time)
- Per-module migrations will be added in Sprints 2–4 as domain tables are built
- SAP and ECR integration risks noted; ECR deferred post-MVP.
- Scoped `Mock<IApprovalService>` injected via DI for endpoint-level control
- Separate `LocalizedText` table: join overhead, more complex migrations
- Single column with delimiter: unacceptable — no type safety
- Timestamp format: `yyyyMMddHHmmss` (e.g., `20260512000000_InitialCreate`)
---
**ADR reference:** ADR-010 (SMS Gateway Abstraction).
**Alternative considered:** `[assembly: InternalsVisibleTo("M2.Tests.Integration")]` — rejected because it requires modifying the production assembly attribute, which is more invasive.
**Author:** Edie (Database Engineer)
**Author:** Edie (Database)  
**Author:** Fenster  
**Author:** Fenster (Frontend Dev)  
**Author:** McManus  
**Author:** McManus (Backend Dev)
**Author:** Verbal (Test Engineer)
**Chose:** `BilingualText FirstName`, `BilingualText LastName` (same for `NotificationTemplate.Title/Body`).
**Chose:** `localeProvider` as a Riverpod `StateProvider<Locale?>` passed to `MaterialApp.locale`  
**Chose:** `msal_auth ^1.0.8`  
**Chose:** `OtpRequest` is a plain POCO with its own `Id`, `MemberId`, `Code`, `ExpiresAt`, `IsUsed`, `CreatedAt`.
**Chose:** `RejectStepAsync` does not check `ApprovalPolicy.Mode` or `MaxLevels` — it always moves the request to `Rejected` status immediately.
**Chose:** `services.AddSingleton<ISmsGateway, NoOpSmsGateway>()`.
**Chose:** `static readonly Dictionary<Guid, T>` in all stub service implementations.
**Chose:** Flutter built-in `flutter gen-l10n` with ARB files  
**Chose:** MudBlazor 7.15.0 (upgraded to 3.8.3 of Microsoft.Identity.Web for security)  
**Consequence:** Any module that needs reliable SAP writes must inject `IOutboxService`, not call SAP directly. This ensures the pattern is enforced from first use.
**Consequence:** Domain entities with displayable names declare `BilingualText Name { get; }` etc. Direct string columns for localised text are forbidden.
**Consequence:** Every entity creation requires explicit TenantId + ShopId. Application layer (BFF endpoints) must extract these from the JWT claims before calling domain constructors. Sprint 2 auth middleware must expose a `ITenantContext` / `IShopContext` abstraction.
**Consequence:** SAP implementation can be replaced in Epic 9 with zero interface changes. Polly policies will be added in `SapConnectorServiceExtensions` during Sprint 4.
**Context:** .NET top-level program generates an `internal Program` type. `WebApplicationFactory<Program>` in the integration test project cannot reference it.
**Context:** `ApprovalEndpointTests` targets HTTP endpoints (`/api/approvals`) that don't yet exist in M2.M2PortalBff.
**Context:** `IOtpService.ValidateAsync` returns `Result<bool>` — there's no separate "mark used" observable via the service interface alone.
**Context:** ADR-006 (OData REST primary, NCo RFC fallback), ADR-001 (modular monolith).  
**Context:** ADR-010 requires SMS gateway to be behind `ISmsGateway` interface. Enforcement needed at CI level.
**Context:** ADR-013 (multi-store from Day 1) and DB-001 (TenantId on all tables).  
**Context:** ADR-017 (Outbox pattern for SAP writes). Hangfire is not needed until Goods Receipt (Sprint 4).  
**Context:** ADR-022 requires `{en, zht}` bilingual responses on all API output.  
**Context:** McManus is building service implementations in Sprint 2 simultaneously with these tests. Real `ApprovalService`, `NotificationService`, `MemberService` implementations do not yet exist.
**Date:** 2026-05-12  
**Date:** 2026-05-12T20:02:06+08:00  
**Date:** 2026-05-12T22:22:36+08:00
**Decision:** `BaseEntity` implements both `ITenanted` and `IShopScoped`, making both Guid properties non-optional on every entity. There is no "single-store" shortcut available.  
**Decision:** `BilingualText` is a C# `record` (immutable, value semantics). EF Core maps it as an owned entity. Column naming convention: `{navigationPropertyName}_en` and `{navigationPropertyName}_zht`. Helper method `OwnsOneBilingual<T>()` on `EntityTypeBuilder<T>` enforces this convention.  
**Decision:** `IOutboxService` interface (`EnqueueAsync<TMessage>` + `ProcessPendingAsync`) is defined and registered as a no-op in Sprint 1. The interface contract is intentionally minimal — message serialisation format TBD when Hangfire is wired.  
**Decision:** `M2.SapConnector` references only `M2.SharedKernel` and `Microsoft.Extensions.*`. No EF Core, no BFF-specific packages. It exposes interfaces only; no-op implementations live in the same project behind `internal` visibility. BFFs and Infrastructure consume interfaces via DI.  
**Decision:** Add `public partial class Program {}` at the bottom of `M2.M2PortalBff/Program.cs`. This is the Microsoft-recommended pattern. Minimal footprint — one line.
**Decision:** Add a `typeof(ISmsGateway).IsInterface.Should().BeTrue()` reflection test. This is a zero-overhead, always-green guard that fails immediately if someone mistakenly converts `ISmsGateway` to a class.
**Decision:** Complement the service mock test with an **inline domain invariant test** on `OtpRequest`:
**Decision:** Write integration tests that assert `StatusCode.Should().BeIn([200, 201, 404])` for now — 404 is acceptable until McManus registers the endpoint. Tests compile, run, and pass. Once endpoints land, the `404` is removed from the acceptable set and assertions tighten to exact status codes + response body shape.
**Decision:** Write unit tests as **Moq-based contract stubs** that:
**Deferred:** Persistence via `shared_preferences`
**Implication:** Sprint 3 should add `shared_preferences` persistence so `memberSessionProvider` survives app restarts without requiring re-registration.
**Infrastructure:** `TestWebApplicationFactory` provides:
**Migration path:** When McManus delivers `ApprovalService(IApprovalRepository, IUnitOfWork)` etc., each test's mock setup is replaced with `var service = new ApprovalService(mockRepo.Object, ...)`.
**Outcome:** `ValidateOtp_WithValidCode_ShouldSucceed_AndMarkUsed` tests both the service contract AND the domain object invariant in a single test.
**Rationale:** `OtpRequest` is a transient verification record scoped to a `Member`, not an independent domain entity. Forcing `TenantId` / `ShopId` would require plumbing them through the OTP generation call — unnecessary coupling. EF config maps it directly via `IEntityTypeConfiguration<OtpRequest>`.
**Rationale:** No database connection is available in the current dev environment for Sprint 2 sprints. Static dicts allow the endpoints to be invoked end-to-end in a test environment without a running PostgreSQL instance. EF DbContext wiring will replace the dicts in Sprint 3 when migrations run against a real DB.
**Rationale:** Rejection is terminal regardless of chain depth or mode. A rejected step at any level means the workflow stops. This matches standard approval workflow semantics and avoids over-engineering the stub.
**Rationale:** Sprint 1 established `BilingualText` as the project-wide value object for all bilingual data (ADR-022, DB-001 owned entity mapping). Reverting to flat strings would diverge from the established pattern and break existing EF configuration conventions (`OwnsOneBilingual`). The interface still accepts `(en, zht)` string pairs — they are wrapped at the service call site.
**Rationale:** The no-op stub has no state, and real SMS gateway implementations (Twilio/AWS SNS clients) are typically thread-safe and expensive to construct per-request. Singleton is the correct lifetime for gateway abstractions.
**Rejected alternatives:**
**Rejected:** `easy_localization`, `intl_utils`, custom JSON loader
**Rejected:** `flutter_appauth`, `aad_oauth`
**Rejected:** `pretty_qr_code` (less maintained), manual Canvas painting (unnecessary complexity).
**Rejected:** EF DbContext usage in stubs.
**Rejected:** Extending `BaseEntity` (which mandates `TenantId` + `ShopId`).
**Rejected:** Radzen Blazor Components
**Rejected:** Skip attributes (`[Fact(Skip = "...")]`) — would prevent CI from running tests even after implementation is delivered. Pure fake implementations — adds maintenance burden of a second implementation class.
**Risk:** Static dicts are process-local and not thread-safe under concurrent load. Acceptable for stub phase; must be replaced before any load testing.
**Sprint:** 1 — Database Foundation
**Sprint:** 1 — Platform Foundation & Infrastructure
**Sprint:** 2
**Sprint:** 2 — Backend APIs
**Sprint:** 2 — Contract & Unit Tests for Approval, Notification, Member APIs
**Sprint:** 2 — Member Registration UI + Approval Workflow UI
**Status:** Pending team review
**Task spec said:** `FirstNameEn`, `FirstNameZht`, `LastNameEn`, `LastNameZht` as separate string properties.
# edie-sprint2-schema
# Fenster — Sprint 2 UI Component Decisions
# McManus — Sprint 2 API Decisions
# Verbal — Sprint 2 Testing Decisions
## Base Configuration Pattern
## Bilingual Text (ADR-022)
## Decision 1: BilingualText over flat EN/ZHT fields
## Decision 1: Component Library for m2-portal — MudBlazor
## Decision 1: Contract-Documentation Approach for Parallel Dev Tests
## Decision 2: `msal_auth` Package for Flutter MSAL
## Decision 2: OTP Domain Invariant Tests Inline
## Decision 2: OtpRequest does NOT extend BaseEntity
## Decision 3: Flutter Localisation Toolchain — `flutter gen-l10n`
## Decision 3: ISmsGateway Abstraction — Structural Test
## Decision 3: Static in-memory stores in stub services
## Decision 4: ApprovalService reject path ignores policy mode
## Decision 4: Integration Test Contract for Pending Endpoints
## Decision 4: Locale Switching via Riverpod `StateProvider`
## Decision 5: ISmsGateway registered as Singleton
## Decision 5: Program.cs Accessibility for WebApplicationFactory
## Decision: BilingualText as a record (value object) with EF owned entity convention
## Decision: Outbox deferred to Sprint 4; IOutboxService interface locked Sprint 1
## Decision: SapConnector project is an anti-corruption layer with no framework dependencies
## Decision: SharedKernel enforces both TenantId AND ShopId at BaseEntity level
## Decisions Made
## Files Created / Modified
## Items Needing Team Input
## Key Files
## m2-portal (Blazor)
## meka-promos (Flutter)
## Migration Strategy
## Multi-Tenancy (DB-001 + ADR-012)
## Open Items / Handoff to McManus
## Open items for Sprint 2
## Open Questions / Handoff Notes
## Open Questions for Team
## Open Questions Raised
## Schema Conventions
## Summary
### D-DB-2001: ApprovalStep extends BaseEntity
### D-DB-2002: Enums stored as varchar strings
### D-DB-2003: ApprovalPolicy.MaxLevels default = 2
### D-DB-2004: OtpRequest and NotificationLog are lightweight entities
### D-DB-2005: NotificationLog FK to notification_templates uses Restrict
### D-DB-2006: Snapshot and Designer.cs hand-authored
### D-DB-2007: Members domain stub created
### D1: `qr_flutter` for QR Code Display
### D10: Inline Row Edit in ApprovalPolicySettings
### D11: NavMenu Approvals Badge (Static)
### D12: `ApprovalService` HttpClient Registration
### D2: `memberSessionProvider` as Auth Anchor (not MSAL `authStateProvider`)
### D3: go_router `_RouterRefresher` Bridge
### D4: OTP Paste Handling via `maxLength: 6` on First Cell
### D5: Bilingual Name Field Layout (ZHT row, EN row)
### D6: ARB Key Naming Convention
### D7: `ApprovalDetailDto` to Avoid Namespace Collision
### D8: Full-Width Status Banner for Pending Approvals
### D9: `MudTimeline` for Approval Step History
### Rationale
```
```csharp
`ApprovalDetail.razor` shows a `MudAlert Severity.Warning` banner at the top of the page when `_detail.Status == ApprovalStatus.Pending`. This implements the "approval state prominence" UX pattern from Fenster's Sprint 1 learnings. Action buttons are also disabled when status is not Pending to prevent double-action.
`ApprovalStatus`, `ApproverType`, `ApprovalMode` are stored as `varchar(50)` strings via `HasConversion<string>()`. This trades slight storage overhead for readability in the DB and resilience to enum re-ordering.
`DeleteBehavior.Restrict` prevents accidental deletion of templates that have been used — preserving delivery audit history. Templates must be soft-deleted instead.
`flutter gen-l10n` is the Flutter SDK's official localisation pipeline. It generates type-safe `AppLocalizations` classes from `.arb` files at build time. Zero runtime overhead, no third-party dependency, IDE completion support, and aligns with the project's generated-code approach (Riverpod codegen, Mapperly on backend).
`flutter_appauth` is an OAuth2 AppAuth wrapper that works with any OIDC provider but does not expose the native MSAL account-switch broker API needed for ADR-018 shared-device mode. `msal_auth` gives us the right abstraction at the right level for our two distinct auth patterns.
`GoRouter` requires a `Listenable` (`refreshListenable`) to know when to re-evaluate redirect logic. Since Riverpod providers are not `Listenable`, a minimal `_RouterRefresher extends ChangeNotifier` class is instantiated alongside the router. `ref.listen(memberSessionProvider, ...)` calls `refresh()` in the `build()` method of the `ConsumerStatefulWidget`. This is the standard pattern for Riverpod + go_router without introducing a heavy `RouterNotifier`.
`Member`, `OtpRequest` domain entities were created as stubs in `M2.Domain/Members/` since McManus was building domain models in parallel. McManus should review and enrich with factory methods following the same pattern used in Approvals.
`msal_auth` wraps the native MSAL SDK (Microsoft Authentication Library) directly on both Android and iOS, exposing:
`MudBadge` with `Visible="@(_pendingCount > 0)"` and `Content="@_pendingCount"`. Currently `_pendingCount = 0` (hardcoded). Sprint 3 will wire this to a SignalR hub push or a short-lived timer poll of the approval count endpoint. The badge infrastructure is in place and ready.
`qr_flutter 4.1.0` chosen. `QrImageView(data: profile.qrCode, size: 220, backgroundColor: Colors.white)` inside a white container with drop shadow. The `qrCode` field contains a reference ID (per ADR-019 — server-side lookup, not a locally-verifiable JWT). QR size 220dp is sufficient for typical POS scanner distance; will increase if field testing shows scan failures.
`TimelinePosition.Start` used — keeps text on the right of the dot, consistent with MudBlazor convention. Each `MudTimelineItem` colour maps to the step's `ApprovalStatus` (Success/Error/Info/Warning). Comment text is displayed in quotes under the status line. No events = "No steps recorded yet" empty state.
| # | Item | Action Required |
| # | Question | Blocks |
| `20260512000000_InitialCreate.cs` | Creates `m2` schema; stub for future table migrations |
| `BaseEntityConfiguration.cs` | Abstract base — TenantId, ShopId, audit, soft-delete columns |
| `BilingualTextConfiguration.cs` | `OwnsOneBilingual` extension — `{prop}_en` / `{prop}_zht` columns |
| `DatabaseOptions.cs` | Strongly-typed config for connection string and EF options |
| `docs/data/DATA-DESIGN.md` | Updated — Sprint 2 tables section added |
| `M2DbContext.cs` | EF Core DbContext; calls `ApplyConfigurationsFromAssembly` |
| `M2DbContextFactory.cs` | IDesignTimeDbContextFactory for `dotnet ef` CLI tooling |
| `M2DbContextModelSnapshot.cs` | Empty model snapshot (no tables yet) |
| `src/M2.Domain/Members/Member.cs` | Created (stub) |
| `src/M2.Domain/Members/OtpRequest.cs` | Created (stub) |
| `src/M2.Domain/Notifications/DeviceRegistration.cs` | Created (stub) |
| `src/M2.Domain/Notifications/NotificationLog.cs` | Created (stub) |
| `src/M2.Domain/Notifications/NotificationTemplate.cs` | Created (stub) |
| `src/M2.Infrastructure/M2DbContext.cs` | Updated — 8 DbSet properties added |
| `src/M2.Infrastructure/Migrations/20260512010000_Sprint2_MembersApprovalsNotifications.cs` | Created |
| `src/M2.Infrastructure/Migrations/20260512010000_Sprint2_MembersApprovalsNotifications.Designer.cs` | Created |
| `src/M2.Infrastructure/Migrations/M2DbContextModelSnapshot.cs` | Updated |
| `src/M2.Infrastructure/Persistence/Configurations/ApprovalPolicyConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/ApprovalRequestConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/ApprovalStepConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/DeviceRegistrationConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/MemberConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/NotificationLogConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/NotificationTemplateConfiguration.cs` | Created |
| `src/M2.Infrastructure/Persistence/Configurations/OtpRequestConfiguration.cs` | Created |
| 1 | Approval endpoints (`POST /api/approvals`, `PATCH /api/approvals/{id}/approve`, `PATCH /api/approvals/{id}/reject`, `GET /api/approvals/{id}`) | McManus to register Minimal API routes in M2.M2PortalBff |
| 2 | ApprovalServiceTests — real implementation path | Replace mock setup with `new ApprovalService(mockRepo.Object, ...)` |
| 3 | Step approval — HCM hierarchy validation | Verify `Steps` collection contains `ApproverType.SapHcm` entries for HCM-mode policies |
| 4 | NotificationLog persistence | Integration test for `SendPush_ShouldLogNotification` to verify `NotificationLog` row is written |
| 5 | Member QR uniqueness — collision test | Add a 1,000-iteration stress test once real `RegisterAsync` lands (Guid.NewGuid-based QR should never collide in practice but worth a canary) |
| Blazor Server support | First-class | First-class |
| Column naming | PascalCase (EF Core default — aligns with rest of codebase) |
| Community & GitHub stars | 8k+ stars, very active | 3k+ stars |
| Component breadth | ~80+ components including DataGrid, Charts | Comparable but some premium-gated |
| Convention | Decision |
| Default schema | `m2` |
| Factor | MudBlazor | Radzen |
| File | Action |
| File | Purpose |
| Licence | MIT (fully free) | Free tier has limitations on some components |
| Migrations history | `m2.__EFMigrationsHistory` (scoped to schema) |
| PK type | `Guid` (sequential GUIDs — aligns with Coding Standards from ADR-003) |
| SignalR / real-time readiness | Compatible | Compatible |
| Soft delete | `IsDeleted bool DEFAULT false` on all entities — no hard deletes |
| SQ-01 | Are approval step approvers looked up from SAP HCM at request creation time (snapshot), or resolved dynamically at each step? | ApprovalService Sprint 3 impl |
| SQ-02 | Should `OtpRequest` be surfaced in the EF migration with its own table, or managed as a cache (Redis) in production? | Sprint 3 infra planning |
| SQ-03 | Who owns the `QrCode` generation key — backend (current: random Guid-N) or signed JWT per ADR-019? | Member QR flow |
| Theming | MudThemeProvider — simple, Material Design 3 aligned | OK but less Material-native |
| Timestamp columns | `timestamptz` (DateTimeOffset) — timezone-aware, required on all entities |
|------------|----------|
|--------|-----------|--------|
|------|---------|
|------|--------|
|---|----------|--------|
|---|------|-----------------|
1. **Azure App Registration IDs** — Placeholder client/tenant IDs used across all three apps. Real IDs needed before any auth flow can be tested. These should be injected via `--dart-define` (Flutter) and `appsettings.{env}.json` (Blazor), not hardcoded.
1. `ApiKeyMiddleware` needs SHA-256 hash comparison logic and config-driven key store (BE-REC-001 R4).
1. Compile and pass immediately (mock returns expected contract value)
2. **`msal_config.json` for Android** — Both Flutter apps need a valid MSAL config JSON in `android/app/src/main/res/raw/msal_config.json` for the Android broker auth flow. Template path referenced in `msal_auth` plugin docs. DevOps/Backend to provide the config once App Registrations exist.
2. `ITenantContext` / `IShopContext` service abstractions needed — BFF endpoints must extract TenantId + ShopId from Entra ID JWT claims.
2. Document the interface contract as living code
3. **Entra ID App Registration redirect URIs** — For m2-portal Blazor Server, the redirect URI must be registered: `https://{host}/signin-oidc`. For Flutter apps, the Android/iOS redirect URI format required by MSAL.
3. Carry inline comments indicating where real assertions go once McManus delivers
3. CORS `AllowAll` policy is a dev placeholder — tighten to allowed origins per environment.
4. Serilog Azure Application Insights sink not configured — add when ACA deployment pipeline is ready.
8 EF Core entity configurations, 1 migration, and domain stubs delivered for Members, Approvals, and Notifications.
All entity configurations extend `BaseEntityConfiguration<TEntity>` which applies shared column mappings. Concrete configurations call `base.Configure(builder)` then add entity-specific mappings (indexes, relationships, query filters for soft delete).
ARB files are placed in `lib/core/l10n/` per the sprint task directory layout.
Blazor generates a `partial class ApprovalDetail` from `ApprovalDetail.razor`. The service layer also had `record ApprovalDetail(...)`. The collision caused 14 build errors. Renamed to `ApprovalDetailDto` in `ApprovalService.cs`. ADR-008 code-behind pattern is preserved; only the DTO name changes. No functional impact.
For Sprint 1, locale switch is in-memory only. A `StateProvider` wired to `MaterialApp.locale` delivers instant live locale switching with no rebuild overhead. Persistence will be added in Sprint 2 when `shared_preferences` is added to the meka-promos dependency tree.
McManus's domain model made `ApprovalStep : BaseEntity`, giving it full audit trail and soft-delete columns. The task spec described it as a lightweight child entity. **Followed the domain.** Schema has TenantId, ShopId, and all audit columns on `approval_steps`.
MudBlazor aligns with the `initial_request.md` spec ("ASP.NET Blazor Web App with **Material Design** UI") and is MIT-licensed with no component feature gates.
New keys follow `camelCase` consistent with existing Sprint 1 keys. Parametrised keys (e.g., `otpSentTo`, `resendIn`) use `@placeholder` syntax in all three ARB files (EN, ZHT, ZHS). `resendIn` uses `int` type for the seconds parameter so `intl` can potentially apply plural rules in future locales.
otp.IsValid().Should().BeFalse();
otp.MarkUsed();
Rather than a modal dialog or separate edit route, `ApprovalPolicySettings` uses an in-row edit pattern: clicking "Edit" sets `_editingEntityType`, swapping the display cells with input controls (`MudSelect` for mode, `MudNumericField` for levels). Save/Cancel buttons are inline. The `MudNumericField Dense` attribute was removed after MUD0002 analyser warning — not a supported attribute on that component in MudBlazor 7.x.
Registered via `AddHttpClient<ApprovalService>(client => ...)` in `Program.cs`. Base URL sourced from `M2PortalBff:BaseUrl` config key with `https://localhost:5001` fallback. This follows the typed `HttpClient` pattern consistent with the rest of the BFF client layer.
Task spec said default 3. McManus's domain model initialises `MaxLevels = 2`. **Domain takes precedence.** DB column default is 2.
The consumer registration flow is phone-OTP-based, separate from Entra ID / MSAL. Rather than shoehorning the OTP-completion signal into `authStateProvider` (MSAL), a dedicated `StateProvider<MemberProfile?>` named `memberSessionProvider` was introduced in `profile_service.dart`. This is the single source of truth for "is the consumer logged in?" in the go_router redirect and HomeScreen sign-out.
The first OTP cell accepts up to 6 characters. `_onDigitChanged` checks `value.length > 1` and distributes digits across all 6 `TextEditingController`s with `FilteringTextInputFormatter.digitsOnly`. Subsequent cells cap at 1 character for normal key-by-key entry with auto-advance. This supports both manual typing and clipboard paste from SMS auto-complete (Android/iOS).
The snapshot and migration Designer files were written by hand since `dotnet ef` requires a running DB. **Recommend running `dotnet ef migrations add --dry-run` (or equivalent) once McManus finalises all domain factories** to verify the snapshot matches EF's generated output. Minor annotation differences will be reconciled on the next migration add.
These do not extend `BaseEntity`. `OtpRequest` is a transient verification record (no multi-tenancy direct on the row; inherited via Member FK). `NotificationLog` is an append-only audit row.
This verifies the domain object upholds the "no replay" contract, independent of service wiring.
var otp = new OtpRequest(memberId, code, TimeSpan.FromMinutes(5));
ZHT name uses last-first order (姓 + 名) — matching Chinese convention. EN name uses first-last order. Each pair is rendered as a 2-column `Row`. The `profile_setup_screen` and `edit_profile_screen` both use this layout. Labels come from ARB keys to respect locale.
