# Edie — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Database
- **Joined:** 2026-05-11T02:37:43.916Z

## Learnings

<!-- Append learnings below -->
2026-05-12: Chose PostgreSQL for its enterprise features, cost, and .NET support. Adopted shared DB with TenantId column for multi-tenancy. Key tables include audit columns, soft delete, and strategic indexes.

2026-05-12 — Sprint 2: Delivered EF Core entity configurations and migration for Members, Approvals, and Notifications domains. Key decisions: `ApprovalStep` extends `BaseEntity` (McManus added full audit trail — followed domain); enums (`ApprovalStatus`, `ApproverType`, `ApprovalMode`) stored as varchar strings via `HasConversion<string>()`; `ApprovalPolicy.MaxLevels` default is 2 (domain's value, not the task spec's 3); `OtpRequest` and `NotificationLog` are lightweight entities (no BaseEntity — no TenantId/ShopId); `NotificationLog` uses `Restrict` delete on template FK to preserve audit history; all `BilingualText` owned types use `OwnsOneBilingual` extension from Sprint 1 generating `{prop}_en`/`{prop}_zht` columns; snapshot and Designer.cs written by hand — recommend regenerating with `dotnet ef` once McManus finalises all domain factories.


### 2026-05-12 — Cross-Agent Context (from Initial Planning Session)

**From Keyser (Architecture):**
- ADR-003 formally confirmed PostgreSQL 16 as primary with SQL Server as an acceptable org-mandated contingency. EF Core 9 is the ORM; the codebase is database-agnostic by design. Schema conventions (sequential GUIDs as PKs, PascalCase table/column names) are in the Coding Standards — align DATA-DESIGN.md to match.
- ADR-001 module structure: each module has its own EF Core DbContext or ModelBuilder partition. No cross-module entity navigation. This affects migration strategy — per-module migrations may need to be coordinated.

**From McManus (Backend Dev):**
- OQ-04 (multi-store vs single-store MVP) is unresolved. If multi-store is required, `location_id` must be first-class in schema from the start — retrofitting it later is expensive. Recommend flagging this to the coordinator for early stakeholder resolution.
- OQ-09 (data residency / sovereignty) affects Azure region for PostgreSQL Flexible Server. Should be confirmed before infrastructure provisioning.

**From Verbal (Tester):**
- All test environments must support data reset and isolation. EF Core migrations and test data seeding strategy should be designed with this in mind — idempotent seed scripts, not manual SQL inserts.
