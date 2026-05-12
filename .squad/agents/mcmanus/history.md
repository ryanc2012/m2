# McManus — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Backend Dev
- **Joined:** 2026-05-11T02:37:43.912Z

## Learnings

<!-- Append learnings below -->
2026-05-12 — Sprint 3: Promotions, Sales, Attendance APIs delivered. Domain entities/interfaces for all three, in-memory stubs, BFF endpoints. Coupon pre-issuance, stackable flag, refund method enforcement, ECR deferred. dotnet build clean.

### 2026-05-12 — Sprint 2: Approval engine, Notification service, Member API delivered. See .squad/log/2026-05-12T142236Z-sprint2-complete.md.

### 2026-05-12 — Sprint 1: Backend Platform Foundation

**What was built:**
- Full `.NET 9` solution (`src/M2.sln`) with 9 projects: SharedKernel, Domain, Infrastructure, SapConnector, 3× BFF (MekaPOS, MekaPromos, M2Portal), and 2× test (Unit, Integration).
- SharedKernel: `ITenanted`, `IShopScoped`, `BaseEntity` (multi-tenancy + multi-store + soft-delete), `Result<T>`, `AppException`, `BilingualText` (EN/ZHT), `ApiKeyMiddleware` stub.
- All three BFFs: Minimal API bootstrap with Entra ID (`Microsoft.Identity.Web`), Serilog structured logging, Swashbuckle (dev only), CORS placeholder, `/health` endpoint.
- SapConnector: `ISapODataClient` + `ISapNcoClient` interfaces, `SapConnectorOptions`, no-op implementations, `SapConnectorServiceExtensions`.
- Infrastructure: `M2DbContext` (EF Core 9 + Npgsql), `IOutboxService` + no-op, `BaseEntityConfiguration` (EF base for all entities), `BilingualTextConfiguration` (owned entity mapping), initial migration stub.
- `dotnet build`: **0 errors, 0 warnings**.

**Key implementation decisions:**
- `BaseEntity` enforces both `TenantId` AND `ShopId` (not optional) — multi-store is baked in from byte zero.
- `BilingualText` is a `record` (value semantics) mapped as EF owned entity with `_en` / `_zht` column suffix convention.
- `ApiKeyMiddleware` is a pass-through stub — hash comparison logic deferred to Sprint 2.
- All SAP no-op implementations are `internal sealed` — consumers must depend on the interface.
- Outbox no-op returns `Task.CompletedTask` — Hangfire wiring deferred to Sprint 4 (Goods Receipt).

**Gotcha — pre-existing Infrastructure files:**
The repo already had `M2.Infrastructure` source files committed (`BaseEntityConfiguration.cs`, `BilingualTextConfiguration.cs`, Migrations, etc.) with correct `M2.SharedKernel` namespace. These were compatible with the new SharedKernel types and compiled cleanly. The initial build failed due to stale incremental build cache — a clean `--no-incremental` build succeeded with 0 errors.

**gitignore gap fixed:** Added standard `.NET` `bin/`, `obj/` exclusions to `.gitignore`. Previously these were tracked, causing noise in commits.



### 2026-05-12 — Backend Backlog Authoring

**Domain Insights:**
- The platform has a clear hub-and-spoke dependency structure: Epic 1 (Foundation) and Epic 9 (SAP Integration) are the critical path blockers for almost everything else. Nothing ships until auth and SAP connector are in place.
- The Promotion Discount Calculation Engine (Epic 5) is the most algorithmically complex feature; it must handle multiple concurrent promotion types, most-favorable selection logic, and coupon redemption state — all within a 200ms SLA during live POS transactions.
- The Approval Workflow Engine (Epic 2) must be designed as a generic, document-agnostic engine — the current consumer is Promotions, but the architecture should allow any document type to plug in via `document_type` + `document_id` contract.
- SAP-style authorization objects require careful upfront schema design: auth object name, field names, and valid value ranges must be defined as standing data before any permission checks can work. This is a hard dependency for Epic 1.
- Coupon issuance strategy (pre-issued on promotion activation vs. on-demand at first browse) has significant performance implications and needs a product decision before Epic 4/5 sprint planning.
- The Goods Receipt flow has a two-phase commit problem: local confirmation must be persisted before SAP posting, with a reliable retry/dead-letter queue to handle SAP unavailability.
- Attendance management is relatively simple but has an important edge case: staff who forget to clock out need a scheduled end-of-day detection job and manager notification.

**Dependencies Identified:**
- ECR vendor/SDK unknown — blocks Epic 6 detailed design.
- SMS gateway vendor unknown — blocks Epic 4 OTP implementation.
- SAP connectivity protocol (RFC/BAPI vs OData) and landscape access unknown — blocks Epic 9.
- SAP org data availability blocks Epic 2 (approval routing) and Epic 7 (attendance staff validation).

**Open Questions (carry forward to team):**
1. ECR vendor and integration protocol?
2. SMS gateway provider?
3. SAP connectivity: RFC/BAPI or OData/REST?
4. SAP auth object definitions: pre-specified by business or backend-designed?
5. Multi-store from day one or single-store MVP?
6. Coupon issuance trigger: pre-issued or on-demand?
7. Escalation target: SAP hierarchy or configurable per workflow step?
8. Data residency / sovereignty requirements?
9. Offline POS support required?
10. Return refund method: original payment only or store credit also supported?

### 2026-05-12 — Sprint 2: Backend APIs

**What was built:**

**Module 1 — Approval Workflow Engine (`M2.Domain/Approvals/`, `M2.Infrastructure/Approvals/`)**
- `ApprovalRequest`, `ApprovalStep`, `ApprovalPolicy` entities extending `BaseEntity` (multi-tenant, multi-store).
- `IApprovalService` (CreateRequest, ApproveStep, RejectStep, GetRequest, GetPendingForApprover).
- `IApprovalPolicyService` (GetPolicy, SetPolicy).
- Stub `ApprovalService`: in-memory dict, respects `ApprovalPolicy.Mode` (SapHcmHierarchy vs StepByStepPosition, ADR-014) and `MaxLevels` (N-level chain, ADR-021).
- `M2PortalBff` endpoints: `POST/GET /approvals/requests`, `GET /approvals/pending`, `POST /approvals/requests/{id}/approve|reject`.

**Module 2 — Notification Service (`M2.Domain/Notifications/`, `M2.Infrastructure/Notifications/`)**
- `NotificationTemplate`, `DeviceRegistration`, `NotificationLog` entities.
- `INotificationService` (SendPush, RegisterDevice, UnregisterDevice).
- `ISmsGateway` (ADR-010): no-op stub, provider-swappable without code changes.
- Stub `NotificationService`: logs dispatch intent to Serilog (no real FCM/APNs yet).
- `M2PortalBff` endpoints: `POST /notifications/devices`, `DELETE /notifications/devices/{id}`, `POST /notifications/send`.

**Module 3 — Member Management API (`M2.Domain/Members/`, `M2.Infrastructure/Members/`)**
- `Member` entity with `BilingualText FirstName/LastName` (ADR-022), `QrCode` (opaque ref, ADR-019), `MembershipTier` enum.
- `OtpRequest` POCO (not BaseEntity — transient verification record, 5-min TTL).
- `IMemberService` (Register, GetById, GetByQrCode, UpdateProfile, Deactivate).
- `IOtpService` (GenerateAsync, ValidateAsync).
- `M2PortalBff`: GET/PUT member admin endpoints.
- `MekaPromosBff`: full consumer endpoints (register, qr lookup, otp generate/validate, update).

**Infra / Config:**
- `M2DbContext`: all Sprint 2 DbSets added.
- `InfrastructureServiceExtensions`: all Sprint 2 services registered.
- Pre-existing EF configurations fixed: nav property refs updated, enum-to-string conversions added.
- `dotnet build`: **0 errors, 0 warnings**.

**Key implementation decisions:**
- Used `BilingualText` value object (established in Sprint 1) for all bilingual name/text fields — overrides task's flat En/Zht fields spec.
- `OtpRequest` intentionally does NOT extend `BaseEntity` — it's a transient verification code record with no tenancy/audit lifecycle.
- All stub services use static in-memory Dictionaries — EF DbContext wiring deferred to Sprint 3 migrations.
- `ApprovalService.RejectStepAsync` resets to `ApprovalStatus.Rejected` regardless of step level (policy check only on approve path).

**Gotcha — pre-existing domain stub files:**
The repo already had minimal entity stubs (`Member.cs`, `OtpRequest.cs`, `NotificationTemplate.cs`, `DeviceRegistration.cs`, `NotificationLog.cs`) committed from a prior session, plus full EF configuration files for all Sprint 2 entities. These configs referenced old navigation properties (`o.Member`, `s.ApprovalRequest`, `t.Logs`) that conflicted with updated entity design. Fixed all three configs (`OtpRequestConfiguration`, `ApprovalRequestConfiguration`, `NotificationLogConfiguration`) to use no-navigation FK mapping.



**From Keyser (Architecture) — resolves open questions:**
- **Q3 SAP connectivity answered:** ADR-006 confirms OData REST primary, NCo RFC/BAPI fallback. This unblocks Epic 9 design. SAP landscape credentials (DEV/QAS/PRD hostnames, client numbers) still needed from client.
- **Q4 SAP auth object schema:** ADR-004 positions `Platform.Authorization` as a backend-designed module. Backend team designs auth objects based on access control requirements. Confirm with business whether SAP team provides a pre-existing auth object catalogue to match.
- **Deployment confirmed:** Azure Container Apps (ACA) — Epic 9 integration tests will need ACA-accessible SAP connectivity or a stable mock/sandbox.

**From Edie (Database):**
- TenantId multi-tenancy is confirmed schema-wide. OQ-04 (multi-store) must be resolved before sprint planning — if `location_id` becomes first-class, Edie will need to add it to the schema baseline before table migrations are written.

**From Fenster (Frontend Dev):**
- Fenster's D3 (QR token) and BE-REC-001 R5 are aligned — signed JWTs with 5-minute TTL covers both backend coupon QR codes and the member-facing QR code in the Promos app. Confirm with Fenster that countdown timer design assumes 5-minute window.
- Fenster's D2 (ECR protocol) is the same as OQ-01 — both blocked until ECR vendor is confirmed by client.



### 2026-05-12 — Sprint 3: Promotions, Sales, and Attendance APIs

**What was built:**

**Module 1 — Promotions (`M2.Domain/Promotions/`, `M2.Infrastructure/Promotions/`)**
- `Promotion` entity: bilingual name, type, status, formulaJson, stackable flag, approvalRequestId, nav to `PromotionProduct` and `Coupon`.
- `Coupon` entity: code (unique), issuedAt, expiresAt, redeemedAt, isRedeemed; FK to Promotion and optional Member.
- `PromotionProduct` join entity: PromotionId, ProductId (SAP product code), DiscountValue.
- `CartItem` / `DiscountResult` models for discount calculation.
- `IPromotionService`: Create, GetById, Activate (triggers pre-issue), Pause, GetActiveForShop.
- `ICouponService`: Issue, Validate, Redeem, GetForMember.
- `IDiscountEngine`: CalculateAsync(cartItems, memberId, shopId) — stackable filter per ADR-020.
- `PromotionService` stub: in-memory dict; `ActivateAsync` calls `ICouponService.IssueAsync` for coupon pre-issue (ADR-013).
- `CouponService` stub: in-memory dict; validates expiry and already-redeemed guard before redeem.
- `DiscountEngine` stub: retrieves active stackable promotions; formula evaluation deferred to Sprint 4.

**Module 2 — Sales (`M2.Domain/Sales/`, `M2.Infrastructure/Sales/`)**
- `SalesTransaction` entity: MemberId (nullable — guest allowed), CashierId (SAP employee), TotalAmount/DiscountAmount computed from line items, PaymentMethod, Status, CompletedAt/VoidedAt.
- `SalesLineItem` entity: FK to transaction, ProductId/Name bilingual split, Qty, UnitPrice, DiscountAmount, LineTotal.
- `ReturnTransaction` entity: FK to original, Reason, RefundAmount, RefundMethod (enforced = original payment ADR-016), ProcessedAt.
- `ISalesService`: CreateTransaction, CompleteTransaction, VoidTransaction, GetById.
- `IReturnService`: InitiateReturn (validates status=Completed + enforces original payment method), CompleteReturn.
- `IEcrService`: interface stub only — ECR deferred post-MVP per ADR-009, no implementation.
- `SalesService` stub: AddLineItem auto-recalculates TotalAmount/DiscountAmount on transaction.
- `ReturnService` stub: fetches original tx, copies PaymentMethod as RefundMethod before creating return.

**Module 3 — Attendance (`M2.Domain/Attendance/`, `M2.Infrastructure/Attendance/`)**
- `AttendanceRecord` entity: EmployeeId (SAP), ClockInAt, ClockOutAt (nullable), Source enum (Manual/SapSync), Notes.
- `AttendanceSummary` value object (record): EmployeeId, Date, TotalHours, IsComplete.
- `IAttendanceService`: ClockIn, ClockOut, GetRecordsForEmployee, GetDailySummary.
- `AttendanceService` stub: guards against double clock-in; calculates TotalHours from complete records.

**Infra / BFF wiring:**
- `M2DbContext`: added DbSets for Promotion, Coupon, PromotionProduct, SalesTransaction, SalesLineItem, ReturnTransaction, AttendanceRecord.
- `InfrastructureServiceExtensions`: registered all 6 Sprint 3 services (ICouponService before IPromotionService — injection order matters).
- `M2PortalBff`: `PromotionEndpoints` + `AttendanceAdminEndpoints` mapped.
- `MekaPromosBff`: `CouponEndpoints` mapped (member coupons + POS coupon redeem).
- `MekaPosBff`: `SalesEndpoints` + `AttendanceEndpoints` mapped (clock-in/out staff-facing).
- `dotnet build`: **0 errors, 0 warnings**.

**Key implementation decisions:**
- `ICouponService` registered before `IPromotionService` in DI so `PromotionService(ICouponService, ...)` constructor injection resolves.
- `DiscountEngine` receives `IPromotionService` — uses empty Guid for tenant in stub (real tenant resolution deferred to EF wiring Sprint 4).
- `SalesTransaction.AddLineItem()` is the aggregate root for line item management; recalculation is internal.
- `ReturnTransaction.RefundMethod` is set from the original transaction at initiation time — not passed by caller — enforcing ADR-016 at the domain boundary.
- `IEcrService` has no implementation class at all (not even a no-op) — consumers must wait for ECR vendor selection.
- Clock-in guard in `AttendanceService`: returns `Result.Failure` if employee already has an open record in same tenant.

**Gotcha — pre-existing Sales enum stubs:**
The repo already had `ReturnStatus.cs` and `TransactionStatus.cs` committed from a prior stub session. These compiled cleanly alongside the new `SalesStatus.cs` and `PaymentMethod.cs` — no conflicts.
