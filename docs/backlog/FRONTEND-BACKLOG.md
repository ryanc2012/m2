# Frontend Product Backlog
**Project:** Meka POS System  
**Author:** Fenster (Frontend Dev)  
**Date:** 2026-05-12  
**Status:** Draft

---

## Table of Contents

1. [App 1 — Meka Promotion App (Flutter)](#app-1--meka-promotion-app-flutter)
   - [Epic 1.1: App Foundation](#epic-11-app-foundation)
   - [Epic 1.2: Member Registration & Profile](#epic-12-member-registration--profile)
   - [Epic 1.3: Promotions](#epic-13-promotions)
   - [Epic 1.4: Push Notifications](#epic-14-push-notifications)
   - [Screen Inventory](#app-1-screen-inventory)
   - [Component Library](#app-1-component-library)
   - [UX Considerations](#app-1-ux-considerations)
2. [App 2 — Meka POS System (Flutter)](#app-2--meka-pos-system-flutter)
   - [Epic 2.1: App Foundation & Authentication](#epic-21-app-foundation--authentication)
   - [Epic 2.2: Attendance — Clock In/Out](#epic-22-attendance--clock-inout)
   - [Epic 2.3: Sales Transaction](#epic-23-sales-transaction)
   - [Epic 2.4: Sales Void](#epic-24-sales-void)
   - [Epic 2.5: Sales Return](#epic-25-sales-return)
   - [Epic 2.6: Goods Receipt](#epic-26-goods-receipt)
   - [Screen Inventory](#app-2-screen-inventory)
   - [Component Library](#app-2-component-library)
   - [UX Considerations](#app-2-ux-considerations)
3. [App 3 — M2 Portal (Blazor)](#app-3--m2-portal-aspnet-blazor)
   - [Epic 3.1: Portal Foundation & Authentication](#epic-31-portal-foundation--authentication)
   - [Epic 3.2: Promotion Formula Management](#epic-32-promotion-formula-management)
   - [Epic 3.3: Approval Workflow UI](#epic-33-approval-workflow-ui)
   - [Epic 3.4: Promotions Dashboard](#epic-34-promotions-overview--dashboard)
   - [Screen Inventory](#app-3-screen-inventory)
   - [Component Library](#app-3-component-library)
   - [UX Considerations](#app-3-ux-considerations)
4. [Internationalization](#internationalization)

---

## App 1 — Meka Promotion App (Flutter)

> Consumer-facing Flutter app (iOS + Android). Members browse promotions, register, and display QR coupon codes at point of sale.

---

### Epic 1.1: App Foundation

---

#### Story: As a new user, I want to see a branded splash screen on launch so that I know I'm opening the right app

**Acceptance Criteria:**
- [ ] Given the app is launched cold, when the OS opens the app, then a branded splash screen displays for 1.5–2 seconds before transitioning
- [ ] Given the splash screen is displayed, when the app finishes initialising, then it navigates to onboarding (first launch) or home (returning user)
- [ ] Given the device is low-powered, when splash animations run, then they are lightweight and do not cause jank (< 16ms frame budget)

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `SplashScreen`, `AppLogo`, `AnimatedBrandMark`  
**API Dependencies:** None (local only; checks auth token for navigation decision)

---

#### Story: As a first-time user, I want to be walked through onboarding slides so that I understand what the app does before signing up

**Acceptance Criteria:**
- [ ] Given first launch, when onboarding is displayed, then 3–4 illustrated slides explain promotions, QR redemption, and membership benefits
- [ ] Given I am on any onboarding slide, when I tap "Skip", then I am taken directly to the registration screen
- [ ] Given I reach the final slide, when I tap "Get Started", then I am taken to the registration screen
- [ ] Given I have previously completed onboarding, when the app is launched again, then onboarding is not shown

**Priority:** Should Have  
**Complexity:** M  
**Screens/Components involved:** `OnboardingScreen`, `OnboardingSlide`, `PageIndicatorDots`, `SkipButton`  
**API Dependencies:** None

---

#### Story: As a user with no internet connection, I want the app to degrade gracefully so that I am not left staring at a blank screen

**Acceptance Criteria:**
- [ ] Given there is no internet, when the home or promotions screen loads, then a friendly offline banner or screen is shown with a retry CTA
- [ ] Given I was previously online and cached promotions exist, when connectivity is lost, then cached promotions are shown with a "Last updated" timestamp
- [ ] Given connectivity is restored, when I tap "Retry" or the app detects the network, then content refreshes automatically
- [ ] Given the QR code is locally stored, when offline, then the QR code screen continues to function

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `OfflineBanner`, `OfflineScreen`, `RetryButton`, `CachedPromotionsList`  
**API Dependencies:** Local cache (SharedPreferences / Hive); Connectivity plugin

---

#### Story: As a user, I want a consistent bottom navigation bar so that I can switch between major sections quickly

**Acceptance Criteria:**
- [ ] Given I am anywhere in the app, when I tap a bottom nav item, then I navigate to that section without losing scroll position in other tabs
- [ ] Given the active section, when the bottom nav is displayed, then the active tab is visually distinguished
- [ ] Bottom nav tabs: Home/Promotions, My QR, Notifications, Profile

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `AppShell`, `BottomNavBar`, `NavBarItem`  
**API Dependencies:** None

---

### Epic 1.2: Member Registration & Profile

---

#### Story: As a new visitor, I want to register using my mobile phone number so that I can become a member and access promotions

**Acceptance Criteria:**
- [ ] Given I am on the registration screen, when I enter my Malaysian mobile number (e.g. 01x-xxxxxxx), then input is validated for format before proceeding
- [ ] Given a valid number is entered, when I tap "Send OTP", then an OTP is sent to my phone and I am taken to the OTP verification screen
- [ ] Given the OTP screen is shown, when I enter the correct 6-digit OTP within the expiry window, then registration proceeds to profile setup
- [ ] Given I enter an incorrect OTP, when I submit, then a clear error message is shown and I may retry (with rate limiting feedback)
- [ ] Given the OTP has expired, when I attempt to verify, then I am prompted to request a new OTP

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `RegistrationScreen`, `PhoneInputField`, `OtpVerificationScreen`, `OtpInputGrid`, `ResendOtpTimer`  
**API Dependencies:** `POST /auth/register/request-otp`, `POST /auth/register/verify-otp`

---

#### Story: As a new member after OTP verification, I want to complete my profile so that the system has my name and preferences

**Acceptance Criteria:**
- [ ] Given OTP is verified, when I am on the profile setup screen, then I must enter at minimum: full name and preferred language (EN/BM)
- [ ] Given I complete all required fields, when I tap "Complete Registration", then my profile is created and I am taken to the home screen
- [ ] Given the form has validation errors, when I attempt to submit, then inline errors are shown per field without clearing already-entered values

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `ProfileSetupScreen`, `NameInputField`, `LanguagePicker`, `PrimaryButton`  
**API Dependencies:** `POST /members/profile`

---

#### Story: As a returning member, I want to log in via phone OTP so that I can access my account securely without a password

**Acceptance Criteria:**
- [ ] Given I am on the login screen, when I enter my registered phone number and tap "Send OTP", then an OTP SMS is dispatched
- [ ] Given the correct OTP is entered, when I submit, then I am authenticated and taken to the home screen
- [ ] Given I am already logged in, when the app is opened, then I go directly to the home screen without re-authenticating
- [ ] Given my session expires, when I try to navigate to a protected screen, then I am redirected to the login screen with a contextual message

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `LoginScreen`, `PhoneInputField`, `OtpVerificationScreen`, `SessionExpiredOverlay`  
**API Dependencies:** `POST /auth/login/request-otp`, `POST /auth/login/verify-otp`, `POST /auth/refresh`

---

#### Story: As a logged-in member, I want to view my profile so that I can see my membership details

**Acceptance Criteria:**
- [ ] Given I navigate to profile, when the screen loads, then I see: name, phone number (masked), membership status, and member since date
- [ ] Given I am on the profile screen, when I tap "Edit", then I can update my name and language preference
- [ ] Given I tap "Log Out", when I confirm the action, then my session is cleared and I am taken to the login/registration landing screen

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `ProfileScreen`, `MembershipStatusBadge`, `EditProfileForm`, `LogoutConfirmationDialog`  
**API Dependencies:** `GET /members/me`, `PATCH /members/me`

---

#### Story: As a member, I want to see my QR code on a dedicated full-screen view so that store staff can easily scan it

**Acceptance Criteria:**
- [ ] Given I tap the QR tab, when the screen loads, then a large, high-contrast QR code is displayed occupying the majority of the screen
- [ ] Given the QR code is displayed, when viewed in bright ambient light, then the screen brightness is auto-boosted to maximum for scanning ease
- [ ] Given I am on the QR screen, when I view the code, then my name and member ID are displayed below the QR for visual confirmation
- [ ] Given the QR code has a time-limited token, when it approaches expiry, then a visible countdown timer is shown and the code auto-refreshes
- [ ] Given I tap "Refresh", when a new token is generated, then the QR code updates immediately with an animated transition

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `QrCodeScreen`, `QrWidget`, `CountdownTimer`, `MemberInfoStrip`, `BrightnessManager`  
**API Dependencies:** `GET /members/me/qr-token`

---

### Epic 1.3: Promotions

---

#### Story: As a member, I want to browse a list of active promotions so that I know what deals are currently available

**Acceptance Criteria:**
- [ ] Given I am on the promotions screen, when it loads, then I see a vertically scrollable card list of all active promotions
- [ ] Given promotions are loading, when the request is in flight, then skeleton card placeholders are shown
- [ ] Given no promotions are active, when the list is empty, then an illustrative empty state message is shown
- [ ] Given a network error occurs, when loading fails, then an error state with a retry button is shown
- [ ] Given a promotion card, when displayed, then it shows: promotion title, thumbnail image, brief description, validity end date, and a discount badge

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `PromotionsListScreen`, `PromotionCard`, `SkeletonCard`, `EmptyStateWidget`, `ErrorStateWidget`, `DiscountBadge`  
**API Dependencies:** `GET /promotions?status=active`

---

#### Story: As a member, I want to tap a promotion card and see its full details so that I understand how to redeem it

**Acceptance Criteria:**
- [ ] Given I tap a promotion card, when the detail screen opens, then it shows: full title, hero image, full description, validity period (start–end), discount type and value, redemption instructions, and applicable products/conditions
- [ ] Given the promotion requires a minimum basket value, when shown in details, then the minimum is prominently displayed
- [ ] Given I want to redeem, when I tap "Show My QR", then I am navigated to the QR code screen

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `PromotionDetailScreen`, `PromotionHeroImage`, `RedemptionInstructionsCard`, `ValidityChip`, `ShowQrButton`  
**API Dependencies:** `GET /promotions/{id}`

---

#### Story: As a member, I want to search and filter promotions so that I can find deals relevant to me quickly

**Acceptance Criteria:**
- [ ] Given the promotions screen, when I tap the search bar, then a keyboard appears and results filter as I type (debounced, 300ms)
- [ ] Given I enter a search term with no matches, when results are displayed, then an appropriate "no results" state is shown
- [ ] Given filter options (e.g., category, discount type), when I apply a filter, then only matching promotions are shown
- [ ] Given filters are active, when displayed, then active filter chips are visible with an option to clear each or all

**Priority:** Should Have  
**Complexity:** M  
**Screens/Components involved:** `SearchBar`, `FilterBottomSheet`, `FilterChip`, `NoResultsWidget`  
**API Dependencies:** `GET /promotions?status=active&q={term}&category={cat}`

---

#### Story: As a member, I want promotions to display appropriate loading, empty, and error states so that I always know what is happening

**Acceptance Criteria:**
- [ ] Given promotions are being fetched, when displayed, then skeleton loaders fill the card positions
- [ ] Given an API error occurs, when the error state renders, then the error message is human-readable (not a raw error code) with a retry option
- [ ] Given the empty state, when rendered, then it includes an encouraging illustration and message

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `SkeletonLoader`, `ErrorStateWidget`, `EmptyStateWidget`  
**API Dependencies:** `GET /promotions`

---

### Epic 1.4: Push Notifications

---

#### Story: As a user, I want to be asked for notification permission in context so that I understand why the app needs it before granting

**Acceptance Criteria:**
- [ ] Given first meaningful interaction (e.g., after registration), when the permission prompt is shown, then a pre-permission rationale screen is shown before the OS dialog
- [ ] Given the user denies permission, when the app continues, then no further OS-level prompts are triggered and the user can still use the app
- [ ] Given the user denies and later wants to enable notifications, when they navigate to Settings in-app, then a deep link to OS notification settings is offered

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `NotificationPermissionRationaleScreen`, `SettingsScreen`  
**API Dependencies:** `POST /members/me/push-token` (to register FCM/APNs token)

---

#### Story: As a member, I want to receive push notifications about new promotions even when the app is in the background so that I never miss a deal

**Acceptance Criteria:**
- [ ] Given the app is in the background, when a promotion push notification is received, then the system displays it in the notification tray
- [ ] Given the app is in the foreground, when a notification arrives, then an in-app banner is displayed at the top of the screen
- [ ] Given I tap the notification, when the app opens, then I am deep-linked to the relevant promotion detail screen
- [ ] Given a general announcement notification (no specific promotion), when tapped, then I am taken to the notifications inbox

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `InAppNotificationBanner`, `NotificationInboxScreen`  
**API Dependencies:** FCM (Android) / APNs (iOS); `POST /members/me/push-token`

---

#### Story: As a member, I want to view my notification history in an inbox so that I can review past alerts I may have missed

**Acceptance Criteria:**
- [ ] Given I navigate to the Notifications tab, when the screen loads, then a chronological list of all received notifications is shown
- [ ] Given a notification has not been read, when displayed in the list, then it has a visual unread indicator
- [ ] Given I tap a notification item, when it opens, then it is marked as read and I can navigate to the linked content if applicable
- [ ] Given there are no notifications, when the inbox loads, then an appropriate empty state is shown

**Priority:** Should Have  
**Complexity:** M  
**Screens/Components involved:** `NotificationInboxScreen`, `NotificationListItem`, `UnreadIndicatorDot`, `EmptyStateWidget`  
**API Dependencies:** `GET /members/me/notifications`, `PATCH /members/me/notifications/{id}/read`

---

#### Story: As a member, I want tapping a promotion notification to deep-link me to that promotion so that I can act on it immediately

**Acceptance Criteria:**
- [ ] Given a push notification references a promotionId, when I tap it (app closed or background), then the app opens and navigates directly to `PromotionDetailScreen` for that ID
- [ ] Given the promotion no longer exists or has expired, when the deep link resolves, then a graceful "promotion not available" message is shown rather than a crash or blank screen

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `PromotionDetailScreen`, `NotFoundScreen`  
**API Dependencies:** `GET /promotions/{id}`

---

### App 1 Screen Inventory

| # | Screen Name | Route | Notes |
|---|---|---|---|
| 1 | SplashScreen | `/splash` | Entry point |
| 2 | OnboardingScreen | `/onboarding` | First launch only |
| 3 | RegistrationScreen | `/register` | Phone number entry |
| 4 | OtpVerificationScreen | `/register/otp` | Shared with login |
| 5 | ProfileSetupScreen | `/register/profile` | Post-OTP, first time only |
| 6 | LoginScreen | `/login` | Returning member |
| 7 | HomeScreen | `/home` | Promotions list (shell tab 1) |
| 8 | PromotionDetailScreen | `/promotions/{id}` | |
| 9 | QrCodeScreen | `/qr` | Shell tab 2; full-screen QR |
| 10 | NotificationInboxScreen | `/notifications` | Shell tab 3 |
| 11 | ProfileScreen | `/profile` | Shell tab 4 |
| 12 | EditProfileScreen | `/profile/edit` | |
| 13 | OfflineScreen | `/offline` | Shown when no connectivity |
| 14 | SettingsScreen | `/settings` | Notification deep-link, language |
| 15 | NotificationPermissionRationaleScreen | Modal | Pre-OS prompt |

---

### App 1 Component Library

| Component | Description |
|---|---|
| `AppShell` | Bottom nav wrapper, route management |
| `BottomNavBar` | 4-tab navigation bar with active indicator |
| `PromotionCard` | Thumbnail, title, validity, discount badge |
| `DiscountBadge` | Pill showing "20% OFF" / "RM5 OFF" etc. |
| `SkeletonCard` | Animated shimmer placeholder |
| `EmptyStateWidget` | Illustration + message for empty lists |
| `ErrorStateWidget` | Error message + retry button |
| `OfflineBanner` | Top/bottom banner for connectivity loss |
| `QrWidget` | `qr_flutter`-based QR renderer |
| `CountdownTimer` | Tick-down display for QR token expiry |
| `OtpInputGrid` | 6-cell OTP entry field |
| `PhoneInputField` | Malaysian phone format + country code picker |
| `ResendOtpTimer` | Countdown + re-send action |
| `InAppNotificationBanner` | Top-of-screen overlay for foreground push |
| `NotificationListItem` | Inbox row with unread indicator |
| `FilterChip` | Removable filter selection chip |
| `FilterBottomSheet` | Bottom sheet with filter options |
| `MembershipStatusBadge` | Active/Inactive/Pending status pill |
| `ValidityChip` | Start–end date display |
| `PrimaryButton` | Brand-coloured full-width CTA button |
| `BrightnessManager` | Auto-brightness boost on QR screen |

---

### App 1 UX Considerations

- **Accessibility:** All interactive elements must meet WCAG 2.1 AA contrast ratios; QR screen must be high-contrast (black on white); screen reader (TalkBack/VoiceOver) labels on all custom widgets.
- **Loading states:** No screen should ever show a blank white flash. Skeleton loaders are mandatory for all async content.
- **Error states:** All network errors must surface a human-readable message and a retry CTA. Never show raw HTTP codes.
- **OTP UX:** Auto-advance focus through OTP digit cells; auto-submit on last digit entry; auto-detect SMS OTP where OS permits.
- **QR Screen:** Boost screen brightness programmatically when QR screen is active; restore on leave.
- **Haptics:** Light haptic feedback on successful OTP submission, QR refresh, and navigation tab switches.
- **Safe Areas:** All screens must respect device safe areas (notches, home indicators) especially on the full-screen QR view.
- **Language toggle:** EN/BM language switch available from settings without requiring re-login.

---

## App 2 — Meka POS System (Flutter)

> Staff-facing Flutter app (iOS + Android). Runs on a shared POS tablet. Staff clock in/out, process sales, handle voids/returns, and confirm goods receipts.

---

### Epic 2.1: App Foundation & Authentication

---

#### Story: As a store manager, I want the POS app to present a staff login screen using Azure Entra ID so that access is authenticated and auditable

**Acceptance Criteria:**
- [ ] Given the POS tablet is at the login screen, when a staff member taps "Sign In", then an Azure Entra ID browser-based SSO flow is initiated (MSAL / AppAuth)
- [ ] Given successful Azure authentication, when the token is received, then the staff member is taken to the POS home screen and their name/role is displayed
- [ ] Given an authentication failure, when the error is received, then a clear message is shown and the staff member may retry
- [ ] Given the device is a shared POS terminal, when one staff member logs out, then all session state is cleared and the login screen is shown for the next staff member

**Priority:** Must Have  
**Complexity:** L  
**Screens/Components involved:** `LoginScreen`, `StaffIdentityHeader`, `EntraIdWebView`  
**API Dependencies:** Azure Entra ID (MSAL); `POST /auth/staff/token-exchange`

---

#### Story: As a staff member, I want the POS to auto-lock after a period of inactivity so that the terminal is not left unattended and accessible

**Acceptance Criteria:**
- [ ] Given the POS is idle for a configurable duration (default: 5 minutes), when the inactivity timeout fires, then the screen locks to a PIN or re-authentication screen
- [ ] Given the screen is locked, when the correct staff PIN is entered, then the POS resumes the prior session state
- [ ] Given a different staff member needs to use the terminal, when the screen is locked, then they may choose "Switch Staff" to go through full Entra ID login

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `LockScreen`, `PinInputWidget`, `SwitchStaffButton`, `InactivityTimer`  
**API Dependencies:** Local PIN validation (encrypted); `POST /auth/staff/token-exchange`

---

#### Story: As a staff member, I want a clear POS app shell optimised for tablet landscape orientation so that the workflow is efficient and ergonomic

**Acceptance Criteria:**
- [ ] Given the POS tablet is in landscape mode, when the app shell loads, then the layout is a split-panel or side-navigation design suited for tablet use
- [ ] Given a staff member navigates between POS functions, when sections are selected, then transitions are fast (< 200ms)
- [ ] Given the POS is in use, when the device is rotated to portrait, then the app either locks to landscape or gracefully adapts the layout

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `PosAppShell`, `SideNavRail`, `StaffIdentityHeader`  
**API Dependencies:** None

---

### Epic 2.2: Attendance — Clock In/Out

---

#### Story: As a staff member, I want to clock in at the start of my shift so that my attendance is recorded accurately

**Acceptance Criteria:**
- [ ] Given I am authenticated and navigate to Attendance, when I see the Clock-In screen, then my name, current date, and time are displayed prominently
- [ ] Given I tap "Clock In", when the request is submitted, then the server timestamp is confirmed back and a success state is shown
- [ ] Given I have already clocked in today, when I view the attendance screen, then "Clock In" is disabled and my clock-in time is displayed
- [ ] Given the server is unreachable, when I attempt to clock in, then a clear error is shown and the action is not silently lost

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `ClockInScreen`, `TimeDisplay`, `StaffNameBadge`, `ConfirmationDialog`  
**API Dependencies:** `POST /attendance/clock-in`

---

#### Story: As a staff member, I want to clock out at the end of my shift so that my hours are correctly captured

**Acceptance Criteria:**
- [ ] Given I have clocked in, when I navigate to Attendance and tap "Clock Out", then a confirmation dialog shows my clock-in time, current time, and calculated duration
- [ ] Given I confirm clock-out, when the request succeeds, then a success screen shows my total hours worked for the shift
- [ ] Given I have not clocked in, when the Clock-Out button is displayed, then it is disabled with a tooltip explaining why

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `ClockOutScreen`, `ShiftSummaryCard`, `ConfirmationDialog`  
**API Dependencies:** `POST /attendance/clock-out`

---

#### Story: As a staff member, I want to see my clock-in/out history for the current period so that I can verify my own attendance record

**Acceptance Criteria:**
- [ ] Given I navigate to Attendance History, when the screen loads, then a list of my attendance records (date, clock-in, clock-out, hours) is shown for the current pay period
- [ ] Given a record is incomplete (e.g., forgot to clock out), when displayed, then it is visually flagged as incomplete

**Priority:** Should Have  
**Complexity:** S  
**Screens/Components involved:** `AttendanceHistoryScreen`, `AttendanceRecordRow`, `IncompleteRecordBadge`  
**API Dependencies:** `GET /attendance/me?period={current}`

---

### Epic 2.3: Sales Transaction

---

#### Story: As a cashier, I want to search for products by barcode scan or name so that I can add items to a transaction quickly

**Acceptance Criteria:**
- [ ] Given a transaction is open, when I scan a barcode using the device camera or external scanner, then the product is looked up and added to the cart
- [ ] Given a barcode is not recognised, when the scan result is received, then a "Product not found" message is shown without crashing
- [ ] Given I type in the search bar, when I enter product name or SKU, then matching results appear in a dropdown within 300ms (debounced)
- [ ] Given a product is found via search, when I tap it, then it is added to the cart with quantity 1

**Priority:** Must Have  
**Complexity:** L  
**Screens/Components involved:** `TransactionScreen`, `BarcodeScanner`, `ProductSearchBar`, `ProductSearchDropdown`, `CartPanel`  
**API Dependencies:** `GET /products?barcode={code}`, `GET /products?q={term}`

---

#### Story: As a cashier, I want to see the cart with line items, quantities, and prices so that I can verify the transaction before payment

**Acceptance Criteria:**
- [ ] Given items are in the cart, when the cart panel is displayed, then each line shows: product name, unit price, quantity, and line total
- [ ] Given I want to change quantity, when I tap the quantity control, then I can increment, decrement, or enter a value; minimum is 1
- [ ] Given I want to remove an item, when I swipe left or tap the delete icon on a line, then the item is removed with an undo snackbar (3-second window)
- [ ] Given items are in the cart, when any line is updated, then subtotal, discount total, and grand total update instantly

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `CartPanel`, `CartLineItem`, `QuantityControl`, `TotalsFooter`, `UndoSnackbar`  
**API Dependencies:** None (local cart state); `POST /promotions/calculate` (for discount recalculation)

---

#### Story: As a cashier, I want the system to automatically calculate and display applicable promotion discounts so that the customer receives the correct price

**Acceptance Criteria:**
- [ ] Given items are in the cart, when the promotion calculation API is called, then applicable promotions are identified and discounts shown per line and as a total
- [ ] Given a promotion applies, when shown in the cart, then the promotion name, original price, and discounted price are clearly differentiated
- [ ] Given multiple promotions could apply, when calculated, then the most beneficial valid combination is applied (per business rules from backend)
- [ ] Given a member QR has been scanned at this transaction, when promotions are calculated, then member-specific promotions are included

**Priority:** Must Have  
**Complexity:** L  
**Screens/Components involved:** `DiscountSummaryPanel`, `PromotionAppliedChip`, `CartLineItem` (with strikethrough price)  
**API Dependencies:** `POST /promotions/calculate` (send cart + memberId → get discount breakdown)

---

#### Story: As a cashier, I want to finalise a sale and send it to the ECR so that the transaction is recorded on the electronic cash register

**Acceptance Criteria:**
- [ ] Given the cart is non-empty and totals are confirmed, when I tap "Confirm & Send to ECR", then the transaction is submitted to the backend which proxies to the ECR
- [ ] Given the ECR submission is successful, when the confirmation is received, then a receipt screen is shown with transaction ID, items, discounts, and total
- [ ] Given the ECR submission fails, when the error is received, then a clear error message is shown with a retry option; the transaction is not silently lost
- [ ] Given the receipt is displayed, when shown, then there is an option to print (if printer connected) and a "New Transaction" button

**Priority:** Must Have  
**Complexity:** L  
**Screens/Components involved:** `PaymentConfirmationScreen`, `ReceiptView`, `EcrStatusIndicator`, `NewTransactionButton`  
**API Dependencies:** `POST /transactions` (includes cart, memberId, promotionIds applied); ECR integration via backend

---

#### Story: As a cashier, I want to scan a customer's member QR code during a transaction so that member-specific promotions are applied

**Acceptance Criteria:**
- [ ] Given a transaction is in progress, when I tap "Scan Member QR", then the camera activates for QR scanning
- [ ] Given a valid member QR is scanned, when the member is identified, then their name is displayed on the transaction screen and discount recalculation triggers automatically
- [ ] Given an invalid or expired QR is scanned, when validation fails, then an appropriate message is shown without disrupting the transaction

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `MemberQrScanner`, `MemberIdentityStrip`, `BarcodeScanner`  
**API Dependencies:** `POST /members/validate-qr`

---

### Epic 2.4: Sales Void

---

#### Story: As a supervisor, I want to search for a completed transaction so that I can initiate a void

**Acceptance Criteria:**
- [ ] Given I navigate to Sales Void, when the screen loads, then I see a search field for transaction ID and a list of today's recent transactions
- [ ] Given I enter a transaction ID, when I search, then the matching transaction is displayed with its details
- [ ] Given the transaction is found, when displayed, then I see: transaction ID, date/time, cashier name, items, and total

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `VoidSearchScreen`, `TransactionSearchBar`, `RecentTransactionsList`, `TransactionSummaryCard`  
**API Dependencies:** `GET /transactions/{id}`, `GET /transactions?date=today&limit=20`

---

#### Story: As a supervisor, I want to confirm a void with a reason so that the void is auditable

**Acceptance Criteria:**
- [ ] Given a transaction is selected for void, when I tap "Void Transaction", then I am shown a void confirmation screen with a mandatory reason selection
- [ ] Given I select a reason and confirm, when the void is submitted, then the transaction is marked as voided and a void confirmation screen is shown
- [ ] Given the void requires supervisor authorisation, when prompted, then a PIN or Entra ID re-authentication step is required
- [ ] Given a void is successful, when the confirmation is displayed, then the void transaction ID and timestamp are shown

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `VoidConfirmationScreen`, `VoidReasonPicker`, `AuthorisationDialog`, `VoidSuccessScreen`  
**API Dependencies:** `POST /transactions/{id}/void` (with reason and authoriser identity)

---

### Epic 2.5: Sales Return

---

#### Story: As a cashier, I want to find a transaction to return against so that I can process a refund for a customer

**Acceptance Criteria:**
- [ ] Given I navigate to Sales Return, when the screen loads, then I can search by transaction ID or scan the original receipt barcode
- [ ] Given the transaction is found, when displayed, then all line items and their returnable quantities are shown (accounting for prior partial returns)

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `ReturnSearchScreen`, `TransactionSearchBar`, `ReturnableItemsList`  
**API Dependencies:** `GET /transactions/{id}`, `GET /transactions/{id}/returnable-items`

---

#### Story: As a cashier, I want to select items to return and see the refund amount so that the customer knows what they'll receive

**Acceptance Criteria:**
- [ ] Given the transaction is displayed, when I select items to return (checkboxes with quantity spinners), then the refund amount updates in real-time
- [ ] Given promotion discounts were applied to the original transaction, when calculating the return, then the refund reflects the discounted amounts paid
- [ ] Given I have selected items, when I tap "Calculate Return", then a summary screen shows item-by-item refund and total refund amount

**Priority:** Must Have  
**Complexity:** L  
**Screens/Components involved:** `ReturnItemSelectionScreen`, `ReturnableLineItem`, `RefundCalculationSummary`  
**API Dependencies:** `POST /transactions/{id}/return/calculate`

---

#### Story: As a cashier, I want to submit a return with a reason so that the return is recorded and the refund is processed

**Acceptance Criteria:**
- [ ] Given the refund summary is shown, when I enter a return reason and tap "Confirm Return", then the return is submitted
- [ ] Given the return is successful, when confirmation is received, then a return receipt is displayed with return ID, items returned, and refund total
- [ ] Given the return fails, when an error is received, then a clear error message is shown with a retry option

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `ReturnConfirmationScreen`, `ReturnReasonInput`, `ReturnReceiptView`  
**API Dependencies:** `POST /transactions/{id}/return`

---

### Epic 2.6: Goods Receipt

---

#### Story: As a stock receiver, I want to see a list of expected deliveries for today so that I know what to look out for

**Acceptance Criteria:**
- [ ] Given I navigate to Goods Receipt, when the screen loads, then today's expected deliveries from the warehouse are listed with delivery reference, supplier, and expected item count
- [ ] Given a delivery is already fully received, when displayed, then it is visually distinguished as complete
- [ ] Given no deliveries are expected today, when the screen loads, then an appropriate empty state is shown

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `ExpectedDeliveriesScreen`, `DeliveryCard`, `EmptyStateWidget`  
**API Dependencies:** `GET /deliveries?date=today&store={storeId}`

---

#### Story: As a stock receiver, I want to view a delivery's expected items and scan/enter received quantities so that the receipt is accurately recorded

**Acceptance Criteria:**
- [ ] Given I open a delivery, when the detail screen loads, then all expected items are listed with SKU, name, expected quantity, and an input for received quantity
- [ ] Given I scan a barcode, when an item matches the delivery, then the received quantity field for that item auto-increments
- [ ] Given I manually enter a received quantity, when it differs from expected, then the row is visually flagged as a discrepancy
- [ ] Given a received quantity exceeds expected, when entered, then a warning is shown (possible input error or over-delivery)

**Priority:** Must Have  
**Complexity:** L  
**Screens/Components involved:** `DeliveryDetailScreen`, `ExpectedItemRow`, `ReceivedQtyInput`, `DiscrepancyIndicator`, `BarcodeScanner`  
**API Dependencies:** `GET /deliveries/{id}/items`

---

#### Story: As a stock receiver, I want to flag discrepancies with a note so that the warehouse is informed of any issues

**Acceptance Criteria:**
- [ ] Given a discrepancy exists (received ≠ expected), when I am about to submit, then a discrepancy review screen summarises all differences
- [ ] Given the discrepancy summary is shown, when I add a note per discrepant item, then the note is saved with the receipt record
- [ ] Given I proceed without fixing discrepancies, when I confirm, then the system accepts the receipt with discrepancies flagged for backend handling

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `DiscrepancyReviewScreen`, `DiscrepancyNoteInput`, `DiscrepancySummaryCard`  
**API Dependencies:** `POST /deliveries/{id}/receive` (with received quantities and discrepancy notes)

---

#### Story: As a stock receiver, I want to submit the goods receipt so that the inventory system is updated

**Acceptance Criteria:**
- [ ] Given all items are accounted for (or discrepancies noted), when I tap "Submit Receipt", then a final confirmation dialog is shown summarising the receipt
- [ ] Given I confirm, when the submission succeeds, then a success screen with the receipt reference number is displayed
- [ ] Given the submission fails, when the error is received, then data is not lost and I can retry

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `ReceiptSubmitConfirmationDialog`, `GoodsReceiptSuccessScreen`  
**API Dependencies:** `POST /deliveries/{id}/receive`

---

### App 2 Screen Inventory

| # | Screen Name | Route | Notes |
|---|---|---|---|
| 1 | LoginScreen | `/login` | Entra ID SSO |
| 2 | LockScreen | `/lock` | Inactivity lock |
| 3 | PosHomeScreen | `/home` | Navigation hub |
| 4 | ClockInScreen | `/attendance/clock-in` | |
| 5 | ClockOutScreen | `/attendance/clock-out` | |
| 6 | AttendanceHistoryScreen | `/attendance/history` | |
| 7 | TransactionScreen | `/transaction` | Main POS screen |
| 8 | PaymentConfirmationScreen | `/transaction/confirm` | |
| 9 | ReceiptScreen | `/transaction/receipt` | |
| 10 | VoidSearchScreen | `/void` | |
| 11 | VoidConfirmationScreen | `/void/{id}/confirm` | |
| 12 | VoidSuccessScreen | `/void/{id}/success` | |
| 13 | ReturnSearchScreen | `/return` | |
| 14 | ReturnItemSelectionScreen | `/return/{id}/items` | |
| 15 | ReturnConfirmationScreen | `/return/{id}/confirm` | |
| 16 | ReturnReceiptScreen | `/return/{id}/receipt` | |
| 17 | ExpectedDeliveriesScreen | `/goods-receipt` | |
| 18 | DeliveryDetailScreen | `/goods-receipt/{id}` | |
| 19 | DiscrepancyReviewScreen | `/goods-receipt/{id}/discrepancies` | |
| 20 | GoodsReceiptSuccessScreen | `/goods-receipt/{id}/success` | |

---

### App 2 Component Library

| Component | Description |
|---|---|
| `PosAppShell` | Tablet-optimised shell, landscape-primary |
| `SideNavRail` | Left-side navigation (Material NavigationRail) |
| `StaffIdentityHeader` | Avatar + staff name + role displayed in shell |
| `InactivityTimer` | Background timer; triggers `LockScreen` |
| `PinInputWidget` | Secure 4–6 digit PIN entry for lock screen |
| `BarcodeScanner` | Camera-based barcode/QR scanner widget |
| `ProductSearchBar` | Search with debounce and dropdown |
| `CartPanel` | Live cart with line items and totals |
| `CartLineItem` | Product name, qty control, price, remove |
| `QuantityControl` | +/- buttons + direct input |
| `TotalsFooter` | Subtotal, discount, tax, grand total |
| `DiscountSummaryPanel` | Applied promotions breakdown |
| `MemberQrScanner` | Camera overlay for scanning member QR |
| `MemberIdentityStrip` | Name badge shown after member QR scan |
| `EcrStatusIndicator` | ECR connectivity status badge |
| `ReceiptView` | Formatted receipt for display/print |
| `TransactionSummaryCard` | Compact transaction overview card |
| `TimeDisplay` | Large time display for clock-in screens |
| `AttendanceRecordRow` | Single attendance record list item |
| `ExpectedItemRow` | Item row in goods receipt detail |
| `ReceivedQtyInput` | Editable qty input with discrepancy flagging |
| `DiscrepancyIndicator` | Visual flag for quantity mismatches |
| `ConfirmationDialog` | Reusable confirm/cancel modal |
| `AuthorisationDialog` | PIN re-auth modal for sensitive actions |
| `VoidReasonPicker` | Dropdown/radio for void reasons |
| `ReturnableLineItem` | Checkbox + qty spinner for return selection |

---

### App 2 UX Considerations

- **Tablet-first layout:** The POS app must be designed for landscape tablet. Split-panel layouts (product search left, cart right) are preferred on the main transaction screen.
- **Shared device model:** Multiple staff use the same device. Session isolation on logout is critical. Auto-lock must be reliable.
- **Speed is everything:** A cashier cannot wait. Barcode scan → cart add must complete < 500ms perceived latency. Product search must feel instant.
- **Fat-finger tolerance:** All tap targets on the POS transaction screen must be at minimum 48×48dp. Key actions (Add to Cart, Confirm) should be larger.
- **Error recovery:** Network failures during a transaction must never lose cart state. Persist cart to local storage on every change.
- **ECR failure handling:** ECR outages should not prevent the transaction from being recorded server-side. Clearly communicate the partial state to the cashier.
- **Offline queue:** Clock-in/out actions should be queued locally if offline and synced when connectivity resumes.
- **Accessibility:** Role-based colour coding (e.g., void in red, receipt in green) must not be the only differentiator — use icons and text labels too.
- **Print support:** ReceiptView should be printable to a Bluetooth or USB receipt printer.

---

## App 3 — M2 Portal (ASP.NET Blazor)

> Back-office web portal for internal staff. Promotion formula management with sequential approval workflows. Material Design UI.

---

### Epic 3.1: Portal Foundation & Authentication

---

#### Story: As an internal user, I want to log in via Azure Entra ID SSO so that I can access the portal with my corporate credentials

**Acceptance Criteria:**
- [ ] Given I navigate to the portal URL unauthenticated, when the page loads, then I am redirected to the Entra ID login page
- [ ] Given I successfully authenticate with Entra ID, when redirected back, then I land on the portal dashboard with my name and role displayed
- [ ] Given my token expires, when I attempt a protected action, then a silent token refresh occurs; if refresh fails, I am redirected to login with a "session expired" message
- [ ] Given I click "Sign Out", when confirmed, then my session is cleared, Entra ID logout is triggered, and I am redirected to the login page

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `LoginRedirectPage`, `AppShellLayout`, `UserAvatarMenu`  
**API Dependencies:** Azure Entra ID (MSAL.js / Microsoft.Identity.Web); `GET /auth/me`

---

#### Story: As a user without permission to access a route, I want to see an appropriate error page so that I understand I am not authorised

**Acceptance Criteria:**
- [ ] Given I navigate to a route my role cannot access, when the page loads, then a 403-style "Access Denied" page is shown with a link back to the dashboard
- [ ] Given a route does not exist, when navigated to, then a 404 "Page Not Found" page is shown
- [ ] Given the route guard evaluates roles, when roles are fetched from claims, then access decisions are made client-side before any API call

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `AccessDeniedPage`, `NotFoundPage`, `RouteGuard`  
**API Dependencies:** `GET /auth/me` (for role claims)

---

#### Story: As a user, I want a responsive Material Design app shell with sidebar navigation so that I can navigate the portal efficiently

**Acceptance Criteria:**
- [ ] Given I am logged in, when the app shell loads, then a left sidebar shows navigation items relevant to my role
- [ ] Given I am on a wide screen (≥1280px), when the sidebar is displayed, then it is expanded with labels visible
- [ ] Given I am on a narrower screen (768–1279px), when the sidebar is displayed, then it collapses to icon-only mode
- [ ] Given I am on mobile (< 768px), when the sidebar is accessed, then it slides in as a drawer overlay
- [ ] Given the current route, when displayed in the sidebar, then the active item is highlighted

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `AppShellLayout`, `SideNavDrawer`, `SideNavItem`, `TopAppBar`, `BreadcrumbTrail`  
**API Dependencies:** None

---

### Epic 3.2: Promotion Formula Management

---

#### Story: As a promotion manager, I want to see a paginated, searchable list of promotion formulas so that I can manage them efficiently

**Acceptance Criteria:**
- [ ] Given I navigate to Promotion Formulas, when the page loads, then a data table shows: name, type, status, validity dates, created by, and actions
- [ ] Given there are more than 20 rows, when displayed, then server-side pagination controls are shown (page size options: 10, 20, 50)
- [ ] Given I type in the search bar, when I pause typing (300ms debounce), then the table filters results by name or description
- [ ] Given I click a column header, when it supports sorting, then the table re-orders by that column (ascending/descending toggle)
- [ ] Given I apply a status filter (Draft/Pending Approval/Active/Rejected/Expired), when applied, then only matching rows are shown
- [ ] Given the table is loading, when displayed, then a skeleton row loader is shown instead of a blank table

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `PromotionFormulaListPage`, `DataTable`, `SearchTextField`, `StatusFilterChips`, `PaginationControls`, `SkeletonTableRows`  
**API Dependencies:** `GET /promotions/formulas?page={n}&pageSize={n}&q={term}&status={status}&sort={col}&dir={asc|desc}`

---

#### Story: As a promotion manager, I want to create a new promotion formula so that I can define a promotion for approval

**Acceptance Criteria:**
- [ ] Given I click "Create New", when the form loads, then I see: name (required), description (required), discount type selector, discount value configuration, validity date range (date pickers), and applicability rules
- [ ] Given discount type is "Percentage", when shown, then a percentage input field (0–100%) is displayed
- [ ] Given discount type is "Fixed Amount", when shown, then a currency amount input (MYR) is displayed
- [ ] Given discount type is "Buy X Get Y", when shown, then quantity fields for X (buy) and Y (free) are displayed
- [ ] Given applicability rules, when configuring, then I can select: All Products, Specific Categories, or Specific SKUs (with a multi-select search)
- [ ] Given a minimum basket value applies, when selected, then a currency input for minimum spend is shown
- [ ] Given I submit with validation errors, when the form validates, then inline errors are displayed per field; the form does not submit
- [ ] Given I submit successfully, when the promotion is created in Draft status, then I am taken to the formula detail page with a success toast

**Priority:** Must Have  
**Complexity:** L  
**Screens/Components involved:** `CreatePromotionFormulaPage`, `DiscountTypeSelector`, `DiscountValueConfig`, `DateRangePicker`, `ApplicabilityRulesPanel`, `ProductSkuMultiSelect`, `FormValidationSummary`  
**API Dependencies:** `POST /promotions/formulas`, `GET /products/categories`, `GET /products?q={term}` (for SKU search)

---

#### Story: As a promotion manager, I want to edit a promotion formula in Draft status so that I can refine it before submitting for approval

**Acceptance Criteria:**
- [ ] Given a formula is in Draft status, when I navigate to its detail page, then an "Edit" button is visible and active
- [ ] Given I click "Edit", when the edit form loads, then all current values are pre-populated
- [ ] Given a formula is NOT in Draft status, when I view its detail page, then the "Edit" button is absent or disabled with a tooltip explaining why
- [ ] Given I save edits successfully, when the save completes, then I am returned to the detail page and a success toast is shown

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `EditPromotionFormulaPage` (reuses create form in edit mode), `PromotionFormulaDetailPage`  
**API Dependencies:** `GET /promotions/formulas/{id}`, `PUT /promotions/formulas/{id}`

---

#### Story: As a promotion manager, I want to view the full details of any promotion formula so that I can review it in read-only mode

**Acceptance Criteria:**
- [ ] Given I click a row or "View" in the list, when the detail page loads, then all formula fields are displayed in a structured read-only layout
- [ ] Given I am on the detail page, when displayed, then the approval status history timeline is shown (see Epic 3.3)
- [ ] Given the formula is in Draft, when viewed by the owner, then the "Submit for Approval" action is available

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `PromotionFormulaDetailPage`, `FormulaFieldDisplay`, `ApprovalHistoryTimeline`, `StatusBadge`  
**API Dependencies:** `GET /promotions/formulas/{id}`, `GET /promotions/formulas/{id}/approval-history`

---

#### Story: As a promotion manager, I want to delete a promotion formula in Draft status so that unused drafts do not clutter the list

**Acceptance Criteria:**
- [ ] Given a Draft formula, when I click "Delete", then a confirmation dialog is shown with the formula name
- [ ] Given I confirm deletion, when the delete succeeds, then I am returned to the list with a success toast and the formula is no longer shown
- [ ] Given the formula is not in Draft status, when displayed, then the delete action is absent

**Priority:** Should Have  
**Complexity:** S  
**Screens/Components involved:** `ConfirmDeleteDialog`, `PromotionFormulaListPage`  
**API Dependencies:** `DELETE /promotions/formulas/{id}`

---

### Epic 3.3: Approval Workflow UI

---

#### Story: As a promotion manager, I want to submit a draft promotion formula for approval so that it can be reviewed and activated

**Acceptance Criteria:**
- [ ] Given a formula is in Draft, when I click "Submit for Approval", then a confirmation dialog summarises what will be submitted
- [ ] Given I confirm, when the submission succeeds, then the formula status changes to "Pending Approval" and the submit button is replaced with a status display
- [ ] Given the approval chain is defined, when the formula is submitted, then the first approver receives a notification (in-portal and optionally email)

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `SubmitForApprovalDialog`, `PromotionFormulaDetailPage`, `StatusBadge`  
**API Dependencies:** `POST /promotions/formulas/{id}/submit`

---

#### Story: As an approver, I want to see my approval inbox so that I can act on pending approval tasks

**Acceptance Criteria:**
- [ ] Given I am an approver and log in, when I view the Approvals section, then I see a list of promotion formulas awaiting my action
- [ ] Given the inbox loads, when displayed, then each item shows: formula name, submitted by, submitted date, and urgency indicator (if any SLA is defined)
- [ ] Given I click an item, when the detail page opens, then I see all formula details and the approval action panel

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `ApprovalInboxPage`, `ApprovalTaskCard`, `PendingBadge`  
**API Dependencies:** `GET /approvals/inbox`

---

#### Story: As an approver, I want to approve or reject a promotion formula with an optional comment so that the outcome is recorded

**Acceptance Criteria:**
- [ ] Given I am viewing an approval task, when I click "Approve", then a confirmation dialog with an optional comment field is shown
- [ ] Given I click "Reject", when the dialog opens, then a mandatory rejection reason text field is shown
- [ ] Given I confirm Approve, when the action succeeds, then the formula advances to the next approver (if sequential chain continues) or becomes Active (if I am the final approver); a success toast is shown
- [ ] Given I confirm Reject, when the action succeeds, then the formula status changes to "Rejected" and the submitter is notified
- [ ] Given I have already acted on this task, when I revisit, then the action buttons are replaced with my recorded decision and timestamp

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `ApprovalActionPanel`, `ApproveDialog`, `RejectDialog`, `CommentTextField`  
**API Dependencies:** `POST /approvals/{taskId}/approve`, `POST /approvals/{taskId}/reject`

---

#### Story: As any stakeholder, I want to see the approval history timeline on a promotion formula so that I understand its full lifecycle

**Acceptance Criteria:**
- [ ] Given a formula has been through any approval steps, when the detail page loads, then a vertical timeline shows each event: submitted, approved by (name, timestamp, comment), rejected by (name, reason, timestamp), re-submitted, etc.
- [ ] Given the formula is currently pending, when the timeline is shown, then the current pending step is visually indicated (e.g., pulsing indicator or "Awaiting" label)

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `ApprovalHistoryTimeline`, `TimelineEvent`, `ApprovalStepCard`  
**API Dependencies:** `GET /promotions/formulas/{id}/approval-history`

---

#### Story: As a submitter whose promotion was rejected, I want to revise and re-submit the formula so that I can address the reviewer's concerns

**Acceptance Criteria:**
- [ ] Given a formula is in "Rejected" status, when I view its detail page, then the rejection reason is prominently displayed
- [ ] Given I click "Revise & Resubmit", when the edit form opens, then all previous values are pre-populated and I can edit them
- [ ] Given I save revisions and re-submit, when successful, then the formula re-enters the approval chain from the first approver and status changes to "Pending Approval"

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `PromotionFormulaDetailPage`, `RejectionReasonCard`, `EditPromotionFormulaPage`  
**API Dependencies:** `PUT /promotions/formulas/{id}`, `POST /promotions/formulas/{id}/submit`

---

#### Story: As a user, I want to receive in-portal notifications for approval events so that I am kept informed without needing to poll manually

**Acceptance Criteria:**
- [ ] Given an approval event occurs (submission, approval, rejection) related to me, when it fires, then a notification badge increments on the notification bell icon in the top bar
- [ ] Given I click the notification bell, when the panel opens, then a list of recent notifications is shown with type, formula name, and timestamp
- [ ] Given I click a notification, when it opens, then I am navigated to the relevant formula detail page
- [ ] Given I have read all notifications, when the panel is viewed, then the badge count clears

**Priority:** Should Have  
**Complexity:** M  
**Screens/Components involved:** `NotificationBell`, `NotificationDropdownPanel`, `NotificationItem`  
**API Dependencies:** `GET /notifications?userId=me`, SignalR or polling for real-time updates; `PATCH /notifications/{id}/read`

---

### Epic 3.4: Promotions Overview / Dashboard

---

#### Story: As a promotion manager, I want a dashboard showing key promotion metrics so that I have an at-a-glance view of the system state

**Acceptance Criteria:**
- [ ] Given I navigate to the Dashboard, when it loads, then I see metric cards for: Active Promotions count, Promotions Pending Approval count, Approvals Pending My Action count, and Expired This Month count
- [ ] Given each metric card is displayed, when I click it, then I am taken to the filtered list view showing only that category
- [ ] Given the dashboard loads, when data is being fetched, then skeleton placeholders fill the metric cards

**Priority:** Must Have  
**Complexity:** M  
**Screens/Components involved:** `DashboardPage`, `MetricCard`, `SkeletonMetricCard`  
**API Dependencies:** `GET /promotions/summary`

---

#### Story: As a user, I want quick filter tabs on the promotions list so that I can jump to relevant subsets without configuring filters manually

**Acceptance Criteria:**
- [ ] Given I am on the Promotion Formula list page, when displayed, then tab chips are shown for: All, My Drafts, Pending Approval, Active, Expired
- [ ] Given I click a tab chip, when the filter applies, then the table updates immediately to show only matching records
- [ ] Given a tab has a count (e.g., "Pending Approval (3)"), when displayed, then the count is shown in the chip label

**Priority:** Must Have  
**Complexity:** S  
**Screens/Components involved:** `QuickFilterTabs`, `FilterChip` (with count badge), `PromotionFormulaListPage`  
**API Dependencies:** `GET /promotions/formulas?status={status}` + `GET /promotions/summary` (for counts)

---

### App 3 Screen Inventory

| # | Screen Name | Route | Notes |
|---|---|---|---|
| 1 | LoginRedirectPage | `/login` | Redirects to Entra ID |
| 2 | DashboardPage | `/` | Post-login landing |
| 3 | PromotionFormulaListPage | `/promotions` | Searchable/filterable table |
| 4 | CreatePromotionFormulaPage | `/promotions/create` | |
| 5 | PromotionFormulaDetailPage | `/promotions/{id}` | Read-only + status/history |
| 6 | EditPromotionFormulaPage | `/promotions/{id}/edit` | Draft only |
| 7 | ApprovalInboxPage | `/approvals` | Approver's pending tasks |
| 8 | ApprovalTaskDetailPage | `/approvals/{taskId}` | (or modal overlay on formula detail) |
| 9 | NotificationsPage | `/notifications` | Full notification history |
| 10 | AccessDeniedPage | `/403` | |
| 11 | NotFoundPage | `/404` | |
| 12 | UserProfilePage | `/profile` | (optional) user details |

---

### App 3 Component Library

| Component | Description |
|---|---|
| `AppShellLayout` | Sidebar + top bar shell (Material) |
| `SideNavDrawer` | Responsive left navigation (full/icon/overlay) |
| `SideNavItem` | Nav link with icon, label, active state |
| `TopAppBar` | Title, notification bell, user avatar |
| `UserAvatarMenu` | Dropdown: profile, sign out |
| `BreadcrumbTrail` | Route breadcrumb for nested pages |
| `DataTable` | Sortable, paginated, searchable MudTable |
| `SkeletonTableRows` | Loading skeleton for data table |
| `PaginationControls` | Page selector with page size dropdown |
| `StatusBadge` | Coloured chip: Draft/Pending/Active/Rejected/Expired |
| `MetricCard` | Dashboard KPI tile with count + label |
| `SkeletonMetricCard` | Loading placeholder for metric |
| `QuickFilterTabs` | Filter tab row with count badges |
| `DiscountTypeSelector` | Radio group for discount type |
| `DiscountValueConfig` | Dynamic input panel per discount type |
| `DateRangePicker` | Start + end date pickers (MudDatePicker) |
| `ApplicabilityRulesPanel` | Product/category/SKU applicability config |
| `ProductSkuMultiSelect` | Async search + multi-select for SKUs |
| `FormValidationSummary` | Top-of-form error summary list |
| `ApprovalHistoryTimeline` | Vertical timeline of approval events |
| `TimelineEvent` | Single event entry with icon, actor, timestamp |
| `ApprovalActionPanel` | Approve/Reject buttons + comment field |
| `ApproveDialog` | Confirm approval with optional comment |
| `RejectDialog` | Mandatory rejection reason input |
| `SubmitForApprovalDialog` | Confirmation before workflow submission |
| `RejectionReasonCard` | Highlighted rejection reason on detail page |
| `NotificationBell` | Badge counter + dropdown trigger |
| `NotificationDropdownPanel` | Flyout list of recent notifications |
| `NotificationItem` | Single notification row |
| `ConfirmDeleteDialog` | Generic destructive action confirmation |
| `SearchTextField` | Debounced text input for table filtering |
| `RouteGuard` | Blazor auth-based route protection component |

---

### App 3 UX Considerations

- **Material Design 3 (Material You):** Use MudBlazor or a comparable MD3 component library. Maintain consistent elevation, colour system, and motion tokens.
- **Responsive design:** The portal must function at 1280px+ (primary) but degrade gracefully to 768px (tablet). Mobile (< 768px) is a secondary concern but should not be broken.
- **Form UX:** Long creation/edit forms should show a sticky "Save Draft" and "Submit" action bar at the bottom that scrolls with the user.
- **Data table:** Columns must support multi-sort, column visibility toggles, and row-click navigation. Use server-side pagination for all tables.
- **Approval workflow clarity:** Approval state must be unmistakably communicated. Use status banners (not just chips) at the top of a detail page when action is required from the viewer.
- **Notification real-time:** Use SignalR for real-time notification delivery in the portal to avoid polling. Fall back to 30-second polling if SignalR is unavailable.
- **Keyboard accessibility:** All data table actions, form fields, and dialogs must be fully keyboard-navigable. Tab order must be logical.
- **Toast notifications:** Use a top-right toast/snackbar for success/error feedback on form submissions and approvals; auto-dismiss after 5 seconds.
- **Loading guards:** No page should show empty content before loading completes. Use skeleton loaders or a loading indicator overlay.
- **WCAG 2.1 AA:** All text/background combinations must meet minimum contrast ratios. Focus outlines must be visible.

---

## Internationalization

**Target locales:** English (en-MY) and Bahasa Malaysia (ms-MY)  
**Applies to:** All three applications

### Flutter Apps (App 1 & App 2)

| Consideration | Detail |
|---|---|
| Library | `flutter_localizations` + `intl` package + ARB files |
| Language selection | User preference stored locally; settable from profile/settings screen |
| Default locale | Detect from device locale; fall back to `en-MY` |
| Currency formatting | `NumberFormat.currency(locale: ..., symbol: 'RM')` |
| Date formatting | `dd/MM/yyyy` (Malaysian standard) |
| RTL | Not required for EN or BM; no RTL layout needed |
| Dynamic content | Promotion titles and descriptions served in both languages from API (localised content strategy required from backend) |
| Strings governance | All hardcoded strings must be extracted to `.arb` files; no string literals in UI code |

### Blazor Portal (App 3)

| Consideration | Detail |
|---|---|
| Library | ASP.NET Core localisation (`IStringLocalizer`) + `.resx` resource files |
| Language selection | User preference stored in profile; cookie-based locale |
| Default locale | Browser `Accept-Language` header; fall back to `en-MY` |
| Currency formatting | `ToString("C", new CultureInfo("ms-MY"))` → RM format |
| Date formatting | `dd/MM/yyyy` consistent with Flutter apps |
| Strings governance | All UI strings in `.resx` files; no string literals in Razor components |
| i18n scope | Portal is internal (staff-only); BM is required but priority is lower than consumer apps |

---

*End of Frontend Product Backlog — v1.0 — 2026-05-12*
