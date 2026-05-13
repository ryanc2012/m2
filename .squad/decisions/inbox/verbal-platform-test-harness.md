# Decision: Rewire Integration Test Harness for 4-Process Topology

**Author:** Verbal (Test Engineer)
**Date:** 2026-05-13
**Status:** Decided

## Context

The confirmed 4-process topology places all `/modules/{name}/` endpoints exclusively in `M2.Platform.Api`.
BFFs call the Platform API via HTTP and do not host module endpoints themselves. The previous integration
test harness (`TestWebApplicationFactory` / `M2IntegrationTestBase`) targeted the BFF `Program` class,
meaning the 8 module smoke tests were asserting against the wrong process.

## Decisions

### 1. Module tests now target `PlatformWebApplicationFactory` (not BFF factory)

All 8 `*ModuleTests.cs` files now inherit `M2PlatformIntegrationTestBase` and declare
`IClassFixture<PlatformWebApplicationFactory>`. The platform factory spins up
`M2.Platform.Api.Program` in-memory with the same test-safe overrides used by the BFF factory
(in-memory EF Core DB, `TestAuthHandler`, NoOp SAP stubs).

### 2. `M2PlatformIntegrationTestBase` sets platform authentication headers

Every `HttpClient` created by `M2PlatformIntegrationTestBase` carries:

| Header | Value |
|---|---|
| `X-Api-Key` | `test-api-key` |
| `X-Internal-Call` | `true` |
| `X-Internal-Secret` | `internal` |

These match the values injected into `PlatformWebApplicationFactory.ConfigureWebHost` via
`Platform:ApiKey` and `Platform:InternalCallSecret` in-memory config keys.

### 3. `M2BffIntegrationTestBase` remains for BFF-level tests

`M2IntegrationTestBase` was split into two classes in `M2IntegrationTestBase.cs`:

- `M2BffIntegrationTestBase` — typed to `WebApplicationFactory<Program>` (BFF `Program`). Retained for
  any future tests that validate BFF-level concerns (health endpoints, BFF auth flows, BFF routing).
- `M2PlatformIntegrationTestBase` — typed to `PlatformWebApplicationFactory`. All module smoke tests
  use this.

`ApprovalEndpointTests` (targeting `/api/approvals` BFF endpoints) was left unchanged — it uses
`TestWebApplicationFactory` directly and tests BFF-level behaviour.

### 4. `InterModuleTestHelper.WithInterModuleLoopback` is now generic

Changed signature from `WebApplicationFactory<Program>` to `WebApplicationFactory<TEntryPoint> where TEntryPoint : class`
so the helper works with both the BFF factory and the Platform factory when McManus wires
`IXxxModuleClient` typed HTTP clients.

### 5. Build status

`M2.Tests.Integration` will not compile until McManus creates `src/M2.Platform.Api/M2.Platform.Api.csproj`.
The project reference is in place. Unit tests (55/55) remain green.

## Consequences

- When McManus lands `M2.Platform.Api`, the integration test project compiles and module smoke tests
  exercise the real Platform API process boundary.
- BFF integration tests continue to use `TestWebApplicationFactory` — no cross-contamination.
- `PlatformWebApplicationFactory` exposes `ApprovalServiceMock` so `ApprovalsModuleTests` can control
  the `IApprovalService` stub without touching production assembly internals.
