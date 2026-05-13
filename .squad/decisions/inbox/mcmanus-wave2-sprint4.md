# McManus Wave 2 Decision Log — Sprint 4

**Author:** McManus (Backend Dev)
**Date:** 2026-05-13
**Commits:** cb48e40 (S4.1), 28e04ff (S4.5), 4bd4c21 (S4.6)

---

## Decision 1 — Idempotency key via `IIdempotencyContext` scoped service (S4.1)

**Problem:** `ISalesService.CreateTransactionAsync` has 7 positional parameters; the 7th is `CancellationToken`. Existing unit tests mock the interface with Moq `Setup` expressions that supply exactly 7 positional args. Adding an 8th optional `string? idempotencyKey` parameter causes `CS0854` (expression trees cannot use optional argument defaults), breaking compilation. Test files are owned by Verbal and must not be modified.

**Decision:** Do not add `idempotencyKey` to the `ISalesService` interface. Instead, introduce a scoped ambient `IIdempotencyContext` (in `M2.Infrastructure.Sales`). The endpoint handler sets `idempotencyCtx.Key = payload.IdempotencyKey` before calling the service. `SalesService` reads from `IIdempotencyContext` internally.

**Trade-offs:**
- The idempotency key is not visible in the interface contract — reviewers need to know about `IIdempotencyContext`
- Follows a well-established pattern (similar to `IHttpContextAccessor`) and avoids polluting the domain interface
- `IIdempotencyContext` is `public` so it can be mocked if SalesService unit tests are ever written against the concrete class

**Rejected alternatives:**
- `IHttpContextAccessor` + HTTP header: would require the endpoint to set a header on the same request, which is awkward in minimal-API handlers
- Overloaded interface method: C# overloads work but add noise to the domain interface; Moq would need explicit setup for the new overload
- `CreateTransactionRequest` value object: breaks all 7 existing test call sites

---

## Decision 2 — SignalR dispatcher as `ISignalRNotificationDispatcher` interface (S4.5)

**Problem:** `NotificationHub` lives in `M2.Platform.Api`. `NotificationService` lives in `M2.Infrastructure`. If `NotificationService` injects `IHubContext<NotificationHub>` directly, it creates an assembly dependency from `M2.Infrastructure` → `M2.Platform.Api` (circular, since Platform.Api already references Infrastructure).

**Decision:** Define `ISignalRNotificationDispatcher` in `M2.Infrastructure.Notifications`. `NotificationService` accepts `ISignalRNotificationDispatcher?` (optional injection — null if not registered). `Platform.Api` registers `SignalRNotificationDispatcher` which wraps `IHubContext<NotificationHub>`.

**Trade-offs:**
- One extra interface + class, but avoids circular dependency entirely
- The `?` optional injection means Infrastructure can be used standalone (e.g., in non-HTTP contexts) without SignalR

**Hub auth design:** WebSocket connections can't send custom headers. `access_token` query parameter is promoted to `X-Api-Key` header by an inline middleware added before `UseAuthentication()`. Existing `ApiKeyMiddleware` then validates it transparently.

---

## Decision 3 — FCM graceful degradation when ADC not configured (S4.6)

**Decision:** `FirebaseApp.Create(...)` is called in `InfrastructureServiceExtensions.AddInfrastructure()`. If `GoogleCredential.GetApplicationDefault()` throws (no `GOOGLE_APPLICATION_CREDENTIALS` set, common in dev/test), the exception is caught and swallowed **only when not in Production** (checked by `environment?.IsDevelopment() == true || environment is null`). In Production, the exception will surface at startup, which is the correct fail-fast behaviour.

At runtime, `SendPushAsync` calls `GetMessagingOrNull()` which returns `null` if `FirebaseApp.DefaultInstance` is null. A warning is logged and the method returns `Result.Success()` (no crash, no mobile push in dev/test — acceptable).

**Stale token handling:** `MessagingErrorCode.Unregistered` removes the device registration from the DB. Other FCM exceptions are logged as warnings but do not fail the operation.

**Package version chosen:** `FirebaseAdmin 3.5.0` (latest stable at time of implementation via `dotnet add package FirebaseAdmin --version 3.*`).

---

## Decision 4 — `IIdempotencyContext` registered as `public` interface

`IIdempotencyContext` is marked `public` (not `internal`) so BFFs or other Platform.Api callers can potentially inject and set it without InternalsVisibleTo tricks. The concrete `IdempotencyContext` implementation remains `internal`.
