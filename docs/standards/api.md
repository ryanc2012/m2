# API / Backend Documentation Standard

> **Author:** McManus (Backend Dev)  
> **Date:** 2026-05-15  
> **Status:** Approved — Mandatory for all backend contributors

---

## Table of Contents

- [⚠️ Security: No Server-Side Demo Mode](#️-security-no-server-side-demo-mode)
1. [API Endpoint Documentation](#1-api-endpoint-documentation)
2. [Service / Repository Layer](#2-service--repository-layer)
3. [Business Logic Rules](#3-business-logic-rules)
4. [Auth & Security](#4-auth--security)
5. [Error Handling](#5-error-handling)
6. [Template: New API Endpoint](#6-template-new-api-endpoint)

---

## ⚠️ Security: No Server-Side Demo Mode

> **This is a hard security rule — no exceptions.**

Demo mode is a **client-only** concern. The `--dart-define=DEMO_MODE=true` flag only affects the Flutter client — it bypasses MSAL token acquisition and injects mock service providers entirely on the client. The API/backend layer is never reached in demo mode.

**The API/backend MUST NEVER:**

- Check for a demo mode flag, header, query parameter, or any other client-supplied signal
- Skip authentication or authorisation based on any client-supplied value
- Return mock, synthetic, or reduced data because a client claims to be in "demo mode"
- Implement any code path that behaves differently because a client says it is in "demo mode"

Any endpoint that behaves differently in "demo mode" on the server is a **security vulnerability** — it is effectively an auth bypass exploitable by any caller who sends the right header or parameter.

**When documenting endpoints:**
The "Demo mode" field must be **omitted** from all API endpoint documentation.  
If a legacy doc contains a "Demo mode" row, replace its value with: `N/A — server has no demo mode`

See §4.3 for the auth-layer documentation rule.

---

## 1. API Endpoint Documentation

Every endpoint exposed by a BFF or Platform.Api module must have a corresponding documentation block. Write it as a Markdown section in the relevant feature doc (or inline in the endpoint file as a `/// <summary>` XML comment on the handler delegate when the endpoint group is short enough to warrant it).

### 1.1 Standard Fields

Each endpoint doc must include all of the following fields:

| Field | Description |
|-------|-------------|
| **Method** | HTTP verb: `GET`, `POST`, `PUT`, `PATCH`, `DELETE` |
| **Path** | Full route including prefix, e.g. `/modules/sales/transactions` |
| **Host** | Which process owns this route — one of: `MekaPosBff`, `MekaPromosBff`, `M2PortalBff`, `Platform.Api` |
| **Auth Required** | `Yes (Bearer)` / `Yes (X-Api-Key)` / `No` — see §4 |
| **Request Body** | JSON schema or record type name + field table |
| **Response Body** | JSON schema or DTO type name + field table |
| **Status Codes** | See §1.2 |
| **Idempotency** | `Yes (IdempotencyKey in body)` / `No` — required for all POST mutations |

### 1.2 Status Codes to Always Document

Document all of the following for every endpoint, even if the response is "not applicable" (mark as `N/A`):

| Code | When to use |
|------|-------------|
| `200 OK` | Successful read or synchronous mutation. Body contains the result DTO. |
| `201 Created` | Resource was created. Include `Location` header if applicable. |
| `400 Bad Request` | Validation failure or business rule rejection. Body: standard error object (§5). |
| `401 Unauthorized` | Missing or invalid Bearer token / API key. |
| `403 Forbidden` | Token valid, but the caller lacks the required scope or SAP auth object. |
| `404 Not Found` | Resource with the given ID does not exist in this tenant/shop scope. |
| `409 Conflict` | Idempotency replay or optimistic concurrency conflict. |
| `429 Too Many Requests` | Rate limit exceeded (MekaPromosBff: 60 req/min per IP). Include `Retry-After` header. |
| `500 Internal Server Error` | Unhandled exception. Never expose stack trace in production. |

If an endpoint cannot logically return a code (e.g. a write-only sink cannot return `404`), mark it `N/A — not applicable` rather than omitting the row.

### 1.3 BFF vs Platform.Api Routing

This platform uses 4 processes. Be explicit about which layer the endpoint lives in:

- **BFF endpoints** (`/sales/...`, `/promotions/...`, etc.) are consumed by Flutter/Blazor. Auth via Bearer (MSAL).
- **Module endpoints** (`/modules/sales/...`, `/modules/promotions/...`, etc.) live in `Platform.Api`. Auth via `X-Api-Key` from the BFF. Do not expose these directly to mobile clients.

Always document both the BFF route and, if the BFF proxies to Platform.Api, the downstream module route.

---

## 2. Service / Repository Layer

### 2.1 Dart Service Classes (Flutter-side)

Flutter services that call BFF endpoints must be documented at the class and method level using Dart doc comments (`///`).

**Class-level:**
```dart
/// Handles all sales transaction operations against the MekaPosBff.
///
/// Auth: Requires a valid MSAL token injected via [MsalAuthProvider].
/// Demo Mode: All methods return [SalesTransactionStub] fixtures when
/// [DemoModeConfig.isEnabled] is true.
class SalesService { ... }
```

**Method-level (required fields):**
```dart
/// Creates a new sales transaction.
///
/// [tenantId] — Active tenant GUID from the current session context.
/// [shopId]   — Active shop GUID from the current session context.
/// [items]    — Non-empty list of line items. Throws [ArgumentError] if empty.
///
/// Returns a [SalesTransactionDto] on success.
///
/// Throws:
/// - [ApiException] with status 400 if any line item fails validation.
/// - [ApiException] with status 401 if the MSAL token has expired.
/// - [NetworkException] on connectivity loss.
///
/// Demo Mode: Returns [SalesTransactionStub.create()] without network call.
Future<SalesTransactionDto> createTransaction(...) async { ... }
```

### 2.2 C# Service Interfaces (Platform-side)

Every public interface method in `M2.Domain` must have XML doc comments covering:

```csharp
/// <summary>
/// Creates a new sales transaction for the given tenant and shop.
/// </summary>
/// <param name="tenantId">Tenant scope — required, must match the caller's claim.</param>
/// <param name="shopId">Shop scope — required.</param>
/// <param name="memberId">Optional member association. Pass null for guest transactions.</param>
/// <param name="cashierId">SAP employee ID of the cashier initiating the transaction.</param>
/// <param name="paymentMethod">Parsed from the BFF payload before this call.</param>
/// <param name="lineItems">At least one item required; validated inside the service.</param>
/// <param name="ct">Propagated from the HTTP request pipeline.</param>
/// <returns>
/// <see cref="Result{T}"/> wrapping the created <see cref="SalesTransaction"/> on success,
/// or a failure with a human-readable error message on domain rule violation.
/// </returns>
/// <remarks>
/// Side effect: Persists to <c>SalesTransactions</c> table via EF Core.
/// Does NOT post to SAP — SAP posting is triggered by a separate outbox worker.
/// </remarks>
Task<Result<SalesTransaction>> CreateTransactionAsync(...);
```

**Required XML doc tags:**

| Tag | Required when |
|-----|--------------|
| `<summary>` | Always |
| `<param>` | Every parameter |
| `<returns>` | All non-void methods |
| `<remarks>` | Whenever there are side effects, dependencies, or deferred behaviour |
| `<exception>` | Only for exceptions thrown directly (not wrapped in `Result<T>`) |

### 2.3 Side Effects and Dependencies

Document side effects explicitly in `<remarks>` or Dart doc body. The following are considered side effects and must always be called out:

- Database writes (EF SaveChanges)
- Outbox enqueue (SAP posting, notifications)
- MediatR event publication
- External HTTP calls (SAP OData/NCo, SMS gateway, FCM/APNs)
- Cache invalidation
- Hangfire job enqueue

**Format:**
```
Side effects:
  - Persists <Entity> to <Table> via EF Core.
  - Enqueues <Payload> via IOutboxService → SapOutboxWorker (async, 30 s poll).
  - Publishes <EventName> via MediatR on <condition>.
```

---

## 3. Business Logic Rules

### 3.1 Rule Documentation Format

Business rules must be documented in a dedicated section in the feature's domain doc (or inline in the service `<remarks>` for short rules). Use this format:

```
Rule ID:    BR-PROMO-001
Name:       Stackable Promotion Filter
Owner:      IDiscountEngine
Trigger:    Every call to CalculateAsync
Condition:  Promotion.IsStackable == true AND Promotion.Status == Active
Action:     Include promotion in discount calculation
Else:       Skip — non-stackable promotions are excluded from engine input
Reference:  ADR-020
```

Assign Rule IDs with the pattern `BR-{DOMAIN}-{NUMBER}` (e.g. `BR-SALES-001`, `BR-PROMO-002`, `BR-APPR-001`).

### 3.2 Decision Tables for Conditional Rules

When a rule has 3 or more condition branches, use a decision table instead of prose:

**Example — Discount Engine Promotion Type Handling:**

| `PromotionType` | `FormulaJson` Keys | Discount Formula | Cap |
|----------------|-------------------|-----------------|-----|
| `PercentDiscount` | `percentage` (decimal) | `total × (percentage / 100)` | Capped at total |
| `FixedDiscount` | `amount` (decimal) | `min(amount, total)` | Capped at total |
| `BuyXGetY` | `buyQty`, `getQty` (int) | `(total_qty / (buyQty + getQty)) × getQty × cheapest_unit_price` | Capped at total |
| Unknown type | — | `0` (no discount, logged) | — |

If `FormulaJson` is malformed or missing a required key, the engine falls back to `0` discount for that promotion (silent degradation by design — see DiscountEngine implementation).

### 3.3 Promotion Eligibility Rules

Document eligibility rules as a checklist — the reader should be able to trace a cart through the rules manually:

```
A promotion is eligible for a cart if ALL of the following are true:
  [ ] Promotion.Status == Active
  [ ] Promotion.IsStackable == true  (non-stackable excluded by DiscountEngine)
  [ ] Current datetime is within [StartDate, EndDate]
  [ ] Shop is in the promotion's target scope (or promotion has no shop restriction)
  [ ] Coupon is valid (if coupon-gated): not expired, not redeemed, issued to this member
```

### 3.4 Return / Refund Rules

Refund method is enforced at the domain boundary (not in the BFF payload):

```
Rule BR-SALES-001: Refund Method Enforcement
  ReturnTransaction.RefundMethod is always copied from SalesTransaction.PaymentMethod
  at the time ReturnService.InitiateReturnAsync is called.
  Callers cannot override this value. (ADR-016)
```

---

## 4. Auth & Security

### 4.1 Authentication Model

This platform has two auth layers — document which applies to each endpoint:

| Layer | Token type | Used by | Enforced by |
|-------|-----------|---------|------------|
| **MSAL / Entra ID** | Bearer JWT | Flutter clients → BFFs | `AddMicrosoftIdentityWebApi` + `RequireAuthorization()` |
| **API Key (X-Api-Key)** | HMAC-SHA256 hash | BFFs → Platform.Api modules | `ApiKeyMiddleware` |

Every endpoint doc must state which auth layer it sits behind. Never document an endpoint as "no auth" unless it is explicitly a public endpoint (e.g. `/health`, CORS preflight).

### 4.2 MSAL-Specific Documentation

When documenting a BFF endpoint that requires a Bearer token, include:

```
Auth:
  Type:    Bearer (MSAL / Entra ID)
  Scopes:  api://<client-id>/access_as_user   ← minimum required scope
  Claims:  tid (tenant), oid (user object ID)
  Notes:   Token is validated by Microsoft.Identity.Web on every request.
           Claims are NOT forwarded to Platform.Api — the BFF resolves
           TenantId/ShopId from its own session context before the module call.
```

If an endpoint requires additional SAP authorization object checks (e.g. sales void requires `Z_POS_VOID`), document the SAP auth object and field value alongside the MSAL scope:

```
SAP Auth Object:  Z_POS_VOID  (field: ACTIVITY, value: 03)
Check:            IAuthorizationService.CheckAsync called inside the service before mutation.
```

### 4.3 Demo Mode Is Client-Only — No Server-Side Bypass

Demo mode (`--dart-define=DEMO_MODE=true`) bypasses MSAL token acquisition entirely on the Flutter side and injects mock service providers. The BFF is **never called** in demo mode.

> **See the [⚠️ Security: No Server-Side Demo Mode](#️-security-no-server-side-demo-mode) section at the top of this document for the full rule.**

If a test requires hitting a real BFF endpoint without MSAL, use a provisioned API key against the dev environment — not a demo mode flag.

### 4.4 Endpoint Authorization Checklist

Before shipping any new endpoint, verify and document:

- [ ] `RequireAuthorization()` is applied to the route group (not just individual routes)
- [ ] The MSAL scope or API key requirement is stated in the doc
- [ ] Any SAP auth object check is performed before the mutation, not after
- [ ] Health endpoints (`/health`, `/health/ready`, `/health/live`) are exempt from both auth and rate limiting
- [ ] 401 and 403 responses are included in the status code table

---

## 5. Error Handling

### 5.1 Standard Error Response Format

All BFF and Platform.Api error responses use this envelope. Do not invent alternative shapes:

```json
{
  "error": "string",         // Human-readable message (English). Safe to display in UI.
  "code": "string",          // Machine-readable code, e.g. "VALIDATION_FAILED", "NOT_FOUND"
  "traceId": "string"        // ASP.NET Core request TraceIdentifier. Include in support tickets.
}
```

`500` responses must never include stack traces or internal exception messages in production. Serilog captures the full detail server-side.

### 5.2 Result<T> to HTTP Response Mapping

All service methods return `Result<T>`. The mapping to HTTP status codes is standardised:

| `Result<T>` state | HTTP mapping | Notes |
|------------------|-------------|-------|
| `IsSuccess = true` | `200 OK` or `201 Created` | Body: DTO |
| `IsFailure`, error starts with `"Not found"` | `404 Not Found` | |
| `IsFailure`, validation / domain rule | `400 Bad Request` | Body: error envelope |
| Unhandled exception (global handler) | `500 Internal Server Error` | Error logged, safe message returned |

Do not return `Result.Failure` for auth failures — let the auth middleware handle 401/403 before the handler is reached.

### 5.3 Documenting Error Scenarios

For each endpoint, document error scenarios in a table under `Error Scenarios`:

| Scenario | Status | `error` message |
|----------|--------|----------------|
| PaymentMethod string not parseable | `400` | `"Invalid payment method"` |
| Transaction ID not found | `404` | `"Not found"` |
| Transaction already voided | `400` | `"Transaction is already voided"` |
| Missing Bearer token | `401` | (ASP.NET Core default) |
| Insufficient MSAL scope | `403` | (ASP.NET Core default) |

Keep error messages stable — they are part of the API contract and Flutter clients may pattern-match on them.

### 5.4 SAP Outbox / Async Errors

For endpoints that trigger SAP posting via the outbox (e.g. Goods Receipt), document the async failure path separately:

```
Async Error Path (SAP Outbox):
  The HTTP response is 200 OK as soon as the outbox entry is persisted.
  SAP posting failures are handled by SapOutboxWorker (Polly exponential backoff, 3 retries).
  After 3 failures, the outbox entry is marked SapOutboxStatus.Failed.
  Monitoring: Query SapOutboxEntries where Status = 'Failed'.
  No callback or webhook is sent to the client on async failure.
```

---

## 6. Template: New API Endpoint

Copy this template when documenting a new endpoint. Fill in every field — mark `N/A` rather than leaving blanks.

---

```markdown
## POST /modules/{domain}/{resource}

> **Host:** Platform.Api  
> **Module:** `{Domain}ModuleEndpoints`  
> **Added:** YYYY-MM-DD (Sprint N)

### Overview

One-sentence description of what this endpoint does and why it exists.

### Auth

| Field | Value |
|-------|-------|
| Type | `Bearer (MSAL)` / `X-Api-Key` / `None` |
| Scope | `api://<client-id>/access_as_user` |
| SAP Auth Object | `Z_XXX` (field: `ACTIVITY`, value: `NN`) or N/A |

### Request

**Body** (`application/json`):

```json
{
  "tenantId": "guid",
  "shopId": "guid",
  "field1": "string",
  "field2": 0
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `tenantId` | `Guid` | Yes | Tenant scope. Must match the caller's MSAL `tid` claim (BFF-resolved). |
| `shopId` | `Guid` | Yes | Shop scope. |
| `field1` | `string` | Yes | … |
| `field2` | `int` | No | Defaults to `0`. |

### Response

**200 OK** (`application/json`):

```json
{
  "id": "guid",
  "status": "string"
}
```

| Field | Type | Description |
|-------|------|-------------|
| `id` | `Guid` | Newly created resource ID. |
| `status` | `string` | Enum string, e.g. `"Pending"`. |

### Status Codes

| Code | Scenario |
|------|----------|
| `200 OK` | Success |
| `400 Bad Request` | Validation failure or business rule rejection |
| `401 Unauthorized` | Missing or invalid token |
| `403 Forbidden` | Valid token, insufficient scope or SAP auth object |
| `404 Not Found` | N/A |
| `409 Conflict` | N/A |
| `429 Too Many Requests` | N/A — not on Platform.Api module routes |
| `500 Internal Server Error` | Unhandled exception |

### Error Scenarios

| Scenario | Status | `error` message |
|----------|--------|----------------|
| `field1` is empty | `400` | `"field1 is required"` |
| Referenced resource not found | `400` | `"…"` |

### Idempotency

`Yes` — pass `idempotencyKey` (UUID) in the request body. Replayed requests with the same key return the original response without re-executing the mutation.  
`No` — this endpoint is safe to retry without side effects.

> ⚠️ Do not add demo mode fields to API endpoint docs. The server has no demo mode. See the security section at the top of this standard.

### Business Rules

- **BR-{DOMAIN}-NNN** — Brief rule description. See §3 of this standard.

### Side Effects

- Persists `{Entity}` to `{table}` via EF Core `SaveChangesAsync`.
- (List any outbox enqueue, events, external calls, or state N/A)
```

---

## Appendix A: Rule ID Registry Prefix Table

| Domain | Prefix |
|--------|--------|
| Sales | `BR-SALES` |
| Promotions / Discounts | `BR-PROMO` |
| Approvals | `BR-APPR` |
| Members | `BR-MBR` |
| Goods Receipt | `BR-GR` |
| Attendance | `BR-ATT` |
| Notifications | `BR-NOTIF` |
| Authorization | `BR-AUTHZ` |

## Appendix B: Related Standards

- `docs/standards/CODING-STANDARDS.md` — C#/.NET and Dart naming, file structure, REST API design
- `docs/architecture/ARCHITECTURE.md` — 4-process topology, BFF pattern, inter-module communication
- `docs/data/DATA-DESIGN.md` — Schema, entity relationships, domain boundaries
