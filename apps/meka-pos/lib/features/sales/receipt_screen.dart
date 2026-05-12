import 'package:flutter/material.dart';

import 'sales_service.dart';

class ReceiptScreen extends StatelessWidget {
  const ReceiptScreen({super.key, required this.transaction});

  final SaleTransaction transaction;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('收據'),
        automaticallyImplyLeading: false,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            // Success indicator
            Icon(Icons.check_circle, size: 72, color: theme.colorScheme.primary),
            const SizedBox(height: 8),
            Text('交易成功', style: theme.textTheme.headlineSmall),
            const SizedBox(height: 4),
            Text(
              '交易 ID: ${transaction.transactionId}',
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.outline),
            ),
            const SizedBox(height: 20),

            // Receipt card
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text('日期', style: theme.textTheme.bodySmall),
                        Text(
                          _formatDate(transaction.createdAt),
                          style: theme.textTheme.bodySmall,
                        ),
                      ],
                    ),
                    const Divider(height: 16),
                    ...transaction.items.map(
                      (item) => Padding(
                        padding: const EdgeInsets.symmetric(vertical: 3),
                        child: Row(
                          children: [
                            Expanded(
                              child: Text('${item.name}\n× ${item.quantity}',
                                  style: theme.textTheme.bodyMedium),
                            ),
                            Text('RM ${item.subtotal.toStringAsFixed(2)}'),
                          ],
                        ),
                      ),
                    ),
                    const Divider(height: 16),
                    _SummaryRow(
                        label: '小計',
                        value: 'RM ${transaction.subtotal.toStringAsFixed(2)}'),
                    if (transaction.discountAmount > 0)
                      _SummaryRow(
                        label: '折扣',
                        value: '- RM ${transaction.discountAmount.toStringAsFixed(2)}',
                        color: theme.colorScheme.error,
                      ),
                    const Divider(height: 12),
                    _SummaryRow(
                      label: '總計',
                      value: 'RM ${transaction.total.toStringAsFixed(2)}',
                      bold: true,
                    ),
                    const SizedBox(height: 8),
                    _SummaryRow(
                      label: '付款方式',
                      value: transaction.paymentMethod.label,
                    ),
                    if (transaction.memberQrCode != null)
                      _SummaryRow(
                        label: '會員',
                        value: transaction.memberQrCode!,
                      ),
                  ],
                ),
              ),
            ),

            const SizedBox(height: 24),
            FilledButton(
              onPressed: () => Navigator.of(context).popUntil((r) => r.isFirst),
              style: FilledButton.styleFrom(
                minimumSize: const Size(double.infinity, 48),
              ),
              child: const Text('完成'),
            ),
          ],
        ),
      ),
    );
  }

  String _formatDate(DateTime dt) {
    return '${dt.year}-${dt.month.toString().padLeft(2, '0')}-'
        '${dt.day.toString().padLeft(2, '0')} '
        '${dt.hour.toString().padLeft(2, '0')}:'
        '${dt.minute.toString().padLeft(2, '0')}';
  }
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({required this.label, required this.value, this.bold = false, this.color});
  final String label;
  final String value;
  final bool bold;
  final Color? color;

  @override
  Widget build(BuildContext context) {
    final style = Theme.of(context).textTheme.bodyMedium?.copyWith(
          fontWeight: bold ? FontWeight.bold : null,
          color: color,
        );
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: style),
          Text(value, style: style),
        ],
      ),
    );
  }
}
