import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'cart_provider.dart';
import 'sales_service.dart';
import 'receipt_screen.dart';

class PaymentScreen extends ConsumerStatefulWidget {
  const PaymentScreen({super.key});

  @override
  ConsumerState<PaymentScreen> createState() => _PaymentScreenState();
}

class _PaymentScreenState extends ConsumerState<PaymentScreen> {
  PaymentMethod _selectedMethod = PaymentMethod.cash;
  bool _processing = false;
  String? _error;

  Future<void> _completeSale() async {
    setState(() {
      _processing = true;
      _error = null;
    });
    try {
      final cart = ref.read(cartProvider);
      final service = ref.read(salesServiceProvider);
      final tx = await service.createTransaction(
        cart: cart,
        paymentMethod: _selectedMethod,
      );
      ref.read(cartProvider.notifier).clearCart();
      if (mounted) {
        Navigator.of(context).pushReplacement(
          MaterialPageRoute(builder: (_) => ReceiptScreen(transaction: tx)),
        );
      }
    } catch (e) {
      setState(() => _error = '交易失敗，請重試。');
    } finally {
      if (mounted) setState(() => _processing = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cart = ref.watch(cartProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text('付款')),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Order summary card
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text('訂單摘要', style: theme.textTheme.titleMedium),
                    const Divider(),
                    ...cart.items.map(
                      (item) => Padding(
                        padding: const EdgeInsets.symmetric(vertical: 2),
                        child: Row(
                          children: [
                            Expanded(child: Text('${item.name} × ${item.quantity}')),
                            Text('RM ${item.subtotal.toStringAsFixed(2)}'),
                          ],
                        ),
                      ),
                    ),
                    if (cart.discountAmount > 0) ...[
                      const Divider(),
                      Row(
                        children: [
                          const Expanded(child: Text('折扣')),
                          Text(
                            '- RM ${cart.discountAmount.toStringAsFixed(2)}',
                            style: TextStyle(color: theme.colorScheme.error),
                          ),
                        ],
                      ),
                    ],
                    const Divider(),
                    Row(
                      children: [
                        Expanded(
                          child: Text('總計',
                              style: theme.textTheme.titleMedium
                                  ?.copyWith(fontWeight: FontWeight.bold)),
                        ),
                        Text(
                          'RM ${cart.total.toStringAsFixed(2)}',
                          style: theme.textTheme.titleLarge
                              ?.copyWith(fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 24),

            Text('付款方式', style: theme.textTheme.titleMedium),
            const SizedBox(height: 12),

            // Payment method selector
            ...PaymentMethod.values.map(
              (method) => RadioListTile<PaymentMethod>(
                value: method,
                groupValue: _selectedMethod,
                title: Text(method.label),
                leading: Icon(_methodIcon(method)),
                onChanged: (v) => setState(() => _selectedMethod = v!),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                  side: BorderSide(
                    color: _selectedMethod == method
                        ? theme.colorScheme.primary
                        : theme.colorScheme.outlineVariant,
                  ),
                ),
                tileColor: _selectedMethod == method
                    ? theme.colorScheme.primaryContainer.withOpacity(0.4)
                    : null,
              ),
            ),

            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
            ],

            const Spacer(),

            FilledButton(
              onPressed: _processing ? null : _completeSale,
              style: FilledButton.styleFrom(
                minimumSize: const Size(double.infinity, 52),
              ),
              child: _processing
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Text('完成交易', style: TextStyle(fontSize: 16)),
            ),
          ],
        ),
      ),
    );
  }

  IconData _methodIcon(PaymentMethod method) {
    switch (method) {
      case PaymentMethod.cash:
        return Icons.payments_outlined;
      case PaymentMethod.card:
        return Icons.credit_card;
      case PaymentMethod.qrPay:
        return Icons.qr_code;
    }
  }
}
