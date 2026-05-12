# Fenster — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Frontend Dev
- **Joined:** 2026-05-11T02:37:43.914Z

## Learnings

<!-- Append learnings below -->

### 2026-05-12 — Sprint 2: Flutter member registration/profile/QR, Blazor approval UI delivered. See .squad/log/2026-05-12T142236Z-sprint2-complete.md.

### 2026-05-12 — Sprint 1: App Shells Created (POS, Promos, Portal)

#### What Was Built

- **meka-pos** (Flutter, POS staff): `flutter pub get` ✅. MSAL shared-device auth via `msal_auth ^1.0.8` (maps to ADR-018/019). Riverpod ProviderScope root. ZHT-only locale (ADR-022). `SingleAccountPca` from `msal_auth` is the correct API for shared-device/single-account mode — each staff login replaces the previous account on the device.
- **meka-promos** (Flutter, member/consumer): `flutter pub get` ✅. Standard (multi-account) MSAL auth via `MultipleAccountPca`. ZHT/ZHS/EN language switcher via `SegmentedButton<Locale>` and Riverpod `localeProvider`. 4-tab bottom nav (Promotions, My QR, Notifications, Profile).
- **m2-portal** (Blazor Server, managers): `dotnet build` ✅ (0 errors). MudBlazor 7.15.0 chosen as component library. Microsoft.Identity.Web upgraded to 3.8.3 to clear vulnerability warning. Sidebar nav with 5 placeholder items. All routes protected via `[Authorize]` + `CascadingAuthenticationState`.

#### Key Technical Decisions Made

1. **`msal_auth` package** chosen over `flutter_appauth` — wraps native MSAL SDK directly and exposes `SingleAccountPca` / `MultipleAccountPca` distinction cleanly. This matches our two auth modes (shared-device POS vs personal member device).
2. **`MultipleAccountPca`** for meka-promos, **`SingleAccountPca`** for meka-pos — the critical distinction between the two Flutter apps at the auth layer.
3. **MudBlazor 7.x** chosen for m2-portal over Radzen — richer free component set, strong community, MIT licence, no server-side licensing constraints.
4. **Locale as Riverpod `StateProvider`** in meka-promos — allows instant live hot-swap of locale without app restart. Will need to add `shared_preferences` persistence in Sprint 2.
5. **`flutter gen-l10n`** (`generate: true` in pubspec + `l10n.yaml`) — standard Flutter localisation toolchain. ARB files in `lib/core/l10n/`. meka-pos has one ARB (ZHT); meka-promos has three (ZHT, ZHS, EN).

#### Watch-Outs for Sprint 2

- **MSAL `msal_auth` 1.0.8**: The `SingleAccountPca.create()` and `MultipleAccountPca.create()` APIs require the `assets/msal_config.json` file to exist at build time on Android. This JSON file must be added for each app's Android assets when Azure App Registration IDs are available.
- **`msal_auth` broker (iOS)**: Set `broker: true` for POS (shared-device), `broker: false` for Promos. On iOS, broker requires Azure Authenticator app to be installed — document this for QA environments.
- **Blazor net9.0 upgrade**: Template defaulted to net7.0. Upgraded to net9.0 in csproj. If any dev sees a TFM mismatch, it's because the scaffold was from the net7 template.
- **`MainLayout.razor.css` / `NavMenu.razor.css`**: Old template CSS files retained. They're empty placeholders — MudBlazor handles styling. Safe to delete in a later cleanup sprint.



### 2026-05-12 — Frontend Backlog: UX Patterns, Architecture, Open Questions
