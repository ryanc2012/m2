# Edie — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Database
- **Joined:** 2026-05-11T02:37:43.916Z

## Learnings

<!-- Append learnings below -->
2026-05-15 — Docs Standards: Authored `docs/standards/database.md` — the database design documentation standard. Key decisions formalised: Mermaid inline ERDs required for new domains (≥3 tables) and cross-domain relationships; cross-module FK refs must explicitly note `(no DB constraint — ADR-001)` rather than being omitted; migration `Down()` safety must be stated in a class comment; audit columns may be abbreviated after first full occurrence; all seed data documented under `## Seed / Demo Data` in DATA-DESIGN.md. Decision filed to `.squad/decisions/inbox/edie-db-doc-standard.md`.
2026-05-13 — Sprint 4 S4.7+S4.8: Delivered WellKnownTenants (SharedKernel), DevSeedService (Infrastructure/Seed, IHostedService, idempotent, Development-only), 4 auth domain entities (Authorization/), 4 EF configurations, and migration 20260513125005_Sprint4_AuthSchema. Auth entities inherit BaseEntity with ShopId=Guid.Empty for tenant-wide roles. UserRoleAssignment has ValidFrom/ValidTo time-bounding. EF tooling required Microsoft.EntityFrameworkCore.Design on startup project — added to M2.Platform.Api. McManus must register DevSeedService in InfrastructureServiceExtensions.cs.
2026-05-12 — Sprint 4: GoodsReceiptNote, GoodsReceiptLineItem, SapOutboxEntry EF configs, migration, model snapshot, DATA-DESIGN.md updated. Project feature-complete for UAT.
2026-05-12 — Sprint 3: Delivered EF Core configs, migration 20260512020000_Sprint3_PromotionsSalesAttendance, updated DbContext and DATA-DESIGN.md for Promotions, Sales, Attendance. Domain model followed over spec; enums as strings; cross-module FKs no DB constraint. Build clean.

2026-05-12 — Sprint 2: 8 EF Core entity configs, migration 20260512010000, DbContext, DATA-DESIGN.md updated. See .squad/log/2026-05-12T142236Z-sprint2-complete.md.
2026-05-12: Chose PostgreSQL for its enterprise features, cost, and .NET support. Adopted shared DB with TenantId column for multi-tenancy. Key tables include audit columns, soft delete, and strategic indexes.

2026-05-12 — Sprint 2: Delivered EF Core entity configurations and migration for Members, Approvals, and Notifications domains. Key decisions: `ApprovalStep` extends `BaseEntity` (McManus added full audit trail — followed domain); enums (`ApprovalStatus`, `ApproverType`, `ApprovalMode`) stored as varchar strings via `HasConversion<string>()`; `ApprovalPolicy.MaxLevels` default is 2 (domain's value, not the task spec's 3); `OtpRequest` and `NotificationLog` are lightweight entities (no BaseEntity — no TenantId/ShopId); `NotificationLog` uses `Restrict` delete on template FK to preserve audit history; all `BilingualText` owned types use `OwnsOneBilingual` extension from Sprint 1 generating `{prop}_en`/`{prop}_zht` columns; snapshot and Designer.cs written by hand — recommend regenerating with `dotnet ef` once McManus finalises all domain factories.

2026-05-12 — Sprint 3: Delivered EF Core configurations, migration `20260512020000_Sprint3_PromotionsSalesAttendance`, and snapshot update for Promotions, Sales, and Attendance domains. Key decisions: domain entities were pre-authored by McManus with richer models than the spec — followed actual domain code throughout; all enum properties (`PromotionType`, `PromotionStatus`, `PaymentMethod`, `SalesStatus`, `AttendanceSource`) stored as varchar(50) via `HasConversion<string>()`; `SalesLineItem` and `ReturnTransaction` extend `BaseEntity` (McManus's choice — full audit trail on line items); `SalesLineItem` uses plain string properties `ProductNameEn`/`ProductNameZht` mapped to `ProductName_en`/`ProductName_zht` columns (not BilingualText owned type — product name is a snapshot, not a live bilingual entity); `PromotionProduct` adds `DiscountValue numeric(18,2)` per-product override (beyond spec, exists in domain); cross-module FKs (`coupons.MemberId`, `sales_transactions.MemberId`, `promotions.ApprovalRequestId`) stored as plain columns without DB-level FK constraints per ADR-001 (no cross-module navigation); `ReturnTransaction.IsComplete` bool + nullable `ProcessedAt` (domain pattern, spec only had processed_at); `Down()` drops in strict reverse FK order; snapshot written by hand — recommend regenerating with `dotnet ef` post-Sprint.


### 2026-05-12 — Cross-Agent Context (from Initial Planning Session)

**From Keyser (Architecture):**
- ADR-003 formally confirmed PostgreSQL 16 as primary with SQL Server as an acceptable org-mandated contingency. EF Core 9 is the ORM; the codebase is database-agnostic by design. Schema conventions (sequential GUIDs as PKs, PascalCase table/column names) are in the Coding Standards — align DATA-DESIGN.md to match.
- ADR-001 module structure: each module has its own EF Core DbContext or ModelBuilder partition. No cross-module entity navigation. This affects migration strategy — per-module migrations may need to be coordinated.

**From McManus (Backend Dev):**
- OQ-04 (multi-store vs single-store MVP) is unresolved. If multi-store is required, `location_id` must be first-class in schema from the start — retrofitting it later is expensive. Recommend flagging this to the coordinator for early stakeholder resolution.
- OQ-09 (data residency / sovereignty) affects Azure region for PostgreSQL Flexible Server. Should be confirmed before infrastructure provisioning.

**From Verbal (Tester):**
- All test environments must support data reset and isolation. EF Core migrations and test data seeding strategy should be designed with this in mind — idempotent seed scripts, not manual SQL inserts.
