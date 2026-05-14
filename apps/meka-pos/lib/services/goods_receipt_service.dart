import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/api/api_client.dart';

enum GoodsReceiptStatus { pending, confirmed, discrepancy }

extension GoodsReceiptStatusLabel on GoodsReceiptStatus {
  String get label {
    switch (this) {
      case GoodsReceiptStatus.pending:
        return '待確認';
      case GoodsReceiptStatus.confirmed:
        return '已確認';
      case GoodsReceiptStatus.discrepancy:
        return '有差異';
    }
  }
}

class GoodsReceiptLine {
  const GoodsReceiptLine({
    required this.lineNumber,
    required this.productCode,
    required this.productNameZht,
    required this.expectedQty,
    required this.receivedQty,
    this.discrepancyNote,
  });

  final int lineNumber;
  final String productCode;
  final String productNameZht;
  final int expectedQty;
  final int receivedQty;
  final String? discrepancyNote;

  int get discrepancy => receivedQty - expectedQty;

  factory GoodsReceiptLine.fromJson(Map<String, dynamic> json) =>
      GoodsReceiptLine(
        lineNumber: json['lineNumber'] as int,
        productCode: json['productCode'] as String,
        productNameZht: json['productNameZht'] as String,
        expectedQty: json['expectedQty'] as int,
        receivedQty: json['receivedQty'] as int,
        discrepancyNote: json['discrepancyNote'] as String?,
      );
}

class GoodsReceipt {
  const GoodsReceipt({
    required this.id,
    required this.sapDeliveryNote,
    required this.status,
    required this.receivedDate,
    this.lines = const [],
  });

  final String id;
  final String sapDeliveryNote;
  final GoodsReceiptStatus status;
  final DateTime receivedDate;
  final List<GoodsReceiptLine> lines;

  factory GoodsReceipt.fromJson(Map<String, dynamic> json) => GoodsReceipt(
        id: json['id'] as String,
        sapDeliveryNote: json['sapDeliveryNote'] as String,
        status: GoodsReceiptStatus.values.firstWhere(
          (s) => s.name == json['status'],
          orElse: () => GoodsReceiptStatus.pending,
        ),
        receivedDate: DateTime.parse(json['receivedDate'] as String),
        lines: (json['lines'] as List? ?? [])
            .map((e) => GoodsReceiptLine.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

/// Typed HTTP service for goods receipts. Falls back to mock data when the
/// BFF is unavailable (BFF endpoint: `/api/goods-receipt/`).
class GoodsReceiptService {
  GoodsReceiptService(this._dio);
  final Dio _dio;

  /// GET /api/goods-receipt/
  Future<List<GoodsReceipt>> listAsync() async {
    try {
      final res = await _dio.get('/api/v1/goods-receipt/');
      return (res.data as List)
          .map((e) => GoodsReceipt.fromJson(e as Map<String, dynamic>))
          .toList();
    } catch (_) {
      return _mockList();
    }
  }

  /// GET /api/goods-receipt/{id}
  Future<GoodsReceipt> getAsync(String id) async {
    try {
      final res = await _dio.get('/api/v1/goods-receipt/$id');
      return GoodsReceipt.fromJson(res.data as Map<String, dynamic>);
    } catch (_) {
      return _mockList().firstWhere(
        (r) => r.id == id,
        orElse: () => _mockList().first,
      );
    }
  }

  static List<GoodsReceipt> _mockList() => [
        GoodsReceipt(
          id: 'gr-001',
          sapDeliveryNote: '4900012345',
          status: GoodsReceiptStatus.pending,
          receivedDate: DateTime.now().subtract(const Duration(hours: 2)),
          lines: [
            const GoodsReceiptLine(
              lineNumber: 1,
              productCode: 'PRD-001',
              productNameZht: '洗髮精 500ml',
              expectedQty: 24,
              receivedQty: 24,
            ),
            const GoodsReceiptLine(
              lineNumber: 2,
              productCode: 'PRD-002',
              productNameZht: '沐浴乳 300ml',
              expectedQty: 12,
              receivedQty: 10,
              discrepancyNote: '2件破損',
            ),
          ],
        ),
        GoodsReceipt(
          id: 'gr-002',
          sapDeliveryNote: '4900012346',
          status: GoodsReceiptStatus.confirmed,
          receivedDate: DateTime.now().subtract(const Duration(days: 1)),
          lines: [
            const GoodsReceiptLine(
              lineNumber: 1,
              productCode: 'PRD-003',
              productNameZht: '護髮素 250ml',
              expectedQty: 36,
              receivedQty: 36,
            ),
          ],
        ),
        GoodsReceipt(
          id: 'gr-003',
          sapDeliveryNote: '4900012347',
          status: GoodsReceiptStatus.discrepancy,
          receivedDate: DateTime.now().subtract(const Duration(days: 2)),
          lines: [
            const GoodsReceiptLine(
              lineNumber: 1,
              productCode: 'PRD-004',
              productNameZht: '洗面乳 100ml',
              expectedQty: 48,
              receivedQty: 40,
              discrepancyNote: '8件缺貨，供應商確認中',
            ),
          ],
        ),
      ];
}

final goodsReceiptServiceProvider = Provider<GoodsReceiptService>(
  (ref) => GoodsReceiptService(ref.read(apiClientProvider)),
);

final goodsReceiptListProvider = FutureProvider<List<GoodsReceipt>>((ref) {
  return ref.read(goodsReceiptServiceProvider).listAsync();
});

final goodsReceiptDetailProvider =
    FutureProvider.family<GoodsReceipt, String>((ref, id) {
  return ref.read(goodsReceiptServiceProvider).getAsync(id);
});
