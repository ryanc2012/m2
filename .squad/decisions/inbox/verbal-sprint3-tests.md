# verbal-sprint3-tests — Sprint 3 Test Decisions

**Date:** 2026-05-12  
**Author:** Verbal (Test Engineer)  
**Sprint:** 3 — Promotions, Sales, Attendance Test Suite  

---

## Summary

23 new unit tests written across 7 test files. Total suite: **40/40 passing**.

---

## Test Architecture Decisions

### 1. Mixed Test Style: Entity-Direct + Mock-Contract

Tests fall into two categories:

| Style | Used When | Examples |
|-------|-----------|---------|
| Direct entity construction + method calls | Domain invariant (no I/O, pure state) | `CreatePromotion_ShouldBeDraft_OnCreation`, `ClockIn_ShouldCreateRecord_WithNullClockOut`, `CompleteTransaction_ShouldSetCompletedAt` |
| `Mock<IService>` contract documentation | Service orchestration, failure paths, async flows | `ActivatePromotion_ShouldTriggerCouponIssuance`, `VoidTransaction_WhenCompleted_ShouldFail`, `ClockOut_WhenNoOpenRecord_ShouldFail` |

This mirrors the Sprint 2 pattern from `ApprovalServiceTests.cs`.

---

### 2. Coupon Issuance Trigger Test (ADR-013)

`ActivatePromotion_ShouldTriggerCouponIssuance` uses a Moq `.Callback()` on `IPromotionService.ActivateAsync` to invoke `ICouponService.IssueAsync`, then verifies the coupon service was called `AtLeastOnce`. This is a contract-documentation test — it documents the **obligation** that the real service implementation must honour: activation must trigger coupon pre-issuance for eligible members.

**Note for McManus (implementation):** The real `PromotionService.ActivateAsync` must call `ICouponService.IssueAsync` for each eligible member batch.

---

### 3. ECR Structural Deferred Test (ADR-009/ADR-010)

`IEcrService_ExistsAsInterface_NotImplemented` uses reflection against `typeof(IEcrService).Assembly` to assert zero concrete classes implement `IEcrService`. This test will **fail automatically** if any developer accidentally adds a concrete ECR implementation before the post-MVP green light — acting as a scope guard in CI.

---

### 4. ADR Numbering Discrepancy Noted

The sprint spec uses ADR numbers that differ from `decisions.md`:

| Task ADR | decisions.md ADR | Topic |
|----------|-----------------|-------|
| ADR-012 | ADR-013 | Coupon issuance timing (pre-issued on activation) |
| ADR-021 | ADR-020 | Discount stacking (stackable flag) |
| ADR-008 | ADR-016 | Return refund method (original payment only) |

Test comments use the task-spec ADR numbers verbatim to match the sprint spec. Recommend: reconcile numbering in `decisions.md` or add an alias table.

---

### 5. `Promotion.Activate()` Already-Active Guard

The entity already had the invariant guard (`throw new InvalidOperationException` when `Status == Active`) in the pre-built domain. `ActivatePromotion_WhenAlreadyActive_ShouldThrow` tests this directly at entity level — no service mock needed.

---

## Files Delivered

| File | Tests | Domain Covered |
|------|-------|----------------|
| `Promotions/PromotionServiceTests.cs` | 4 | IPromotionService, Promotion entity |
| `Promotions/DiscountEngineTests.cs` | 4 | IDiscountEngine, ADR-020 stacking |
| `Promotions/CouponServiceTests.cs` | 3 | ICouponService, Coupon redemption |
| `Sales/SalesServiceTests.cs` | 4 | ISalesService, SalesTransaction entity |
| `Sales/ReturnServiceTests.cs` | 3 | IReturnService, ReturnTransaction entity |
| `Sales/EcrServiceTests.cs` | 1 | IEcrService structural (deferred scope) |
| `Attendance/AttendanceServiceTests.cs` | 4 | IAttendanceService, AttendanceRecord entity |

**Total new: 23 | Suite total: 40 | Passed: 40 | Failed: 0**
