# Fenster — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Frontend Dev
- **Joined:** 2026-05-11T02:37:43.914Z

## Learnings

<!-- Append learnings below -->

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
