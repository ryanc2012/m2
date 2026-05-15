# Frontend Documentation Standard

**Owner:** Fenster (Frontend Dev)  
**Applies to:** `apps/meka-pos`, `apps/meka-promos`  
**Stack:** Flutter / Dart, Riverpod, GoRouter, Material 3

---

## 1. Screen / Page Documentation

Every screen (a `Widget` that maps to a route) gets a doc entry. Shell containers like `HomeScreen` that host tabs also count.

### Required fields

| Field | Description |
|---|---|
| **Screen name** | Class name, e.g. `CouponsScreen` |
| **Purpose** | One sentence — what the user does here |
| **Route path** | As declared in GoRouter, e.g. `/registration/otp` |
| **Route params** | Any `extra` or path params, their types and source |
| **Entry conditions** | Auth state, prior steps, or flags required to reach this screen |
| **Demo mode behavior** | What changes when `kDemoMode == true`; write "No change" if nothing differs |

### Widget tree overview

List **key structural widgets** only — not every `Padding` or `SizedBox`. Aim for 5–10 lines. Focus on layout skeleton and the widgets that carry state or user interaction.

```
Scaffold
  AppBar — title, optional action icons
  body: AsyncValue.when(...)
    loading → CircularProgressIndicator
    error   → retry Column
    data    → ListView of _CouponCard
  BottomNavigationBar (if shell screen)
```

### State management

List every Riverpod provider/notifier the screen reads or mutates. One line per provider.

| Provider | Type | Purpose |
|---|---|---|
| `myCouponsProvider` | `FutureProvider<List<Coupon>>` | Fetches coupon list; invalidated on retry tap |
| `memberSessionProvider` | `StateProvider<MemberSession?>` | Auth gate — null = unauthenticated |

---

## 2. Component Documentation

A widget deserves its own doc entry when it:

- Is reused in **2 or more screens**, OR
- Has **meaningful props** (not just child or style pass-throughs), OR
- Has **distinct visual states** (loading, error, empty), OR
- Is a **business concept widget** (e.g. `CouponCard`, `MemberQrDisplay`)

Private `_` widgets local to a single screen file **do not** need doc entries unless unusually complex.

### Props / parameters table

| Param | Type | Required | Description |
|---|---|---|---|
| `coupon` | `Coupon` | ✅ | The coupon model to render |
| `onTap` | `VoidCallback?` | ❌ | Called when card is tapped; null disables tap |

### Visual states

Document every distinct state the widget can render:

| State | Trigger | Appearance |
|---|---|---|
| **Default (active)** | `coupon.isActive == true` | Full opacity, trailing QR icon, tappable |
| **Redeemed** | `coupon.isRedeemed == true` | 50% opacity, check icon, not tappable |
| **Expired** | `coupon.isExpired == true` | 50% opacity, clock icon, not tappable |
| **Loading** | Parent `AsyncValue` loading | `CircularProgressIndicator` shown by parent |

### Example usage snippet

Always include a minimal, copy-pasteable example:

```dart
_CouponCard(
  coupon: Coupon(
    id: 'c-001',
    promotionNameZht: '九折優惠',
    status: CouponStatus.active,
    expiresAt: DateTime(2025, 12, 31),
  ),
)
```

---

## 3. Theme & Design Tokens

### Color usage

**Always reference `AppColors` names or `colorScheme` roles — never raw hex.**

| Do ✅ | Don't ❌ |
|---|---|
| `theme.colorScheme.primary` | `Color(0xFF0E7490)` |
| `AppColors.primary` | `Colors.cyan[700]` |
| `theme.colorScheme.outline` | `Color(0xFF6B7280)` |
| `theme.colorScheme.primaryContainer` | `Color(0xFFCCEBF1)` |

When documenting a screen or component, note **which color roles** are used and **why** (e.g. "error state uses `colorScheme.error`; inactive items use `colorScheme.outline` at 50% opacity").

### AppColors reference

Defined in `apps/<app>/lib/shared/theme/app_theme.dart`.

| Token | Hex (meka-promos) | Hex (meka-pos) | Usage |
|---|---|---|---|
| `AppColors.primary` | `0xFF0E7490` (Tailwind cyan-700) | `0xFF1A237E` (deep indigo) | Primary actions, AppBar |
| `AppColors.secondary` | `0xFF155E75` (Tailwind cyan-800) | `0xFF0288D1` (blue accent) | Secondary actions |
| `AppColors.error` | `0xFFB71C1C` | `0xFFB71C1C` | Error states |
| `AppColors.surface` | `0xFFF5F5F5` | `0xFFF5F5F5` | Light mode scaffold background |
| `AppColors.darkSurface` | `0xFF121212` | `0xFF121212` | Dark mode scaffold background |

> `ColorScheme.fromSeed(seedColor: AppColors.primary).copyWith(primary: AppColors.primary, ...)` is used in both apps to pin exact brand colors while preserving the full M3 tonal palette. Dynamic color (device wallpaper) is intentionally disabled.

### Typography scale

Use M3 text roles. Do not define custom `TextStyle` values for things M3 already covers.

| Role | Example usage |
|---|---|
| `textTheme.titleLarge` | Screen headings |
| `textTheme.titleMedium` | Card titles, section headers |
| `textTheme.bodyMedium` | List body text (default) |
| `textTheme.bodySmall` | Subtitles, timestamps, secondary labels |
| `textTheme.labelLarge` | Section headers in lists (e.g. "有效優惠券") |
| `textTheme.labelSmall` | Chips, badges, demo banner text |

When documenting typography decisions, reference the role name, not point sizes.

### Spacing conventions

- Base unit: **8 dp**
- Standard card padding: `EdgeInsets.symmetric(horizontal: 16, vertical: 4)`
- Standard screen body padding: `EdgeInsets.all(16)`
- Stack gaps between related items: `SizedBox(height: 8)` or `SizedBox(height: 12)`
- Large section gaps: `SizedBox(height: 24)`
- Minimum tap target: **48 dp** (M3 default; meka-pos enforces `minimumSize: Size(200, 56)` on primary buttons)

---

## 4. Navigation / Routing

### Route table format

Document all routes in a table. The authoritative source is `app.dart` (GoRouter config).

| Path | Screen | Auth required | Params | Notes |
|---|---|---|---|---|
| `/` | `HomeScreen` | ❌ (tabs 1, 2, 4 gated in-screen) | — | Initial location |
| `/registration` | `RegistrationScreen` | ❌ | — | First step of sign-up flow |
| `/registration/otp` | `OtpVerificationScreen` | ❌ | `extra: String` (phone) | Phone passed via GoRouter `extra` |
| `/registration/profile` | `ProfileSetupScreen` | ❌ | `extra: String` (verification token) | Token from OTP step |
| `/login` | `LoginScreen` | ❌ | — | |
| `/login/otp` | `LoginOtpScreen` | ❌ | `extra: Map<String,String>` (phone, memberId) | |
| `/profile/edit` | `EditProfileScreen` | ✅ | — | |

### Auth guard

Document how auth is enforced:

- **Router-level redirect:** `GoRouter.redirect` callback; reads `memberSessionProvider`; redirects authenticated users away from onboarding routes.
- **In-screen guard:** `HomeScreen._guardedBody` checks `memberSessionProvider` for tabs that require auth (indexes 1, 2, 4); renders `_LoginPromptBody` for unauthenticated users.
- **Point-of-action guard:** `PromotionDetailScreen._getCoupon()` shows a `showModalBottomSheet` login prompt before proceeding.

### Flow diagrams

Flow diagrams are **required** when:
- A flow has **3 or more screens** in sequence (e.g. registration: phone → OTP → profile setup)
- There are **branching paths** based on auth state or business rules

Flow diagrams are **optional** for:
- Single-screen actions (tap → result)
- Flows already covered by the route table above

Use simple ASCII or Mermaid `flowchart LR` blocks. Keep it to the happy path + one error branch.

```
Registration flow:
/registration → (phone submitted) → /registration/otp
              → (OTP verified) → /registration/profile
              → (profile saved) → / (home)
```

---

## 5. Demo Mode

Demo mode is enabled with `flutter run --dart-define=DEMO_MODE=true`. The constant `kDemoMode` (in `lib/core/demo/demo_mode.dart`) is `false` by default.

### How demo mode works (system-level)

- `ProviderScope` in `main.dart` injects `demoProviderOverrides` when `kDemoMode == true`
- Static fixture data replaces all network calls via `DemoRegistrationService`, `DemoProfileService`, etc.
- The router auth guard sees a pre-seeded `memberSessionProvider` and treats the user as authenticated
- `DemoBanner` (amber bar) is injected at the top of every screen via `MaterialApp.router.builder`

### Per-screen demo documentation

For each screen, document:

| Field | Description |
|---|---|
| **Has demo data** | Yes / No |
| **Data source** | Provider override name in `demo_providers.dart` (e.g. `myCouponsProvider`) |
| **Sample data summary** | What the demo fixtures show (e.g. "3 coupons: 1 active, 1 redeemed, 1 expired") |
| **Behavioral differences** | Any UI hints or bypass logic (e.g. "OTP screen shows 🎭 hint text") |

If a screen shows no demo-specific changes, write: **Demo mode: No change — screen is available to unauthenticated users or uses no live data.**

---

## 6. Screen Documentation Template

Copy this template for each new screen or when back-filling existing ones.

```markdown
## [ScreenName]

**File:** `lib/features/<feature>/<screen_name>_screen.dart`  
**Route:** `/path/here`  
**Purpose:** One sentence describing what the user does on this screen.

### Entry conditions

- Auth state: authenticated / unauthenticated / either
- Required prior step: (e.g. "must complete OTP verification")
- Other: (flags, device type, etc.)

### Route params

| Param | Type | Source | Description |
|---|---|---|---|
| `extra` | `String` | Previous screen | Phone number from registration form |

_None_ (if no params)

### Widget tree overview

```
Scaffold
  AppBar — ...
  body:
    ...
```

### State management

| Provider | Type | Purpose |
|---|---|---|
| `exampleProvider` | `FutureProvider<T>` | ... |

### Theme / colors used

- `colorScheme.primary` — ...
- `colorScheme.outline` — ...

### Demo mode

| Field | Value |
|---|---|
| Has demo data | Yes / No |
| Data source | `demoProviderOverrides` → `xyzProvider` |
| Sample data | Brief description of fixtures |
| UI differences | Any hint text, bypasses, or visual changes |

### Notes / known issues

- ...
```

---

## See Also

- `docs/standards/CODING-STANDARDS.md` — Dart/Flutter code style rules
- `apps/meka-promos/lib/shared/theme/app_theme.dart` — AppColors + AppTheme source
- `apps/meka-pos/lib/shared/theme/app_theme.dart` — POS AppColors + AppTheme source
- `apps/meka-promos/lib/core/demo/demo_mode.dart` — kDemoMode constant
- `apps/meka-promos/lib/core/demo/demo_providers.dart` — demoProviderOverrides list
