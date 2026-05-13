# M2 Local Development Setup Guide

> **Author:** McManus (Backend Dev)
> **Date:** 2026-05-13
> **Audience:** Backend engineers onboarding to the M2 system

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Clone & Restore](#1-clone--restore)
3. [Start PostgreSQL](#2-start-postgresql)
4. [Configure Secrets](#3-configure-secrets)
5. [Run Database Migrations](#4-run-database-migrations)
6. [Start All Processes](#5-start-all-processes)
7. [Verify Everything Is Running](#6-verify-everything-is-running)
8. [Debugging Tips](#7-debugging-tips)

---

## Architecture Recap

M2 is a **4-process** system. All processes must be running for full end-to-end scenarios:

| Process | Role | HTTPS Port | HTTP Port |
|---|---|---|---|
| `M2.Platform.Api` | Platform core — all domain modules, only process with DB access | `5100` | — |
| `M2.MekaPosBff` | BFF for the Flutter POS app | `7274` | `5016` |
| `M2.MekaPromosBff` | BFF for the Flutter member/promos app | `7065` | `5280` |
| `M2.M2PortalBff` | BFF for the Blazor admin portal | `7104` | `5075` |

> **Critical startup order:** `M2.Platform.Api` **must** start first. The BFFs call Platform.Api on startup health-check and will fail to serve requests until it is reachable.

> **Tip:** For most backend work you only need **Platform.Api + one BFF**. Running all 4 is only necessary when testing cross-BFF or cross-module scenarios.

---

## Prerequisites

Install the following before starting:

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** — verify with `dotnet --version` (must be `9.x`)
2. **[Docker Desktop](https://www.docker.com/products/docker-desktop/)** — used to run PostgreSQL locally
3. **IDE** (pick one):
   - [Visual Studio 2022 17.9+](https://visualstudio.microsoft.com/) with the **ASP.NET and web development** workload
   - [JetBrains Rider 2024.1+](https://www.jetbrains.com/rider/)
   - [VS Code](https://code.visualstudio.com/) with the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension
4. **[EF Core CLI tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)** — install once globally:
   ```bash
   dotnet tool install --global dotnet-ef
   ```
   Verify: `dotnet ef --version` (must be `9.x`)

---

## 1. Clone & Restore

```bash
git clone https://github.com/ryan-chung_mekim/m2.git
cd m2

# Restore all NuGet packages
dotnet restore src/M2.sln
```

---

## 2. Start PostgreSQL

Platform.Api is the only process that connects to the database. Run PostgreSQL locally via Docker:

```bash
docker run -d \
  --name m2-postgres \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=m2_dev \
  -p 5432:5432 \
  postgres:16
```

Verify it is running:
```bash
docker ps --filter name=m2-postgres
```

### Connection string

The connection string used for local dev is:

```
Host=localhost;Port=5432;Database=m2_dev;Username=postgres;Password=postgres
```

This is configured via the `ConnectionStrings:DefaultConnection` key in `appsettings.json` for Platform.Api, **and** via the `M2_DB` environment variable for EF Core CLI tools (see [Section 4](#4-run-database-migrations)).

If you change the password or host, update both places.

---

## 3. Configure Secrets

**Never commit real secrets to source control.** Use `dotnet user-secrets` to store sensitive values per project.

The `appsettings.json` files contain `__PLACEHOLDER__` values for secrets — replace them via user-secrets as shown below.

---

### M2.Platform.Api

Platform.Api only needs the DB connection string and its own internal keys for dev. The `appsettings.Development.json` already sets `Platform:ApiKey` to `platform-dev-key`, which is sufficient for local dev.

```bash
cd src/M2.Platform.Api

dotnet user-secrets init   # only needed once

# PostgreSQL connection string (match your Docker container credentials)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=m2_dev;Username=postgres;Password=postgres"

# SAP OData base URL — use a dev/sandbox SAP endpoint or a mock
dotnet user-secrets set "Sap:ODataBaseUrl" "https://your-sap-dev.example.com/odata/"
```

> The `Platform:ApiKey` (`platform-dev-key`) and `Platform:InternalCallSecret` (`internal`) are already set to safe dev defaults in `appsettings.json` and `appsettings.Development.json`. You do **not** need to override them for local dev.

---

### M2.MekaPosBff

```bash
cd src/M2.MekaPosBff

dotnet user-secrets init

# Azure Entra ID (get these from the team's shared dev app registration)
dotnet user-secrets set "AzureAd:TenantId"  "<tenant-id>"
dotnet user-secrets set "AzureAd:ClientId"  "<client-id>"
dotnet user-secrets set "AzureAd:Audience"  "<audience>"

# SAP Connector
dotnet user-secrets set "SapConnector:BaseUrl" "https://your-sap-dev.example.com/sap/opu/odata/"
dotnet user-secrets set "SapConnector:ApiKey"  "<sap-api-key>"

# Platform.Api — must match the key set in Platform.Api
dotnet user-secrets set "Platform:ApiKey"            "platform-dev-key"
dotnet user-secrets set "Platform:InternalCallSecret" "internal"
dotnet user-secrets set "Platform:BaseUrl"           "https://localhost:5100"
```

---

### M2.MekaPromosBff

```bash
cd src/M2.MekaPromosBff

dotnet user-secrets init

dotnet user-secrets set "AzureAd:TenantId"  "<tenant-id>"
dotnet user-secrets set "AzureAd:ClientId"  "<client-id>"
dotnet user-secrets set "AzureAd:Audience"  "<audience>"

dotnet user-secrets set "SapConnector:BaseUrl" "https://your-sap-dev.example.com/sap/opu/odata/"
dotnet user-secrets set "SapConnector:ApiKey"  "<sap-api-key>"

dotnet user-secrets set "Platform:ApiKey"            "platform-dev-key"
dotnet user-secrets set "Platform:InternalCallSecret" "internal"
dotnet user-secrets set "Platform:BaseUrl"           "https://localhost:5100"
```

---

### M2.M2PortalBff

```bash
cd src/M2.M2PortalBff

dotnet user-secrets init

dotnet user-secrets set "AzureAd:TenantId"  "<tenant-id>"
dotnet user-secrets set "AzureAd:ClientId"  "<client-id>"
dotnet user-secrets set "AzureAd:Audience"  "<audience>"

dotnet user-secrets set "SapConnector:BaseUrl" "https://your-sap-dev.example.com/sap/opu/odata/"
dotnet user-secrets set "SapConnector:ApiKey"  "<sap-api-key>"

dotnet user-secrets set "Platform:ApiKey"            "platform-dev-key"
dotnet user-secrets set "Platform:InternalCallSecret" "internal"
dotnet user-secrets set "Platform:BaseUrl"           "https://localhost:5100"
```

> **Where to get the real values:** Ask Ryan or the team lead for the shared dev Azure app registration details and SAP dev credentials. Do not paste them into any file that gets committed.

---

## 4. Run Database Migrations

EF Core migrations are applied manually. The design-time factory reads the connection string from the `M2_DB` environment variable, falling back to `Host=localhost;Port=5432;Database=m2_dev;Username=postgres;Password=postgres`.

Set the environment variable, then apply all pending migrations:

**PowerShell (Windows):**
```powershell
$env:M2_DB = "Host=localhost;Port=5432;Database=m2_dev;Username=postgres;Password=postgres"

dotnet ef database update `
  --project src/M2.Infrastructure `
  --startup-project src/M2.Platform.Api
```

**bash / zsh (macOS / WSL):**
```bash
export M2_DB="Host=localhost;Port=5432;Database=m2_dev;Username=postgres;Password=postgres"

dotnet ef database update \
  --project src/M2.Infrastructure \
  --startup-project src/M2.Platform.Api
```

This creates the `m2` schema and all tables. Migration history is tracked in `m2.__EFMigrationsHistory`.

> **Adding a new migration** (for developers working on domain changes):
> ```bash
> dotnet ef migrations add <MigrationName> \
>   --project src/M2.Infrastructure \
>   --startup-project src/M2.Platform.Api \
>   --output-dir Migrations
> ```

---

## 5. Start All Processes

### ⚠️ Startup Order

**Always start `M2.Platform.Api` first.** The BFFs call Platform.Api and depend on it being ready.

```
M2.Platform.Api  →  (then)  M2.MekaPosBff / M2.MekaPromosBff / M2.M2PortalBff
```

---

### Option A — CLI (Separate Terminal Windows)

Open 4 terminals from the repo root. Run each command in its own terminal:

**Terminal 1 — Platform.Api (start this first):**
```bash
dotnet run --project src/M2.Platform.Api --launch-profile "M2.Platform.Api"
```
Wait until you see `Now listening on: https://localhost:5100` before starting BFFs.

**Terminal 2 — MekaPosBff:**
```bash
dotnet run --project src/M2.MekaPosBff --launch-profile https
```

**Terminal 3 — MekaPromosBff:**
```bash
dotnet run --project src/M2.MekaPromosBff --launch-profile https
```

**Terminal 4 — M2PortalBff:**
```bash
dotnet run --project src/M2.M2PortalBff --launch-profile https
```

---

### Option B — Visual Studio: Multiple Startup Projects

1. Right-click the **Solution** in Solution Explorer → **Set Startup Projects…**
2. Select **Multiple startup projects**
3. Set all four projects to **Start** in this order (drag to reorder):
   1. `M2.Platform.Api`
   2. `M2.MekaPosBff`
   3. `M2.MekaPromosBff`
   4. `M2.M2PortalBff`
4. Click **OK**, then press **F5**

> Visual Studio starts them in listed order with a brief delay, but Platform.Api's startup is fast — the BFFs should be fine. If a BFF shows connection errors on the first request, just retry; Platform.Api may still be warming up.

---

### Option C — VS Code: Compound Launch Config

Create or update `.vscode/launch.json` at the repo root with the following. This compound config starts all 4 processes together when you hit **F5** in VS Code.

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Platform.Api",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/M2.Platform.Api/bin/Debug/net9.0/M2.Platform.Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/M2.Platform.Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:5100"
      }
    },
    {
      "name": "MekaPosBff",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/M2.MekaPosBff/bin/Debug/net9.0/M2.MekaPosBff.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/M2.MekaPosBff",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7274;http://localhost:5016"
      }
    },
    {
      "name": "MekaPromosBff",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/M2.MekaPromosBff/bin/Debug/net9.0/M2.MekaPromosBff.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/M2.MekaPromosBff",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7065;http://localhost:5280"
      }
    },
    {
      "name": "M2PortalBff",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/M2.M2PortalBff/bin/Debug/net9.0/M2.M2PortalBff.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/M2.M2PortalBff",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "https://localhost:7104;http://localhost:5075"
      }
    }
  ],
  "compounds": [
    {
      "name": "M2 — All Processes",
      "configurations": [
        "Platform.Api",
        "MekaPosBff",
        "MekaPromosBff",
        "M2PortalBff"
      ],
      "stopAll": true
    }
  ]
}
```

Select **"M2 — All Processes"** from the Run & Debug dropdown and press **F5**.

---

## 6. Verify Everything Is Running

Once all processes are up, confirm each one is healthy:

| Process | Health Check URL |
|---|---|
| Platform.Api | https://localhost:5100/health |
| MekaPosBff | https://localhost:7274/health |
| MekaPromosBff | https://localhost:7065/health |
| M2PortalBff | https://localhost:7104/health |

Using `curl` (PowerShell):
```powershell
curl -k https://localhost:5100/health
curl -k https://localhost:7274/health
curl -k https://localhost:7065/health
curl -k https://localhost:7104/health
```

Each should return `200 OK` with a `Healthy` status.

---

## 7. Debugging Tips

### Swagger UI

Swagger UI is available in the `Development` environment at `/swagger` for each process:

| Process | Swagger URL |
|---|---|
| Platform.Api | https://localhost:5100/swagger |
| MekaPosBff | https://localhost:7274/swagger |
| MekaPromosBff | https://localhost:7065/swagger |
| M2PortalBff | https://localhost:7104/swagger |

Use Platform.Api's Swagger to test domain endpoints directly, bypassing the BFF layer. Authenticate requests by setting the `X-Api-Key: platform-dev-key` header.

---

### Attaching the Debugger

**Visual Studio / Rider:** Use the **Multiple Startup Projects** or compound run config (Options B/C above) — breakpoints work automatically in all attached processes.

**VS Code:** The compound launch config in Option C attaches the debugger to all 4 processes. Set breakpoints in any project.

**Attach to a running process:** If a process was started via `dotnet run` and you want to attach later:
- Visual Studio: **Debug → Attach to Process** → filter by `M2.Platform.Api` etc.
- VS Code: Add a `"request": "attach"` configuration and select the process PID.

---

### Common Startup Errors

| Error | Likely Cause | Fix |
|---|---|---|
| `Connection refused` on port 5100 | Platform.Api hasn't started yet | Start Platform.Api first and wait for `Now listening on` |
| `No connection could be made` (PostgreSQL) | Docker container not running | Run `docker start m2-postgres` or re-run the `docker run` command from Section 2 |
| `Invalid API key` from a BFF | `Platform:ApiKey` mismatch between BFF and Platform.Api | Ensure all BFF user-secrets have `Platform:ApiKey` = `platform-dev-key` |
| `Pending model changes` / migration error | New migrations not applied | Re-run `dotnet ef database update` from Section 4 |
| `HTTPS certificate not trusted` | Dev cert not installed | Run `dotnet dev-certs https --trust` |
| `AzureAd configuration missing` | BFF user-secrets not set | Set `AzureAd:TenantId`, `ClientId`, `Audience` via `dotnet user-secrets` |
| BFF returns `502 Bad Gateway` to the Flutter app | Platform.Api is down or unreachable | Check Platform.Api health endpoint; verify `Platform:BaseUrl` = `https://localhost:5100` |

---

### Useful One-Liners

```powershell
# Check all 4 health endpoints in one shot
@(5100, 7274, 7065, 7104) | ForEach-Object { 
    try { $r = Invoke-WebRequest -Uri "https://localhost:$_/health" -SkipCertificateCheck -UseBasicParsing
          Write-Host "Port $_`: $($r.StatusCode) — $($r.Content)" }
    catch { Write-Host "Port $_`: UNREACHABLE" }
}

# Tail Platform.Api logs (if running via CLI)
dotnet run --project src/M2.Platform.Api 2>&1 | Select-String -Pattern "ERR|WARN|fail"

# List EF pending migrations
dotnet ef migrations list \
  --project src/M2.Infrastructure \
  --startup-project src/M2.Platform.Api
```

---

*For architecture questions see [`docs/architecture/ARCHITECTURE.md`](../architecture/ARCHITECTURE.md).
For coding standards see [`docs/standards/CODING-STANDARDS.md`](../standards/CODING-STANDARDS.md).*
