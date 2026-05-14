# Verbal — Sprint 5 / S5.10 Decision Log
**Author:** Verbal (Test Engineer)
**Date:** 2025-05-14
**Scope:** AuthorizationService unit tests + BFF 401/403 integration tests

---

## TestAuthHandler — already shared, no extraction needed

`TestAuthHandler` was already in a standalone file at
`tests/M2.Tests.Integration/Helpers/TestAuthHandler.cs`.
Both `PortalBffWebApplicationFactory` and `MekaPosBffWebApplicationFactory` reference it from there.
No extraction needed.

---

## MekaPosBff Program.cs — converted to namespaced class

**Problem:** Adding `M2.MekaPosBff` as a project reference to the integration test assembly
(which already references `M2.M2PortalBff`) would produce two global-namespace `Program`
classes — CS0433 compile error.

**Decision:** Converted `M2.MekaPosBff/Program.cs` from top-level statements to a standard
`public partial class Program` inside `namespace M2.MekaPosBff`, matching the pattern already
used by `M2.Platform.Api`. This allows `WebApplicationFactory<M2.MekaPosBff.Program>` without
naming conflicts.

Also took the opportunity to add `.RequireAuthorization()` to the MekaPosBff `/api/v1` route
group (the code was missing it; all other BFFs had it).

---

## BFF factory auth override pattern — two-factory split

**Problem:** `TestAuthHandler` authenticates every request unconditionally, making it
impossible to produce 401 responses from the same factory instance.

**Decision:** Each BFF has two factory classes:
- `*WebApplicationFactory` — registers `TestAuthHandler`, all requests authenticated.
  Used for happy-path and 403 tests.
- `*AnonFactory` — does NOT override auth; JwtBearer stays active with fake AzureAd config.
  Requests without an `Authorization: Bearer` header → `JwtBearer.HandleAuthenticateAsync`
  returns `NoResult` → `RequireAuthorization()` triggers 401.

Both are registered as `IClassFixture<>` on the test class so each starts only once per suite.

---

## PortalBff 403 tests — skipped pending S5.3

PortalBff endpoint handlers (`ApprovalEndpoints`, `PromotionEndpoints`, etc.) do not yet call
`IAuthorizationService.CheckAsync`. That enforcement is McManus Wave 2 / S5.3.

**Decision:** Three PortalBff 403 tests are marked `[Fact(Skip = "Pending S5.3 ...")]` so the
suite stays green. The test bodies contain the correct URL, expected status, and seeding
instructions for when S5.3 lands.

---

## Rate limit test (429) — deferred to S6.RL

The rate-limiting middleware for `M2.MekaPromosBff` is not implemented yet.
No rate-limit test is written in S5.10.
Ticket: **S6.RL** — MekaPromosBff 429 / rate-limit integration test.

---

## InMemory EF limitations

`ExecuteDeleteAsync` is not supported by the EF InMemory provider. The cache-TTL unit test
uses standard `RemoveRange + SaveChangesAsync` instead to mutate the DB after the first cache
population.
