import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';

enum CouponStatus { active, redeemed, expired }

class Coupon {
  const Coupon({
    required this.id,
    required this.code,
    required this.promotionId,
    required this.promotionNameEn,
    required this.promotionNameZht,
    required this.expiresAt,
    required this.status,
  });

  final String id;

  /// The value embedded in the QR code — scanned by POS staff.
  final String code;
  final String promotionId;
  final String promotionNameEn;
  final String promotionNameZht;
  final DateTime expiresAt;
  final CouponStatus status;

  bool get isActive => status == CouponStatus.active;
  bool get isRedeemed => status == CouponStatus.redeemed;
  bool get isExpired => status == CouponStatus.expired;

  factory Coupon.fromJson(Map<String, dynamic> json) => Coupon(
        id: json['id'] as String,
        code: json['code'] as String,
        promotionId: json['promotionId'] as String,
        promotionNameEn: json['promotionNameEn'] as String,
        promotionNameZht: json['promotionNameZht'] as String,
        expiresAt: DateTime.parse(json['expiresAt'] as String),
        status: CouponStatus.values.firstWhere(
          (s) => s.name == json['status'],
          orElse: () => CouponStatus.active,
        ),
      );
}

/// Stub service — calls MekaPromosBff /members/me/coupons endpoints.
class CouponsService {
  CouponsService(this._dio);
  final Dio _dio;

  /// GET /members/me/coupons — all coupons for authenticated member.
  Future<List<Coupon>> getMyCoupons() async {
    final res = await _dio.get('/api/v1/members/me/coupons');
    return (res.data as List)
        .map((e) => Coupon.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// GET /members/me/coupons/{id}
  Future<Coupon> getCoupon(String id) async {
    final res = await _dio.get('/api/v1/members/me/coupons/$id');
    return Coupon.fromJson(res.data as Map<String, dynamic>);
  }
}

final couponsServiceProvider = Provider<CouponsService>(
  (ref) => CouponsService(ref.read(apiClientProvider)),
);

final myCouponsProvider = FutureProvider<List<Coupon>>((ref) async {
  return ref.read(couponsServiceProvider).getMyCoupons();
});

final couponDetailProvider =
    FutureProvider.family<Coupon, String>((ref, id) async {
  return ref.read(couponsServiceProvider).getCoupon(id);
});
