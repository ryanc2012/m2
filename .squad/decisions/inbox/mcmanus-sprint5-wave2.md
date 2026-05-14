# McManus — Sprint 5 Wave 2 Decisions

**Author:** McManus  
**Date:** 2025-05-14  
**Tickets:** S5.3, S5.4

---

## Auth Object Naming Conventions (S5.3)

Auth object names follow the `M_{MODULE}_{ACTION}` convention established in Wave 1. Finalized assignments per BFF:

| Auth Object | Scope |
|---|---|
| `M_PROMOTION_MANAGE` | Create, activate, pause promotions (M2PortalBff) |
| `M_APPROVAL_MANAGE` | Approve and reject approval requests (M2PortalBff) |
| `M_GOODS_RECEIPT_CREATE` | Create GRN, confirm, record discrepancy, post to SAP (M2PortalBff + MekaPosBff) |
| `M_REPORTING_VIEW` | All reporting reads (M2PortalBff) |
| `M_MEMBER_ADMIN` | Update member profile (admin) (M2PortalBff) |
| `M_NOTIFICATION_MANAGE` | Admin push send (M2PortalBff) |
| `M_SALES_CREATE` | Create sales transaction (MekaPosBff) |
| `M_SALES_VOID` | Void transactions and initiate returns (MekaPosBff) |
| `M_ATTENDANCE_SELF` | Clock-in / clock-out (own record, MekaPosBff) |
| `M_APIKEY_MANAGE` | Full API key CRUD (M2PortalBff via Platform.Api) |

Read-only / list operations on Promotions, Approvals, Attendance (admin reads), Members (GET),
and all MekaPromosBff endpoints allow any authenticated user — no additional auth object check.

MekaPromosBff is consumer-facing with API key machine-to-machine auth; ClaimsPrincipal.NameIdentifier
may be absent on API-key-authenticated requests. Per spec: if userId is null, AuthorizationService
returns Deny. Since no write auth object checks were added to MekaPromosBff endpoints, this is a
non-issue for Wave 2.

---

## API Key Path Exception (S5.4)

`/api/v1/apikeys` is registered directly on **Platform.Api** rather than following the `/modules/{name}/`
convention used for inter-module calls.

**Rationale:** API key management is an admin-facing operation invoked by human operators through
the Portal BFF or directly. It is not a domain module called by other BFFs via X-Internal-Call.
Placing it under `/modules/apikeys` would incorrectly imply it is an inter-module boundary and
subject it to the internal-secret bypass logic in ApiKeyMiddleware. The `/api/v1/apikeys` prefix
mirrors the admin API pattern and keeps the endpoint guarded by RequireAuthorization() +
M_APIKEY_MANAGE auth object check.

---

## Migration Name (S5.4)

EF Core migration: `Sprint5_ApiKeys`

Table: `m2.api_keys`  
Unique index: `IX_api_keys_TenantId_KeyHash`  
Key storage: SHA-256 hex (lowercase), 64 chars. Plaintext returned only at creation time, never persisted.
