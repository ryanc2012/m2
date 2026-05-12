import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/api/api_client.dart';

class AppNotification {
  AppNotification({
    required this.id,
    required this.title,
    required this.body,
    required this.createdAt,
    required this.isRead,
    this.type = 'general',
  });

  final String id;
  final String title;
  final String body;
  final DateTime createdAt;
  bool isRead;
  final String type;

  factory AppNotification.fromJson(Map<String, dynamic> json) =>
      AppNotification(
        id: json['id'] as String,
        title: json['title'] as String,
        body: json['body'] as String,
        createdAt: DateTime.parse(json['createdAt'] as String),
        isRead: json['isRead'] as bool? ?? false,
        type: json['type'] as String? ?? 'general',
      );
}

/// Stub service — calls MekaPromosBff notification history endpoints.
/// Falls back to mock data when the BFF is unavailable.
class NotificationsService {
  NotificationsService(this._dio);
  final Dio _dio;

  /// GET /api/notification-history/
  Future<List<AppNotification>> getNotificationsAsync() async {
    try {
      final res = await _dio.get('/api/notification-history/');
      return (res.data as List)
          .map((e) => AppNotification.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (_) {
      return _mockNotifications();
    }
  }

  /// PATCH /api/notification-history/{id}/read
  Future<void> markReadAsync(String id) async {
    try {
      await _dio.patch('/api/notification-history/$id/read');
    } catch (_) {
      // Silently ignore when BFF is unavailable; local state already updated.
    }
  }

  static List<AppNotification> _mockNotifications() => [
        AppNotification(
          id: 'notif-001',
          title: '新優惠活動開始！',
          body: '夏日護膚優惠現已開始，立即查看您的優惠券！',
          createdAt: DateTime.now().subtract(const Duration(minutes: 30)),
          isRead: false,
          type: 'promotion',
        ),
        AppNotification(
          id: 'notif-002',
          title: '優惠券即將到期',
          body: '您的「買一送一」優惠券將於明天到期，請盡快使用。',
          createdAt: DateTime.now().subtract(const Duration(hours: 3)),
          isRead: false,
          type: 'coupon',
        ),
        AppNotification(
          id: 'notif-003',
          title: '積分兌換成功',
          body: '您已成功兌換 500 積分，優惠券已加入您的帳戶。',
          createdAt: DateTime.now().subtract(const Duration(days: 1)),
          isRead: true,
          type: 'points',
        ),
        AppNotification(
          id: 'notif-004',
          title: '歡迎加入 Meka！',
          body: '感謝您成為 Meka 會員，享受專屬會員優惠。',
          createdAt: DateTime.now().subtract(const Duration(days: 7)),
          isRead: true,
          type: 'general',
        ),
      ];
}

final notificationsServiceProvider = Provider<NotificationsService>(
  (ref) => NotificationsService(ref.read(apiClientProvider)),
);

final notificationsListProvider =
    FutureProvider<List<AppNotification>>((ref) async {
  return ref.read(notificationsServiceProvider).getNotificationsAsync();
});
