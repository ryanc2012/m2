# Fenster — Sprint 2 UI Component Decisions

**Date:** 2026-05-12  
**Author:** Fenster (Frontend Dev)  
**Sprint:** 2 — Member Registration UI + Approval Workflow UI

---

## meka-promos (Flutter)

### D1: `qr_flutter` for QR Code Display
`qr_flutter 4.1.0` chosen. `QrImageView(data: profile.qrCode, size: 220, backgroundColor: Colors.white)` inside a white container with drop shadow. The `qrCode` field contains a reference ID (per ADR-019 — server-side lookup, not a locally-verifiable JWT). QR size 220dp is sufficient for typical POS scanner distance; will increase if field testing shows scan failures.

**Rejected:** `pretty_qr_code` (less maintained), manual Canvas painting (unnecessary complexity).

### D2: `memberSessionProvider` as Auth Anchor (not MSAL `authStateProvider`)
The consumer registration flow is phone-OTP-based, separate from Entra ID / MSAL. Rather than shoehorning the OTP-completion signal into `authStateProvider` (MSAL), a dedicated `StateProvider<MemberProfile?>` named `memberSessionProvider` was introduced in `profile_service.dart`. This is the single source of truth for "is the consumer logged in?" in the go_router redirect and HomeScreen sign-out.

**Implication:** Sprint 3 should add `shared_preferences` persistence so `memberSessionProvider` survives app restarts without requiring re-registration.

### D3: go_router `_RouterRefresher` Bridge
`GoRouter` requires a `Listenable` (`refreshListenable`) to know when to re-evaluate redirect logic. Since Riverpod providers are not `Listenable`, a minimal `_RouterRefresher extends ChangeNotifier` class is instantiated alongside the router. `ref.listen(memberSessionProvider, ...)` calls `refresh()` in the `build()` method of the `ConsumerStatefulWidget`. This is the standard pattern for Riverpod + go_router without introducing a heavy `RouterNotifier`.

### D4: OTP Paste Handling via `maxLength: 6` on First Cell
The first OTP cell accepts up to 6 characters. `_onDigitChanged` checks `value.length > 1` and distributes digits across all 6 `TextEditingController`s with `FilteringTextInputFormatter.digitsOnly`. Subsequent cells cap at 1 character for normal key-by-key entry with auto-advance. This supports both manual typing and clipboard paste from SMS auto-complete (Android/iOS).

### D5: Bilingual Name Field Layout (ZHT row, EN row)
ZHT name uses last-first order (姓 + 名) — matching Chinese convention. EN name uses first-last order. Each pair is rendered as a 2-column `Row`. The `profile_setup_screen` and `edit_profile_screen` both use this layout. Labels come from ARB keys to respect locale.

### D6: ARB Key Naming Convention
New keys follow `camelCase` consistent with existing Sprint 1 keys. Parametrised keys (e.g., `otpSentTo`, `resendIn`) use `@placeholder` syntax in all three ARB files (EN, ZHT, ZHS). `resendIn` uses `int` type for the seconds parameter so `intl` can potentially apply plural rules in future locales.

---

## m2-portal (Blazor)

### D7: `ApprovalDetailDto` to Avoid Namespace Collision
Blazor generates a `partial class ApprovalDetail` from `ApprovalDetail.razor`. The service layer also had `record ApprovalDetail(...)`. The collision caused 14 build errors. Renamed to `ApprovalDetailDto` in `ApprovalService.cs`. ADR-008 code-behind pattern is preserved; only the DTO name changes. No functional impact.

### D8: Full-Width Status Banner for Pending Approvals
`ApprovalDetail.razor` shows a `MudAlert Severity.Warning` banner at the top of the page when `_detail.Status == ApprovalStatus.Pending`. This implements the "approval state prominence" UX pattern from Fenster's Sprint 1 learnings. Action buttons are also disabled when status is not Pending to prevent double-action.

### D9: `MudTimeline` for Approval Step History
`TimelinePosition.Start` used — keeps text on the right of the dot, consistent with MudBlazor convention. Each `MudTimelineItem` colour maps to the step's `ApprovalStatus` (Success/Error/Info/Warning). Comment text is displayed in quotes under the status line. No events = "No steps recorded yet" empty state.

### D10: Inline Row Edit in ApprovalPolicySettings
Rather than a modal dialog or separate edit route, `ApprovalPolicySettings` uses an in-row edit pattern: clicking "Edit" sets `_editingEntityType`, swapping the display cells with input controls (`MudSelect` for mode, `MudNumericField` for levels). Save/Cancel buttons are inline. The `MudNumericField Dense` attribute was removed after MUD0002 analyser warning — not a supported attribute on that component in MudBlazor 7.x.

### D11: NavMenu Approvals Badge (Static)
`MudBadge` with `Visible="@(_pendingCount > 0)"` and `Content="@_pendingCount"`. Currently `_pendingCount = 0` (hardcoded). Sprint 3 will wire this to a SignalR hub push or a short-lived timer poll of the approval count endpoint. The badge infrastructure is in place and ready.

### D12: `ApprovalService` HttpClient Registration
Registered via `AddHttpClient<ApprovalService>(client => ...)` in `Program.cs`. Base URL sourced from `M2PortalBff:BaseUrl` config key with `https://localhost:5001` fallback. This follows the typed `HttpClient` pattern consistent with the rest of the BFF client layer.

---

## Open Questions Raised

- **OQ-FE-01:** Should `ApprovalPolicySettings` be restricted to an admin role only? The page is `[Authorize]` but not role-gated. Needs decision from Keyser/McManus before Sprint 3.
- **OQ-FE-02:** QR code expiry countdown on ProfileScreen — not yet implemented. Awaiting OQ-11 resolution (member QR token lifetime). Design should use a 5-minute (300s) window per McManus' BE-REC-001 R5 assumption.
- **OQ-FE-03:** SMS OTP auto-read on Android (SMS Retriever API) — requires a package (`sms_autofill`) and backend SMS message format alignment. Backend must format the OTP SMS with the app hash. Raise with McManus for Sprint 3.
