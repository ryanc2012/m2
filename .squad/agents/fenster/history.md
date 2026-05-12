# Fenster — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Frontend Dev
- **Joined:** 2026-05-11T02:37:43.914Z

## Learnings

<!-- Append learnings below -->

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

#### UX Patterns Identified

- **QR Code Screen:** Screen brightness must be programmatically boosted when the QR screen is active (and restored on leave). This is a common POS consumer app pattern, easily handled via Flutter's `SystemChrome.setEnabledSystemUIMode` + a brightness plugin.
- **Shared Device (POS App):** The POS runs on a shared tablet. Two critical UX decisions flow from this: (1) Auto-lock on inactivity with PIN unlock to restore session, (2) Full Entra ID re-auth for "Switch Staff" to ensure no bleed-over of session state between users.
- **OTP Flow:** Auto-advance focus between OTP digit cells, auto-submit on final digit, and SMS OTP auto-detection (Android SMS Retriever API / iOS AutoFill) are table-stakes for a Malaysian consumer app. Without these, registration friction is too high.
- **Skeleton Loaders:** Every async data surface must use skeleton/shimmer placeholders. Blank screens or spinners alone are not acceptable. This applies uniformly to all three apps.
- **Error States:** All three apps must surface human-readable error messages with retry CTAs. Raw HTTP codes must never appear in UI.
- **Offline degradation (Promotion App):** Cache promotions locally (Hive or SharedPreferences). Serve stale data with a "Last updated" badge when offline. QR code must work offline (locally stored token until expiry).
- **Cart persistence (POS App):** Cart state must be written to local storage on every mutation. A network failure mid-transaction must never lose the cart.
- **Approval state prominence (M2 Portal):** Status badges alone are insufficient for approval screens. Use full-width status banners at the top of detail pages when the viewer's action is required.
- **Bottom nav (Promotion App):** 4-tab bottom nav — Promotions, My QR, Notifications, Profile. Keep it simple; no drawer needed for this consumer app.
- **Tablet-first POS layout:** Split-panel landscape: product search/scan on left, cart on right. All tap targets ≥ 48×48dp. Avoid cramped portrait-only designs.

#### Component Architecture Insights

- **Shared widget conventions:** All three apps need an `EmptyStateWidget` and `ErrorStateWidget` pattern. In Flutter apps, these should be widget classes accepting illustration, title, body, and optional CTA callback. In Blazor, a reusable `<EmptyState>` and `<ErrorState>` render fragment component.
- **Discount badge / type config:** The discount type configuration in the M2 Portal create/edit form is a dynamic panel that swaps fields based on the selected discount type (Percentage / Fixed / Buy-X-Get-Y). Implement as a single `DiscountValueConfig` component that renders conditionally — avoids duplication and keeps validation logic in one place.
- **Approval timeline:** The `ApprovalHistoryTimeline` component is a standalone read-only component used on the formula detail page. It should accept a flat list of `ApprovalEvent` objects and render a styled vertical timeline with icons per event type (submitted, approved, rejected, resubmitted).
- **Barcode scanner widget:** Both App 1 (member QR scan in POS) and App 2 use camera-based barcode scanning. Extract `BarcodeScanner` as a shared Flutter widget (via `mobile_scanner` package) so the same implementation is reused.
- **Reusable ConfirmationDialog:** Both Flutter apps have multiple confirmation dialogs (clock-in confirm, void confirm, return confirm, goods receipt submit). Centralise into a single configurable `ConfirmationDialog` widget with title, body, confirm label, and cancel label parameters.

#### Open Questions for the Team

1. **Promotion App — member QR token lifetime:** What is the server-defined QR token expiry? This affects the countdown timer design and whether offline QR use is feasible at all after expiry.
2. **POS App — Entra ID on shared device:** Azure Entra ID SSO typically assumes a personal device. For a shared POS tablet, we likely need ROPC (Resource Owner Password Credentials) or a device-code flow — or a brokered auth approach. This needs to be resolved before Epic 2.1 starts. **Backend/Auth input needed.**
3. **POS App — ECR integration protocol:** What protocol does the ECR use? Serial/COM port, TCP socket, or HTTP? The Flutter app needs to know how to communicate with the ECR device locally or via backend proxy. **Backend/infra input needed.**
4. **M2 Portal — approval chain depth:** Is the approval chain always two levels (submitter → approver → done) or is it configurable and variable depth? This significantly affects the `ApprovalHistoryTimeline` and `ApprovalActionPanel` complexity.
5. **i18n — API content in BM:** Do promotion titles, descriptions, and discount rules need to be returned in both EN and BM from the API? If yes, the API contract needs a localised content strategy. **Backend input needed.**
6. **Notification delivery in portal:** Real-time (SignalR) vs. polling — has this been decided? SignalR is preferred but requires infra planning.
7. **M2 Portal receipt printer support (POS App):** Which Bluetooth/USB receipt printer models are in scope? This affects how the `ReceiptView` component generates printable output.
8. **Promotions — discount stacking rules:** Can multiple promotions apply to a single transaction? Is this controlled by the backend's `POST /promotions/calculate` endpoint? The UI needs to know whether to show a single discount or a breakdown of stacked discounts.

#### Files Written
- `docs/backlog/FRONTEND-BACKLOG.md` — comprehensive frontend product backlog, all three apps

### 2026-05-12 — Cross-Agent Context (from Initial Planning Session)

**From Keyser (Architecture) — resolves open questions:**
- **D7 (SignalR) resolved:** ADR-005 confirms Notification module uses SignalR for Blazor web. Hub co-located with `M2PortalBff`. `NotificationBell` and `NotificationDropdownPanel` can be implemented against SignalR — no polling fallback needed unless SignalR unavailable.
- **Riverpod confirmed (ADR-007):** Both Flutter apps must use Riverpod exclusively. All state management patterns (async data loading, QR expiry countdown, cart persistence) should use Riverpod providers and `AsyncValue`.

**From McManus (Backend Dev) — resolves open questions:**
- **D3 (QR token) partially resolved:** BE-REC-001 R5 establishes signed JWTs with 5-minute TTL for coupon QR codes. Apply the same assumption to the member QR code (`QrCodeScreen` countdown timer) until OQ-11 is formally decided. Design the countdown timer around a 5-minute (300s) window.
- **D4 (discount stacking):** McManus recommends on-demand coupon issuance (lazy on first `GET /members/me/coupons`). Stacking rules (OQ-12) still pending — design `DiscountSummaryPanel` to support both single and stacked discount display; backend will confirm via the `POST /promotions/calculate` response shape.
- **D2 (ECR protocol):** Same as OQ-01; unresolved. `EcrStatusIndicator` design should plan for backend-proxy mode as the default assumption (HTTP/REST via backend) pending client confirmation.

**From Verbal (Tester):**
- flutter_test is confirmed for unit/widget tests; Playwright for Blazor e2e. Skeleton loader and error state patterns (documented in Fenster's learnings) must have corresponding widget test coverage — test the empty/error render paths, not just the happy path.
