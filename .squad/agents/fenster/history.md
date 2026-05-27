# Fenster — History

## Core Context

- **Project:** A Point of Sale (POS) system for managing transactions, inventory, and sales operations.
- **Role:** Frontend Dev
- **Joined:** 2026-05-11T02:37:43.914Z

## Recent Learnings

### 2026-05-15 — Frontend Documentation Standards & Theme Updates

Created `docs/standards/frontend.md` covering Flutter and Blazor as unified structure with platform-specific sections. Fixed theme color handling: use `.copyWith()` after `ColorScheme.fromSeed()` to lock exact primary/secondary hex values. Disabled dynamic color in meka-promos. Changed meka-promos theme to dark cyan (`0xFF0E7490`). Added demo OTP hint text and profile/promo detail providers.
- **`msal_auth` broker (iOS)**: Set `broker: true` for POS (shared-device), `broker: false` for Promos. On iOS, broker requires Azure Authenticator app to be installed — document this for QA environments.
- **Blazor net9.0 upgrade**: Template defaulted to net7.0. Upgraded to net9.0 in csproj. If any dev sees a TFM mismatch, it's because the scaffold was from the net7 template.
- **`MainLayout.razor.css` / `NavMenu.razor.css`**: Old template CSS files retained. They're empty placeholders — MudBlazor handles styling. Safe to delete in a later cleanup sprint.



### 2026-05-12 — Frontend Backlog: UX Patterns, Architecture, Open Questions


### 2026-05-12 — Sprint 3: POS Core Flows, Member Promotions/Coupons, Portal Promotion Management

#### What Was Built

**meka-pos (Flutter POS)**
- **`features/sales/cart_provider.dart`** — `CartItem`, `CartState`, `CartNotifier` (Riverpod `StateNotifier`). Cart state: items, discount, optional member association. Methods: addItem, removeItem, updateQuantity, applyDiscount, setMember, clearMember, clear.
- **`features/sales/cart_screen.dart`** — Horizontal stub product grid (6 items), cart item list with +/– qty controls, member QR bottom-sheet, member banner, footer total + "前往付款" button.
- **`features/sales/payment_screen.dart`** — Order summary card, RadioListTile method selector (Cash/Card/QR Pay), SalesService.createTransaction() call, navigate to ReceiptScreen.
- **`features/sales/receipt_screen.dart`** — Post-sale: items, subtotal, discount, total, payment method, transaction ID. "完成" pops to root.
- **`features/sales/sales_service.dart`** — SaleTransaction model + PaymentMethod enum + SalesService Dio stubs (POST/GET /sales/transactions).
- **`features/returns/return_screen.dart`** — Transaction ID lookup, CheckboxListTile item selection, ReturnService.submitReturn() call.
- **`features/returns/return_service.dart`** — ReturnableItem / OriginalTransaction + Dio stubs (GET transaction, POST returns).
- **`features/member_lookup/member_lookup_screen.dart`** — Camera placeholder + manual QR entry, MemberInfo display (name + tier), "關聯至當前銷售" links to cartProvider.
- **`features/member_lookup/member_lookup_service.dart`** — MemberInfo model + stub → GET /members/qr/{code}.
- **`features/attendance/clock_in_out_screen.dart`** — Employee ID input, Clock In / Clock Out buttons, last record display.
- **`features/attendance/attendance_service.dart`** — AttendanceRecord + stubs (POST clock-in/clock-out, GET last).
- **`features/home/home_screen.dart`** — 5-tab nav: 首頁 (placeholder), 銷售 (CartScreen), 會員 (MemberLookupScreen), 退貨 (ReturnScreen), 考勤 (ClockInOutScreen).

**meka-promos (Flutter Member App)**
- **`features/promotions/promotions_service.dart`** — Promotion model + PromotionType enum + stubs + activePromotionsProvider / promotionDetailProvider.
- **`features/promotions/promotions_screen.dart`** — Banner card list: bilingual name, type chip, validity dates.
- **`features/promotions/promotion_detail_screen.dart`** — Full bilingual detail, getCoupon() → snackbar; button only when isActive.
- **`features/coupons/coupons_service.dart`** — Coupon model with CouponStatus enum + stubs + myCouponsProvider / couponDetailProvider.
- **`features/coupons/coupons_screen.dart`** — Grouped list: active / redeemed (greyed) / expired (greyed).
- **`features/coupons/coupon_detail_screen.dart`** — Large QrImageView (qr_flutter), expiry, status chip, "展示給收銀員掃描" hint.
- **ARB files** (EN/ZHT/ZHS): added `coupons` l10n key.
- **`features/home/home_screen.dart`** — 5-tab nav: Promotions, Coupons, My QR, Notifications (placeholder), Profile.

**m2-portal (Blazor)**
- **`Services/PromotionService.cs`** — PromotionSummary, PromotionDetailDto, Create/UpdatePromotionRequest, PromotionType/PromotionStatus enums; typed HttpClient stub (GET, POST, PUT, activate, pause).
- **`Pages/Promotions/PromotionList.razor`** — Table: bilingual name, Type, Status chips, Start/End, IsStackable icon, View/Edit actions.
- **`Pages/Promotions/PromotionCreate.razor`** — Bilingual fields (ZHT + EN), Type dropdown, FormulaJson textarea, date pickers, IsStackable toggle → POST /promotions → detail page.
- **`Pages/Promotions/PromotionDetail.razor`** — Full-width MudAlert banner when PendingApproval (prominence pattern), read-only detail, Activate/Pause/ApprovalLink/Edit actions.
- **`Pages/Promotions/PromotionEdit.razor`** — Draft-only guard, editable fields, PUT /promotions/{id}.
- **`Pages/Promotions.razor`** — `@namespace M2Portal.Pages.Stubs` to avoid Blazor class-vs-namespace collision.
- **`Program.cs`** — PromotionService registered via typed HttpClient.

#### Key Technical Decisions Made

1. **5-tab POS nav** — Reused the old 收貨 (Goods Receipt) tab slot for 退貨 (Returns). Member lookup becomes a standalone tab — better discoverability vs buried inside CartScreen. Sales flow (cart → payment → receipt) uses Navigator.push, not separate tabs.
2. **Inline member QR sheet in CartScreen** — `showModalBottomSheet` for quick member association mid-transaction avoids forcing a tab switch mid-sale.
3. **`PromotionDetailDto` naming** — Same Dto suffix pattern as `ApprovalDetailDto` Sprint 2: avoids C# collision between Razor partial class `M2Portal.Pages.Promotions.PromotionDetail` and service DTO.
4. **`@namespace M2Portal.Pages.Stubs` on legacy Promotions.razor** — Cannot delete the Sprint 2 stub file; `@namespace` directive moves its generated class out of `M2Portal.Pages`, resolving Blazor compiler's class-vs-namespace conflict with new `Pages/Promotions/` subfolder.
5. **Camera scanner as UX placeholder** — `mobile_scanner` not added; camera placeholder widget shown with "即將推出" label. Avoids native permission complexity before integration testing is available.

#### Watch-Outs for Sprint 4

- **`mobile_scanner` integration**: Camera stub in POS member lookup + member app QR scan. Add to pubspec + AndroidManifest/Info.plist when platform testing is ready.
- **Cart persistence**: `CartNotifier` is in-memory only. Add `shared_preferences` write on every mutation to survive network failures (backlog requirement).
- **QR brightness boost**: `CouponDetailScreen` doesn't boost brightness. Add `SystemChrome` override on screen enter/leave (Sprint 1 UX pattern).
- **Double AppBar issue**: `PromotionsScreen` and `CouponsScreen` have their own `AppBar` inside `HomeScreen`'s `Scaffold`. Sprint 4 should convert them to scaffold-less list bodies or restructure routing.
- **`PromotionCreate` FormulaJson**: Raw textarea. Sprint 4 should add JSON parse validation and structured `DiscountValueConfig` conditional widget (DiscountType → Percentage fields / Fixed fields / Buy-X-Get-Y fields).
- **`_pendingCount` SignalR**: Still static 0 in NavMenu. Sprint 4 to wire SignalR hub per ADR-005.
