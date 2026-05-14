import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/coupons/coupons_service.dart';
import '../../features/profile/profile_service.dart';
import '../../features/promotions/promotions_service.dart';
import '../../features/registration/registration_service.dart';
import 'demo_data.dart';
import 'demo_services.dart';

/// Riverpod [Override]s injected into [ProviderScope] when [kDemoMode] is true.
/// Swaps all network-backed providers for static demo fixtures.
final List<Override> demoProviderOverrides = [
  // Pre-populate session so go_router skips /registration entirely.
  memberSessionProvider.overrideWith((ref) => demoProfile),

  memberProfileProvider.overrideWith((ref) async => demoProfile),

  activePromotionsProvider.overrideWith((ref) async => demoPromotions),

  myCouponsProvider.overrideWith((ref) async => demoCoupons),

  // Fake OTP flows — any phone / any 6-digit code works.
  registrationServiceProvider.overrideWith((ref) => DemoRegistrationService()),
  profileServiceProvider.overrideWith((ref) => DemoProfileService()),

  // Fake detail pages for each demo promotion.
  for (final promo in demoPromotions)
    promotionDetailProvider(promo.id).overrideWith((ref) async => promo),

  // Fake detail pages for each demo coupon.
  for (final coupon in demoCoupons)
    couponDetailProvider(coupon.id).overrideWith((ref) async => coupon),
];
