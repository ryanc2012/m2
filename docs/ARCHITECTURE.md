# M2 Architecture

## System Overview

M2 is a multi-tenant Point of Sale system with a 4-process deployment topology:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  MekaPosBff     │    │ MekaPromosBff   │    │  M2PortalBff    │
│  (POS staff)    │    │ (member app)    │    │ (admin portal)  │
│  :5000          │    │  :5001          │    │  :5002          │
└────────┬────────┘    └────────┬────────┘    └────────┬────────┘
         │                      │                      │
         └──────────────────────┼──────────────────────┘
                                │ HTTPS REST (API Key)
                                ▼
                    ┌───────────────────────┐
                    │   M2.Platform.Api     │
                    │   (platform core)     │
                    │   :5100               │
                    │                       │
                    │  /modules/members/    │
                    │  /modules/approvals/  │
                    │  /modules/promotions/ │
                    │  /modules/sales/      │
                    │  /modules/attendance/ │
                    │  /modules/goods-...   │
                    │  /modules/reporting/  │
                    │  /modules/notifications/│
                    └──────────┬────────────┘
                               │
                               ▼
                    ┌───────────────────────┐
                    │   PostgreSQL (Azure)  │
                    └───────────────────────┘
```

## Projects

| Project | Role | Port |
|---------|------|------|
| `M2.Platform.Api` | Platform core — all domain modules, DB access | 5100 |
| `M2.MekaPosBff` | BFF for Flutter POS staff app | 5000 |
| `M2.MekaPromosBff` | BFF for Flutter member/promos app | 5001 |
| `M2.M2PortalBff` | BFF for Blazor manager/admin portal | 5002 |
| `M2.Domain` | Domain models, interfaces, DTOs | — |
| `M2.Infrastructure` | EF Core, service implementations, migrations | — |
| `M2.SharedKernel` | Base entities, Result<T>, middleware | — |
| `M2.SapConnector` | SAP OData/NCo client stubs | — |

## Communication Pattern

- **BFF → Platform:** HTTPS REST with `X-Api-Key` header. Platform validates the key via `ApiKeyMiddleware`.
- **Platform → DB:** EF Core with PostgreSQL. Multi-tenant via `TenantId` on all entities.
- **SAP integration:** Platform calls SAP OData REST (primary) or NCo RFC (fallback) via `ISapODataClient`.

## Module Endpoints

All domain modules are exposed at `/modules/{name}/` on the Platform API. BFFs call these via typed
`IXxxModuleClient` HTTP clients registered in `M2.Infrastructure/InterModule/`.

The `InterModuleServiceExtensions.AddInterModuleClients()` configures each typed client with:
- `BaseAddress` pointing to `Platform:BaseUrl` (defaults to `https://localhost:5100`)
- `X-Api-Key` header set from `Platform:ApiKey`

## Key Decisions

See `.squad/decisions.md` for all ADRs. Key ones:
- **ADR-001:** Modular Monolith with HTTP-enforced module boundaries. Platform core is an independent
  process; BFFs are thin routing/auth layers.
- **ADR-002:** One BFF per client app.
- **ADR-007:** Entra ID + MSAL authentication across all apps.
- **ADR-013:** Multi-tenant from day 1 — all entities have `TenantId` + `ShopId`.

For full architecture narrative and ADR context, see `docs/architecture/ARCHITECTURE.md`.
