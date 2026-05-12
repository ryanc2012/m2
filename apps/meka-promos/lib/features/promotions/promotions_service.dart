import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';

enum PromotionType { percentage, fixed, buyXGetY }

extension PromotionTypeLabel on PromotionType {
  String get label {
    switch (this) {
      case PromotionType.percentage:
        return '百分比折扣';
      case PromotionType.fixed:
        return '固定折扣';
      case PromotionType.buyXGetY:
        return '買X送Y';
    }
  }
}

class Promotion {
  const Promotion({
    required this.id,
    required this.nameEn,
    required this.nameZht,
    required this.type,
    required this.startDate,
    required this.endDate,
    required this.isActive,
    this.description,
    this.bannerImageUrl,
  });

  final String id;
  final String nameEn;
  final String nameZht;
  final PromotionType type;
  final DateTime startDate;
  final DateTime endDate;
  final bool isActive;
  final String? description;
  final String? bannerImageUrl;

  factory Promotion.fromJson(Map<String, dynamic> json) => Promotion(
        id: json['id'] as String,
        nameEn: json['nameEn'] as String,
        nameZht: json['nameZht'] as String,
        type: PromotionType.values.firstWhere(
          (t) => t.name == json['type'],
          orElse: () => PromotionType.percentage,
        ),
        startDate: DateTime.parse(json['startDate'] as String),
        endDate: DateTime.parse(json['endDate'] as String),
        isActive: json['isActive'] as bool? ?? true,
        description: json['description'] as String?,
        bannerImageUrl: json['bannerImageUrl'] as String?,
      );
}

/// Stub service — calls MekaPromosBff /promotions endpoints.
class PromotionsService {
  PromotionsService(this._dio);
  final Dio _dio;

  /// GET /promotions — active promotions for the authenticated member's shop.
  Future<List<Promotion>> getActivePromotions() async {
    final res = await _dio.get('/promotions');
    return (res.data as List)
        .map((e) => Promotion.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// GET /promotions/{id}
  Future<Promotion> getPromotion(String id) async {
    final res = await _dio.get('/promotions/$id');
    return Promotion.fromJson(res.data as Map<String, dynamic>);
  }

  /// POST /promotions/{id}/coupons — issue a coupon for the member.
  Future<void> getCoupon(String promotionId) async {
    await _dio.post('/promotions/$promotionId/coupons');
  }
}

final promotionsServiceProvider = Provider<PromotionsService>(
  (ref) => PromotionsService(ref.read(apiClientProvider)),
);

final activePromotionsProvider = FutureProvider<List<Promotion>>((ref) async {
  return ref.read(promotionsServiceProvider).getActivePromotions();
});

final promotionDetailProvider =
    FutureProvider.family<Promotion, String>((ref, id) async {
  return ref.read(promotionsServiceProvider).getPromotion(id);
});
