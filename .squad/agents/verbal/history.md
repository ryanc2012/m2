# Verbal — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Tester
- **Joined:** 2026-05-11T02:37:43.917Z

## Learnings

<!-- Append learnings below -->

## 2026-05-13 — Integration Test Harness Rewired
- PlatformWebApplicationFactory targets M2.Platform.Api.Program directly
- M2PlatformIntegrationTestBase sets X-Api-Key + X-Internal-Call + X-Internal-Secret on every client
- All 8 module smoke tests now use PlatformWebApplicationFactory
- Integration tests depend on Platform.Api having UseAuthorization() in pipeline — confirmed working after Copilot fix

2026-05-12 — Sprint 4: 15 new tests, 55/55 passing, domain invariants, NotificationLog.MarkAsRead(), reporting value objects. Project feature-complete for UAT.
2026-05-12 — Sprint 3: 23 new tests (Promotions, Sales, Attendance, ECR scope guard). 40/40 passing. Test style: entity-direct + mock-contract. ADR/test number mapping noted.

2026-05-12 — Sprint 2: 17 unit tests, integration harness, 3 projects delivered. See .squad/log/2026-05-12T142236Z-sprint2-complete.md.
- 2026-05-12: Test strategy established. Key decisions: test pyramid (70/20/10), tool choices (xUnit, flutter_test, bUnit, Playwright, Pact, k6, Restler, OWASP ZAP). Risks: SAP integration testability, auth complexity, device fragmentation. Recommend: automate security/perf, mock SAP, enforce contract tests.
- 2026-05-12 (Sprint 2): Wrote contract and unit test stubs for Approval, Notification, Member APIs. 17 unit tests passing. Integration test scaffold (WebApplicationFactory) ready for McManus's endpoint delivery. Strategy: Moq-based contract documentation tests compile and pass now; each test carries a note for the McManus implementation to run real assertions against. Domain entity factory methods not needed — McManus pre-built public constructors on all domain entities. ISmsGateway structural test confirms ADR-010 abstraction at type-system level. OTP domain invariant tests verify MarkUsed() contract inline.
- 2026-05-12 (Sprint 3): Wrote 23 new tests across Promotions, Sales, Attendance. Total suite: 40/40 passing. Mix of entity-level invariant tests (direct construction + method calls) and Moq contract-documentation tests for service interfaces. Key learnings: All domain entities were pre-built by McManus with public constructors; Promotion.Activate() already guarded against double-activation. ADR numbering in sprint spec does not always match decisions.md (task ADR-012 = decisions ADR-013 coupon issuance; task ADR-021 = decisions ADR-020 stacking; task ADR-008 = decisions ADR-016 return payment method) — used task refs verbatim in comments. EcrServiceTests uses reflection to assert no concrete IEcrService exists (structural deferred-scope test pattern). IDiscountEngine.CalculateAsync is async with shopId + optional memberId.

### 2026-05-12 — Cross-Agent Context (from Initial Planning Session)

**From Keyser (Architecture):**
- ADR-001 (Modular Monolith): In-process module boundaries mean integration tests can test full request flows in a single test host (no network between modules). `WebApplicationFactory<T>` with real EF Core + PostgreSQL (Testcontainers) is the recommended integration test pattern. No service mesh or inter-service network tests needed at this stage.
- ADR-006 (SAP Integration — OData primary): SAP mock strategy should target the OData HTTP surface. Use `WireMock.NET` or equivalent to stub SAP OData responses. Contract tests (Pact) should cover the OData contract between the SAP Adapter module and the real SAP system.
- ADR-004 (Authorization in-process): Auth tests must cover the SAP-style auth object model — authorization objects, field names, and value ranges. Unit test the permission evaluation logic exhaustively; integration-test the cache invalidation path.

**From McManus (Backend Dev):**
- BE-REC-001 R4 (API keys SHA-256 hashed) and R5 (QR codes as signed JWTs) are security-critical paths. OWASP ZAP scans and Restler fuzzing must specifically cover the API key issuance endpoint and the coupon QR validation endpoint.
- BE-REC-001 R1 (idempotency keys on Sales API): Integration tests must verify idempotent replay behavior — same key, same result, no duplicate records.

**From Edie (Database):**
- Soft delete is universal on all domain entities. Delete tests must assert soft-delete behavior (IsDeleted=true, record still exists) — hard-delete must never succeed on domain entities. Add explicit test cases for soft-delete + re-query visibility filtering.
