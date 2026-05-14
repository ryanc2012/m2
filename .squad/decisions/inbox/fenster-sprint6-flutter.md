# Fenster — Sprint 6 Flutter Implementation Decisions

**Sprint:** 6 | **Stories:** S6.5, S6.6, S6.7, S6.8  
**Date:** 2026-05-14

---

## Decision 1: API key injected via --dart-define (meka-promos)

The meka-promos app API key is read at compile time via `String.fromEnvironment('API_KEY', defaultValue: 'meka-promos-dev-key')`. The key is never hardcoded in source. CI/CD injects the real key with `--dart-define=API_KEY=<secret>` at build time.

**Rejected:** Storing the key in a config file committed to source — exposes the secret.

---

## Decision 2: MSAL token caching via `_cachedToken` field (meka-pos)

The meka-pos API client maintains a module-level `String? _cachedToken` field. `AuthService` calls `setAuthToken(token)` after a successful MSAL acquisition. The Dio interceptor attaches the Bearer token on every request.

Full silent-refresh logic (detect 401 → call `acquireTokenSilently` → retry request) is deferred to Sprint 7 — doing it now would require circular dependencies between `ApiClient` and `AuthService` that aren't justified pre-Sprint 7.

**Rejected:** Full `ref`-based auth interceptor — creates circular Provider dependency with `authServiceProvider`.

---

## Decision 3: `clearCart()` added to CartNotifier post-checkout (meka-pos)

`CartNotifier` now exposes a `clearCart()` method (mirrors the existing `clear()` method) per the sprint spec. `PaymentScreen._completeSale()` calls `clearCart()` after a successful transaction before navigating to `ReceiptScreen`.

**Why both exist:** `clear()` is kept for internal/legacy call sites; `clearCart()` is the canonical public API per the sprint requirement.

---

## Decision 4: Coupon QR uses `coupon.code` as QR data (meka-promos)

`CouponDetailScreen` renders `QrImageView(data: coupon.code, ...)`. The `code` field is a plain string from the BFF response (e.g. `MEKA-2026-ABCD1234`), not a signed JWT.

Signed JWT coupon codes (where the QR encodes a server-signed token that POS can verify offline) are deferred to Sprint 7 per the security roadmap.

**Rejected for now:** JWT-signed QR — requires Sprint 7 key distribution and POS-side verification logic.

---

## Files changed (S6.5–S6.8)

### meka-pos
- `lib/core/api/api_client.dart` — base URL from env, `_cachedToken` + `setAuthToken()`, MSAL Bearer interceptor
- `lib/features/sales/cart_provider.dart` — added `clearCart()`
- `lib/features/sales/payment_screen.dart` — calls `clearCart()` post-checkout
- `lib/features/sales/sales_service.dart` — paths prefixed `/api/v1/`
- `lib/features/attendance/attendance_service.dart` — new `AttendanceStatus` model, `getStatus()`, timestamp in clock-in/out, `/api/v1/` paths
- `lib/features/attendance/clock_in_out_screen.dart` — staffId from `authStateProvider`, loads status on init, shows hours worked
- `lib/features/returns/return_service.dart` — paths prefixed `/api/v1/`
- `lib/features/member_lookup/member_lookup_service.dart` — path prefixed `/api/v1/`
- `lib/services/goods_receipt_service.dart` — path updated to `/api/v1/`

### meka-promos
- `lib/core/api/api_client.dart` — base URL from env, `_kApiKey` from env, X-Api-Key interceptor
- `lib/features/registration/registration_service.dart` — `/api/v1/` paths, added `findMemberByPhone`, `generateOtpById`, `validateOtpById`
- `lib/features/promotions/promotions_service.dart` — paths prefixed `/api/v1/`
- `lib/features/coupons/coupons_service.dart` — paths prefixed `/api/v1/`
- `lib/features/profile/profile_service.dart` — paths prefixed `/api/v1/`
- `lib/features/login/login_screen.dart` — replaced MSAL login with phone+OTP login
- `lib/features/login/login_otp_screen.dart` — new; OTP verification for login flow
- `lib/app.dart` — added `/login` and `/login/otp` routes, updated redirect guard
- `lib/features/registration/registration_screen.dart` — added "Already a member? Login" link
- `lib/core/l10n/app_en.arb` / `app_zht.arb` / `app_zhs.arb` — added `loginTitle`, `newMemberRegister` keys
