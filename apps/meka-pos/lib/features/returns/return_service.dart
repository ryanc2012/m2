import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';

class ReturnableItem {
  const ReturnableItem({
    required this.id,
    required this.name,
    required this.price,
    required this.quantity,
    this.selected = false,
  });

  final String id;
  final String name;
  final double price;
  final int quantity;
  final bool selected;

  double get subtotal => price * quantity;

  ReturnableItem copyWith({bool? selected}) => ReturnableItem(
        id: id,
        name: name,
        price: price,
        quantity: quantity,
        selected: selected ?? this.selected,
      );

  factory ReturnableItem.fromJson(Map<String, dynamic> json) => ReturnableItem(
        id: json['id'] as String,
        name: json['name'] as String,
        price: (json['price'] as num).toDouble(),
        quantity: json['quantity'] as int,
      );
}

class OriginalTransaction {
  const OriginalTransaction({
    required this.transactionId,
    required this.items,
    required this.total,
    required this.createdAt,
  });

  final String transactionId;
  final List<ReturnableItem> items;
  final double total;
  final DateTime createdAt;

  factory OriginalTransaction.fromJson(Map<String, dynamic> json) => OriginalTransaction(
        transactionId: json['transactionId'] as String,
        items: (json['items'] as List)
            .map((e) => ReturnableItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        total: (json['total'] as num).toDouble(),
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

/// Stub service — calls MekaPosBff /sales/transactions.
class ReturnService {
  ReturnService(this._dio);
  final Dio _dio;

  /// GET /sales/transactions/{id}
  Future<OriginalTransaction> getTransaction(String transactionId) async {
    final res = await _dio.get('/api/v1/sales/transactions/$transactionId');
    return OriginalTransaction.fromJson(res.data as Map<String, dynamic>);
  }

  /// POST /sales/transactions/{id}/returns
  Future<void> submitReturn({
    required String transactionId,
    required List<String> itemIds,
  }) async {
    await _dio.post('/api/v1/sales/transactions/$transactionId/returns', data: {
      'itemIds': itemIds,
    });
  }
}

final returnServiceProvider = Provider<ReturnService>(
  (ref) => ReturnService(ref.read(apiClientProvider)),
);
