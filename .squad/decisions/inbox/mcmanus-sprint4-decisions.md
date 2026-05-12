# McManus — Sprint 4 Decisions

**Date:** 2026-12-05  
**Author:** McManus  
**Sprint:** 4

---

## Decision 1: GoodsReceiptStatus Enum — Pending/Confirmed/Discrepancy

Previous files used `Draft/Received/Confirmed`. Updated to `Pending/Confirmed/Discrepancy` per Sprint 4 spec. `MarkReceived()` retained on `GoodsReceiptNote` as a state-transition helper (sets `ReceivedAt` without changing status enum) to preserve staff workflow.

**Impact:** Any existing migration that referenced `Draft` or `Received` enum values will need updating before production deployment.

---

## Decision 2: SapODataClient — IConfiguration Key `Sap:ODataBaseUrl`

`SapODataClient` reads `Sap:ODataBaseUrl` directly from `IConfiguration` as specified. This coexists with `SapConnectorOptions` (section `SapConnector`) used for timeout/ApiKey. Teams should add `Sap:ODataBaseUrl` to all appsettings environments.

---

## Decision 3: SapNcoClient — NotSupportedException Stub

`SapNcoClient` implements `ISapNcoClient` (empty interface) and exposes no callable methods at the interface level — consistent with the NCo library requiring native DLLs unavailable in CI. If RFC methods are added to `ISapNcoClient` in future sprints, `SapNcoClient` must throw `NotSupportedException` on each.

---

## Decision 4: NoOpSapODataClient Retained for Test Use

`NoOpSapODataClient` is kept as `internal` and is no longer registered in DI (replaced by `SapODataClient`). Integration tests that override DI can still reference it directly or swap in their own mock.

---

## Decision 5: INotificationHistoryService — MemberId as string

`GetByMemberAsync` uses `string memberId` (not `Guid`) to align with `NotificationLog.RecipientUserId` (also `string`). No cross-module FK (ADR-001).

---

## Decision 6: Reporting AttendanceSummary — Separate from Attendance Domain

`M2.Domain.Reporting.AttendanceSummary` is a distinct shop-aggregate value object. The existing `M2.Domain.Attendance.AttendanceSummary` is a per-employee record. Both are intentionally different — no consolidation until product requirements clarify.

---

## Decision 7: GoodsReceiptService PostToSapAsync — Outbox Deferred

`PostToSapAsync` currently logs and returns success (no actual outbox enqueue). Full Hangfire outbox wiring deferred to Sprint 5 when `IOutboxService` gets a real PostgreSQL-backed implementation. ADR-017 pattern is preserved in the interface contract.
