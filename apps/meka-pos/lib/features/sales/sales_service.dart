import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';
import 'cart_provider.dart';

enum PaymentMethod { cash, card, qrPay }

extension PaymentMethodLabel on PaymentMethod {
  String get label {
    switch (this) {
      case PaymentMethod.cash:
        return '現金';
      case PaymentMethod.card:
        return '信用卡';
      case PaymentMethod.qrPay:
        return 'QR Pay';
    }
  }
}

class SaleTransaction {
  const SaleTransaction({
    required this.transactionId,
    required this.items,
    required this.subtotal,
    required this.discountAmount,
    required this.total,
    required this.paymentMethod,
    required this.memberQrCode,
    required this.createdAt,
  });

  final String transactionId;
  final List<CartItem> items;
  final double subtotal;
  final double discountAmount;
  final double total;
  final PaymentMethod paymentMethod;
  final String? memberQrCode;
  final DateTime createdAt;

  factory SaleTransaction.fromJson(Map<String, dynamic> json) => SaleTransaction(
        transactionId: json['transactionId'] as String,
        items: (json['items'] as List)
            .map((e) => CartItem(
                  id: e['id'] as String,
                  name: e['name'] as String,
                  price: (e['price'] as num).toDouble(),
                  quantity: e['quantity'] as int,
                ))
            .toList(),
        subtotal: (json['subtotal'] as num).toDouble(),
        discountAmount: (json['discountAmount'] as num).toDouble(),
        total: (json['total'] as num).toDouble(),
        paymentMethod: PaymentMethod.values.firstWhere(
          (m) => m.name == json['paymentMethod'],
          orElse: () => PaymentMethod.cash,
        ),
        memberQrCode: json['memberQrCode'] as String?,
        createdAt: DateTime.parse(json['createdAt'] as String),
      );
}

/// Stub service — calls MekaPosBff /sales/transactions.
class SalesService {
  SalesService(this._dio);
  final Dio _dio;

  /// POST /sales/transactions — create a new sale.
  Future<SaleTransaction> createTransaction({
    required CartState cart,
    required PaymentMethod paymentMethod,
  }) async {
    final res = await _dio.post('/sales/transactions', data: {
      'items': cart.items
          .map((i) => {'id': i.id, 'quantity': i.quantity})
          .toList(),
      'discountAmount': cart.discountAmount,
      'paymentMethod': paymentMethod.name,
      'memberQrCode': cart.memberQrCode,
    });
    return SaleTransaction.fromJson(res.data as Map<String, dynamic>);
  }

  /// GET /sales/transactions/{id}
  Future<SaleTransaction> getTransaction(String id) async {
    final res = await _dio.get('/sales/transactions/$id');
    return SaleTransaction.fromJson(res.data as Map<String, dynamic>);
  }
}

final salesServiceProvider = Provider<SalesService>(
  (ref) => SalesService(ref.read(apiClientProvider)),
);
