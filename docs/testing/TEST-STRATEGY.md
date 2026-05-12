# Test Strategy

_Last updated: 2026-05-12_

## 1. Testing Philosophy & Principles
- **Quality is everyone’s responsibility.** Testing is embedded throughout the SDLC.
- **Shift left:** Test early, automate everything possible.
- **Fail fast:** Catch issues at the lowest level.
- **Definition of Done:**
  - All code covered by appropriate automated tests (unit, integration, e2e).
  - All critical paths and edge cases tested.
  - No critical/blocker bugs open.
  - Security, performance, and contract tests pass.

## 2. Test Pyramid
- **Recommended ratios:** 70% unit, 20% integration, 10% e2e.
- **Unit:** Pure logic, services, helpers, validators.
- **Integration:** API endpoints, DB, SAP, auth, service boundaries.
- **E2E:** User journeys (POS, mobile, portal), cross-system flows.

## 3. Backend Testing Strategy (ASP.NET Core)
- **Unit:** xUnit, Moq/NSubstitute, naming: `ClassName_Method_Scenario_Expected`
- **Integration:** WebApplicationFactory, test DB (SQLite/in-memory), reset state per test.
- **Contract:** Use Pact or similar for BFF-to-service contracts.
- **API:** Recommend Rest Client (VSCode), Postman, or Restler for automated API fuzzing.

## 4. Flutter Mobile Testing Strategy
- **Widget:** `flutter_test` for UI logic/components.
- **Integration:** `integration_test` for flows, device automation.
- **Mocking:** `mockito` or `http_mock_adapter` for API.
- **Device coverage:** Test iOS/Android, key screen sizes, emulators + real devices.
- **CI:** Use emulators in pipeline, parallelize where possible.

## 5. Blazor Testing Strategy
- **Component:** bUnit for Blazor components.
- **Integration/E2E:** Playwright for UI flows, especially approval workflows.
- **Scenarios:** Cover all approval paths, edge cases, and error handling.

## 6. Authentication & Authorization Testing
- **Azure Entra ID:** Use test tenant or mock tokens for lower envs.
- **API Key:** Test valid/invalid/expired/missing keys.
- **SAP Auth:** Mock SAP auth objects, test all role/permission paths.

## 7. SAP Integration Testing
- **Without SAP:** Use SAP sandbox or mock service.
- **Integration env:** Dedicated test SAP system or robust mocks.
- **Data setup:** Use fixtures/scripts to seed test data.

## 8. Security Testing
- **OWASP Top 10:** Automated scans (e.g., OWASP ZAP), manual review.
- **Auth bypass:** Attempt privilege escalation, token tampering.
- **Input validation:** Fuzz inputs, test for injection (SQL, XSS, etc).
- **API security:** Test rate limits, API key abuse, error leakage.

## 9. Performance Testing
- **Scenarios:** Sales throughput, promo load, concurrent POS users.
- **Tool:** k6 (preferred), JMeter, NBomber for .NET.
- **Targets:** Define SLAs for response time, throughput, error rate.

## 10. Test Environment Strategy
- **Matrix:** Dev (fast feedback), Staging (prod-like), Prod (smoke only).
- **Data:** Use synthetic, anonymized, or seeded data. Reset between runs.
- **CI/CD:**
  - Unit: every commit
  - Integration: PRs, nightly
  - E2E: nightly, pre-release

## 11. Test Tooling Summary
| Test Type         | Tool(s)                | When it Runs           |
|-------------------|-----------------------|-----------------------|
| Unit              | xUnit, flutter_test   | Every commit/PR       |
| Integration       | WebAppFactory, bUnit, integration_test | PR, nightly |
| E2E               | Playwright, integration_test | Nightly, pre-release |
| API               | Rest Client, Postman, Restler | PR, nightly |
| Contract          | Pact                  | PR, nightly           |
| Security          | OWASP ZAP, Restler    | Nightly, pre-release  |
| Performance       | k6, JMeter, NBomber   | Pre-release           |
