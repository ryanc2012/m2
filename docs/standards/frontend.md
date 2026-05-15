# Frontend Documentation Standard

**Owner:** Fenster (Frontend Dev)  
**Applies to:** `apps/meka-pos`, `apps/meka-promos`, `apps/m2-portal`

> Sections are tagged **[Flutter]**, **[Blazor]**, or **[Both]** to indicate applicability.

---

## Overview

This monorepo has two frontend platforms:

| Platform | Apps | Location | Stack |
|---|---|---|---|
| **Flutter** (mobile) | `meka-pos`, `meka-promos` | `apps/` | Flutter/Dart, Riverpod, GoRouter, Material 3 |
| **Blazor** (web portal) | `m2-portal` | `apps/m2-portal/` | Blazor Server, .NET 9, MudBlazor 7, Entra ID auth |

---

## 1. Platform Overview

### 1.1 Flutter Apps (mobile) [Flutter]

| App | Path | Users | Auth |
|---|---|---|---|
| `meka-pos` | `apps/meka-pos/` | POS staff | MSAL `SingleAccountPca` (shared-device) |
| `meka-promos` | `apps/meka-promos/` | Members / consumers | MSAL `MultipleAccountPca` |

Stack: Flutter/Dart · Riverpod (state) · GoRouter (routing) · Material 3 (`ColorScheme.fromSeed`) · `msal_auth ^1.0.8`

### 1.2 Blazor Web App (portal) [Blazor]

| App | Path | Users | Auth |
|---|---|---|---|
| `m2-portal` | `apps/m2-portal/` | Managers / back-office staff | Entra ID OIDC via `Microsoft.Identity.Web` |

Stack: Blazor Server (`AddServerSideBlazor` + `MapBlazorHub` + `_Host` fallback) · .NET 9 · MudBlazor 7.x · SignalR (real-time notifications) · `M2.M2PortalBff` as backend API

All routes protected by `options.FallbackPolicy = options.DefaultPolicy` in `Program.cs`. Per-page reinforcement via `@attribute [Authorize]`.

Pages are split into `.razor` (markup) + `.razor.cs` (partial class codebehind). Layout: `MainLayout.razor` wraps every page with `MudLayout`, `MudAppBar`, and a responsive `MudDrawer` sidebar (`NavMenu.razor`).

---

## 2. Screen / Page Documentation [Both]

### 2.1 Flutter screens [Flutter]

Every screen (a `Widget` that maps to a route) gets a doc entry. Shell containers like `HomeScreen` that host tabs also count.

#### Required fields

| Field | Description |
|---|---|
| **Screen name** | Class name, e.g. `CouponsScreen` |
| **Purpose** | One sentence — what the user does here |
| **Route path** | As declared in GoRouter, e.g. `/registration/otp` |
| **Route params** | Any `extra` or path params, their types and source |
| **Entry conditions** | Auth state, prior steps, or flags required to reach this screen |
| **Demo mode behavior** | What changes when `kDemoMode == true`; write "No change" if nothing differs |

#### Widget tree overview

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

#### State management

List every Riverpod provider/notifier the screen reads or mutates. One line per provider.

| Provider | Type | Purpose |
|---|---|---|
| `myCouponsProvider` | `FutureProvider<List<Coupon>>` | Fetches coupon list; invalidated on retry tap |
| `memberSessionProvider` | `StateProvider<MemberSession?>` | Auth gate — null = unauthenticated |

### 2.2 Blazor pages [Blazor]

Every Blazor page (a `.razor` file with an `@page` directive) gets a doc entry.

#### Required fields

| Field | Description |
|---|---|
| **Page name** | Component class name, e.g. `PromotionList` |
| **Purpose** | One sentence — what the manager does here |
| **File** | `Pages/<Feature>/<Name>.razor` + `<Name>.razor.cs` |
| **Route** | `@page` directive value, e.g. `/promotions` |
| **Route params** | `@page "/promotions/{Id:guid}"` params, their types |
| **Auth** | `[Authorize]` attribute and any role/policy requirements |

#### Lifecycle methods used

Document which lifecycle hooks the page uses and why:

| Hook | Purpose |
|---|---|
| `OnInitializedAsync` | Initial data load (most pages) |
| `OnParametersSetAsync` | Re-load when route params change |
| `OnAfterRenderAsync(firstRender)` | JS interop, SignalR subscription |

#### Cascading parameters

If the page consumes cascading values (e.g. `CascadingAuthenticationState`), list them:

| Parameter | Type | Source | Purpose |
|---|---|---|---|
| `AuthenticationState` | `Task<AuthenticationState>` | `CascadingAuthenticationState` | Read current user identity |

#### Services injected

List `[Inject]` properties declared in the `.razor.cs` codebehind:

| Service | Purpose |
|---|---|
| `PromotionService` | Typed `HttpClient` → `M2PortalBff` |
| `ISnackbar` | MudBlazor toast notifications |
| `NavigationManager` | Programmatic navigation |

---

## 3. Component Documentation [Both]

### 3.1 Flutter widgets [Flutter]

A widget deserves its own doc entry when it:

- Is reused in **2 or more screens**, OR
- Has **meaningful props** (not just child or style pass-throughs), OR
- Has **distinct visual states** (loading, error, empty), OR
- Is a **business concept widget** (e.g. `CouponCard`, `MemberQrDisplay`)

Private `_` widgets local to a single screen file **do not** need doc entries unless unusually complex.

#### Props / parameters table

| Param | Type | Required | Description |
|---|---|---|---|
| `coupon` | `Coupon` | ✅ | The coupon model to render |
| `onTap` | `VoidCallback?` | ❌ | Called when card is tapped; null disables tap |

#### Visual states

Document every distinct state the widget can render:

| State | Trigger | Appearance |
|---|---|---|
| **Default (active)** | `coupon.isActive == true` | Full opacity, trailing QR icon, tappable |
| **Redeemed** | `coupon.isRedeemed == true` | 50% opacity, check icon, not tappable |
| **Expired** | `coupon.isExpired == true` | 50% opacity, clock icon, not tappable |
| **Loading** | Parent `AsyncValue` loading | `CircularProgressIndicator` shown by parent |

#### Example usage snippet

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

### 3.2 Blazor components [Blazor]

A Razor component deserves its own doc entry when it:

- Is reused in **2 or more pages**, OR
- Has **meaningful parameters** beyond simple display, OR
- Has **distinct render states** (loading, error, empty), OR
- Is a **domain concept component** (e.g. `NotificationBell`, `ApprovalStatusChip`)

#### Parameters table

| Param | Type | Attribute | Required | Description |
|---|---|---|---|---|
| `Item` | `PromotionSummary` | `[Parameter]` | ✅ | The promotion to render |
| `OnStatusChanged` | `EventCallback<PromotionStatus>` | `[Parameter]` | ❌ | Raised when status chip is clicked |
| `UserName` | `string` | `[CascadingParameter]` | auto | Injected from `CascadingAuthenticationState` |

#### Render fragments

Document any `RenderFragment` parameters used for slot-based composition:

| Fragment | Purpose |
|---|---|
| `ChildContent` | Default slot |
| `Actions` | Button row injected into footer |

#### Example usage snippet

```razor
<PromotionStatusChip Status="@item.Status"
                     OnStatusChanged="@HandleStatusChange" />
```

---

## 4. Theme & Design Tokens [Both]

### 4.1 Flutter — color usage [Flutter]

**Always reference `AppColors` names or `colorScheme` roles — never raw hex.**

| Do ✅ | Don't ❌ |
|---|---|
| `theme.colorScheme.primary` | `Color(0xFF0E7490)` |
| `AppColors.primary` | `Colors.cyan[700]` |
| `theme.colorScheme.outline` | `Color(0xFF6B7280)` |
| `theme.colorScheme.primaryContainer` | `Color(0xFFCCEBF1)` |

When documenting a screen or component, note **which color roles** are used and **why** (e.g. "error state uses `colorScheme.error`; inactive items use `colorScheme.outline` at 50% opacity").

#### AppColors reference

Defined in `apps/<app>/lib/shared/theme/app_theme.dart`.

| Token | Hex (meka-promos) | Hex (meka-pos) | Usage |
|---|---|---|---|
| `AppColors.primary` | `0xFF0E7490` (Tailwind cyan-700) | `0xFF1A237E` (deep indigo) | Primary actions, AppBar |
| `AppColors.secondary` | `0xFF155E75` (Tailwind cyan-800) | `0xFF0288D1` (blue accent) | Secondary actions |
| `AppColors.error` | `0xFFB71C1C` | `0xFFB71C1C` | Error states |
| `AppColors.surface` | `0xFFF5F5F5` | `0xFFF5F5F5` | Light mode scaffold background |
| `AppColors.darkSurface` | `0xFF121212` | `0xFF121212` | Dark mode scaffold background |

> `ColorScheme.fromSeed(seedColor: AppColors.primary).copyWith(primary: AppColors.primary, ...)` is used in both apps to pin exact brand colors while preserving the full M3 tonal palette. Dynamic color (device wallpaper) is intentionally disabled.

#### Typography scale [Flutter]

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

#### Spacing conventions [Flutter]

- Base unit: **8 dp**
- Standard card padding: `EdgeInsets.symmetric(horizontal: 16, vertical: 4)`
- Standard screen body padding: `EdgeInsets.all(16)`
- Stack gaps between related items: `SizedBox(height: 8)` or `SizedBox(height: 12)`
- Large section gaps: `SizedBox(height: 24)`
- Minimum tap target: **48 dp** (M3 default; meka-pos enforces `minimumSize: Size(200, 56)` on primary buttons)

### 4.2 Blazor — theme & CSS tokens [Blazor]

m2-portal uses **MudBlazor's built-in theme system** — no custom CSS framework. Reference MudBlazor `Color` enum values and `Typo` enum values; do not use raw hex in component markup.

| Do ✅ | Don't ❌ |
|---|---|
| `Color="Color.Primary"` | `style="color: #0e7490"` |
| `Typo="Typo.h4"` | `style="font-size: 2rem"` |
| `Severity="Severity.Warning"` | manual amber background |

**Dark mode:** MudBlazor supports `MudThemeProvider IsDarkMode`. m2-portal does not currently toggle dark mode — default light theme is active. If dark mode is added, use `MudThemeProvider @bind-IsDarkMode` wired to a user preference stored in browser `localStorage`.

**Shared design tokens:** There are no cross-platform shared tokens between Flutter and Blazor today. Brand colors are aligned manually (`AppColors.primary` ↔ MudBlazor `MudTheme.Palette.Primary`). If a shared token layer is added, document it here.

---

## 5. Navigation / Routing [Both]

### 5.1 Flutter routing [Flutter]

#### Route table format

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

#### Auth guard

- **Router-level redirect:** `GoRouter.redirect` callback; reads `memberSessionProvider`; redirects authenticated users away from onboarding routes.
- **In-screen guard:** `HomeScreen._guardedBody` checks `memberSessionProvider` for tabs that require auth (indexes 1, 2, 4); renders `_LoginPromptBody` for unauthenticated users.
- **Point-of-action guard:** `PromotionDetailScreen._getCoupon()` shows a `showModalBottomSheet` login prompt before proceeding.

#### Flow diagrams

Flow diagrams are **required** when:
- A flow has **3 or more screens** in sequence
- There are **branching paths** based on auth state or business rules

Use simple ASCII or Mermaid `flowchart LR` blocks. Keep it to the happy path + one error branch.

```
Registration flow:
/registration → (phone submitted) → /registration/otp
              → (OTP verified) → /registration/profile
              → (profile saved) → / (home)
```

### 5.2 Blazor routing [Blazor]

#### Route table format

Document all pages in a table. The authoritative source is the `@page` directive in each `.razor` file.

| Path | Component | Auth | Params | Notes |
|---|---|---|---|---|
| `/` | `Index.razor` | ✅ | — | Redirects to `/dashboard` |
| `/dashboard` | `Dashboard/Index.razor` | ✅ | — | KPI summary |
| `/promotions` | `Promotions/PromotionList.razor` | ✅ | — | |
| `/promotions/create` | `Promotions/PromotionCreate.razor` | ✅ | — | |
| `/promotions/{Id:guid}` | `Promotions/PromotionDetail.razor` | ✅ | `Id: Guid` | |
| `/promotions/{Id:guid}/edit` | `Promotions/PromotionEdit.razor` | ✅ | `Id: Guid` | Draft-only guard in codebehind |
| `/approvals` | `Approvals/...` | ✅ | — | |
| `/attendance` | `Attendance.razor` | ✅ | — | |
| `/goods-receipt` | `GoodsReceipt/...` | ✅ | — | |
| `/reporting/sales` | `Reporting/...` | ✅ | — | |
| `/reporting/attendance` | `Reporting/...` | ✅ | — | |
| `/notifications` | `Notifications/...` | ✅ | — | Notification log |

#### Auth enforcement

Auth is applied globally via `FallbackPolicy` in `Program.cs` — every route requires authentication unless explicitly overridden. Per-page `@attribute [Authorize]` is added for explicitness and role-scoping when needed.

Unauthenticated users are redirected to Entra ID login via `RedirectToLogin.razor` (wraps `NavigationManager.NavigateTo`).

#### Programmatic navigation

Use injected `NavigationManager`:

```csharp
[Inject] private NavigationManager Nav { get; set; } = null!;

Nav.NavigateTo($"promotions/{id}");
```

---

## 6. State Management [Both]

### 6.1 Flutter — Riverpod [Flutter]

Document providers per screen/feature as described in §2.1. Key patterns:

- Use `FutureProvider` for async data loads; expose `.when(data, loading, error)` in UI.
- Use `StateNotifier` for mutable state (e.g. `CartNotifier`).
- Use `StateProvider` for simple flags (e.g. `localeProvider`, `memberSessionProvider`).
- Invalidate providers on user action with `ref.invalidate(provider)`.
- Demo mode injects overrides via `ProviderScope(overrides: demoProviderOverrides)` in `main.dart`.

### 6.2 Blazor — component & service state [Blazor]

m2-portal uses Blazor Server's scoped service lifetime for state. Key patterns:

| Pattern | When to use | Example |
|---|---|---|
| **Component fields** (`private T _field`) | Local UI state (loading flags, form models) | `_loading`, `_error`, `_promotions` |
| **Scoped services** | State shared across components in a user session | `IDashboardService` |
| **Cascading values** | Data flowing down a component tree without prop-drilling | `CascadingAuthenticationState` → `AuthorizeView` |
| **SignalR** | Real-time server push (notifications, approval count) | `NotificationHubService` → `NotificationBell` |

Always call `StateHasChanged()` after mutating fields from async callbacks or SignalR handlers that run outside the render cycle.

```csharp
_hubConnection.On<int>("PendingCountUpdated", count =>
{
    _pendingCount = count;
    InvokeAsync(StateHasChanged);
});
```

---

## 7. Demo Mode [Flutter only]

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

> **Blazor (m2-portal):** N/A — the web portal uses real Entra ID auth. There is no client-side demo mode. Development testing uses actual Azure AD test accounts or local dev overrides via `appsettings.Development.json`.

---

## 8. Templates

### 8.1 Flutter screen template [Flutter]

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

### 8.2 Blazor page template [Blazor]

Copy this template for each new Blazor page or when back-filling existing ones.

````markdown
## [PageName]

**Files:** `Pages/<Feature>/<Name>.razor` + `Pages/<Feature>/<Name>.razor.cs`  
**Route:** `@page "/route/here"` (and any overloads)  
**Purpose:** One sentence describing what the manager does on this page.

### Auth

- `@attribute [Authorize]` — all users / role: `"PortalAdmin"` / policy: `"ApproverOnly"`
- Route params: `{Id:guid}` — promotion ID passed in URL

### Services injected

| Service | Purpose |
|---|---|
| `PromotionService` | Load and mutate promotion data via M2PortalBff |
| `ISnackbar` | Toast feedback |
| `NavigationManager` | Redirect after save / cancel |

### Lifecycle

| Hook | Purpose |
|---|---|
| `OnInitializedAsync` | Load data on first render |
| `OnParametersSetAsync` | Reload when `Id` route param changes |

### Component state

| Field | Type | Purpose |
|---|---|---|
| `_loading` | `bool` | Drives `MudProgressLinear` visibility |
| `_error` | `string?` | Drives `MudAlert` visibility |
| `_model` | `PromotionDetailDto?` | Bound data |

### Key MudBlazor components used

- `MudTable<T>` — data grid
- `MudChip` — status badge
- `MudAlert` — inline warning/error banners

### Notes / known issues

- ...
````

---

## See Also

- `docs/standards/CODING-STANDARDS.md` — Dart/Flutter code style rules
- `apps/meka-promos/lib/shared/theme/app_theme.dart` — AppColors + AppTheme source
- `apps/meka-pos/lib/shared/theme/app_theme.dart` — POS AppColors + AppTheme source
- `apps/meka-promos/lib/core/demo/demo_mode.dart` — kDemoMode constant
- `apps/meka-promos/lib/core/demo/demo_providers.dart` — demoProviderOverrides list
- `apps/m2-portal/Program.cs` — Blazor Server setup, service registrations, auth config
- `apps/m2-portal/Shared/MainLayout.razor` — MudBlazor layout shell
- `apps/m2-portal/Pages/` — All portal page components
