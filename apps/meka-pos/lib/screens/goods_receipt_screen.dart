import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../services/goods_receipt_service.dart';

class GoodsReceiptScreen extends ConsumerWidget {
  const GoodsReceiptScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final listAsync = ref.watch(goodsReceiptListProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('收貨管理')),
      body: listAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(
                Icons.error_outline,
                size: 48,
                color: Theme.of(context).colorScheme.error,
              ),
              const SizedBox(height: 12),
              const Text('載入失敗，請重試'),
              const SizedBox(height: 8),
              FilledButton(
                onPressed: () => ref.invalidate(goodsReceiptListProvider),
                child: const Text('重試'),
              ),
            ],
          ),
        ),
        data: (receipts) {
          if (receipts.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(
                    Icons.inventory_2_outlined,
                    size: 64,
                    color: Theme.of(context).colorScheme.outline,
                  ),
                  const SizedBox(height: 12),
                  Text(
                    '暫無收貨記錄',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          color: Theme.of(context).colorScheme.outline,
                        ),
                  ),
                ],
              ),
            );
          }

          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(goodsReceiptListProvider),
            child: ListView.separated(
              padding: const EdgeInsets.symmetric(vertical: 8),
              itemCount: receipts.length,
              separatorBuilder: (_, __) => const Divider(height: 1, indent: 16),
              itemBuilder: (context, index) {
                final receipt = receipts[index];
                return _ReceiptListTile(receipt: receipt);
              },
            ),
          );
        },
      ),
    );
  }
}

class _ReceiptListTile extends StatelessWidget {
  const _ReceiptListTile({required this.receipt});
  final GoodsReceipt receipt;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListTile(
      leading: CircleAvatar(
        backgroundColor: _statusColor(receipt.status, theme).withOpacity(0.15),
        child: Icon(
          _statusIcon(receipt.status),
          color: _statusColor(receipt.status, theme),
          size: 20,
        ),
      ),
      title: Text(
        'SAP: ${receipt.sapDeliveryNote}',
        style: theme.textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
      ),
      subtitle: Text(
        _formatDate(receipt.receivedDate),
        style: theme.textTheme.bodySmall,
      ),
      trailing: Chip(
        label: Text(
          receipt.status.label,
          style: theme.textTheme.labelSmall?.copyWith(
            color: _statusColor(receipt.status, theme),
          ),
        ),
        backgroundColor: _statusColor(receipt.status, theme).withOpacity(0.12),
        side: BorderSide.none,
        padding: const EdgeInsets.symmetric(horizontal: 4),
      ),
      onTap: () => Navigator.of(context).push(
        MaterialPageRoute(
          builder: (_) => GoodsReceiptDetailScreen(receiptId: receipt.id),
        ),
      ),
    );
  }

  Color _statusColor(GoodsReceiptStatus status, ThemeData theme) {
    switch (status) {
      case GoodsReceiptStatus.pending:
        return Colors.amber.shade700;
      case GoodsReceiptStatus.confirmed:
        return Colors.green.shade700;
      case GoodsReceiptStatus.discrepancy:
        return theme.colorScheme.error;
    }
  }

  IconData _statusIcon(GoodsReceiptStatus status) {
    switch (status) {
      case GoodsReceiptStatus.pending:
        return Icons.pending_actions_outlined;
      case GoodsReceiptStatus.confirmed:
        return Icons.check_circle_outline;
      case GoodsReceiptStatus.discrepancy:
        return Icons.warning_amber_outlined;
    }
  }

  String _formatDate(DateTime dt) =>
      '${dt.year}-${dt.month.toString().padLeft(2, '0')}-'
      '${dt.day.toString().padLeft(2, '0')}';
}

/// Detail screen — shows header info and line-by-line breakdown.
class GoodsReceiptDetailScreen extends ConsumerWidget {
  const GoodsReceiptDetailScreen({super.key, required this.receiptId});

  final String receiptId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final detailAsync = ref.watch(goodsReceiptDetailProvider(receiptId));

    return Scaffold(
      appBar: AppBar(
        title: const Text('收貨明細'),
        leading: const BackButton(),
      ),
      body: detailAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.error_outline,
                  size: 48, color: Theme.of(context).colorScheme.error),
              const SizedBox(height: 12),
              const Text('載入失敗'),
              TextButton(
                onPressed: () =>
                    ref.invalidate(goodsReceiptDetailProvider(receiptId)),
                child: const Text('重試'),
              ),
            ],
          ),
        ),
        data: (receipt) => _DetailBody(receipt: receipt),
      ),
    );
  }
}

class _DetailBody extends StatelessWidget {
  const _DetailBody({required this.receipt});
  final GoodsReceipt receipt;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header card
          Card(
            margin: EdgeInsets.zero,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('送貨單號', style: theme.textTheme.labelMedium),
                      Chip(
                        label: Text(
                          receipt.status.label,
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: _statusColor(receipt.status, theme),
                          ),
                        ),
                        backgroundColor:
                            _statusColor(receipt.status, theme).withOpacity(0.12),
                        side: BorderSide.none,
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    receipt.sapDeliveryNote,
                    style: theme.textTheme.titleLarge
                        ?.copyWith(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Icon(Icons.calendar_today_outlined,
                          size: 14, color: theme.colorScheme.outline),
                      const SizedBox(width: 4),
                      Text(
                        '收貨日期：${_formatDate(receipt.receivedDate)}',
                        style: theme.textTheme.bodySmall,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          Text('商品明細', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),

          if (receipt.lines.isEmpty)
            Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  '暫無明細資料',
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: theme.colorScheme.outline,
                  ),
                ),
              ),
            )
          else
            ...receipt.lines.map((line) => _LineItemCard(line: line)),
        ],
      ),
    );
  }

  Color _statusColor(GoodsReceiptStatus status, ThemeData theme) {
    switch (status) {
      case GoodsReceiptStatus.pending:
        return Colors.amber.shade700;
      case GoodsReceiptStatus.confirmed:
        return Colors.green.shade700;
      case GoodsReceiptStatus.discrepancy:
        return theme.colorScheme.error;
    }
  }

  String _formatDate(DateTime dt) =>
      '${dt.year}-${dt.month.toString().padLeft(2, '0')}-'
      '${dt.day.toString().padLeft(2, '0')}';
}

class _LineItemCard extends StatelessWidget {
  const _LineItemCard({required this.line});
  final GoodsReceiptLine line;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final hasDiscrepancy = line.discrepancy != 0;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      color: hasDiscrepancy
          ? theme.colorScheme.errorContainer.withOpacity(0.4)
          : null,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        line.productNameZht,
                        style: theme.textTheme.bodyLarge
                            ?.copyWith(fontWeight: FontWeight.w600),
                      ),
                      Text(
                        line.productCode,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: theme.colorScheme.outline,
                        ),
                      ),
                    ],
                  ),
                ),
                if (hasDiscrepancy)
                  Icon(
                    Icons.warning_amber,
                    color: theme.colorScheme.error,
                    size: 20,
                  ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              children: [
                _QtyBadge(
                  label: '預期',
                  qty: line.expectedQty,
                  color: theme.colorScheme.outline,
                ),
                const SizedBox(width: 12),
                _QtyBadge(
                  label: '實收',
                  qty: line.receivedQty,
                  color: hasDiscrepancy
                      ? theme.colorScheme.error
                      : Colors.green.shade700,
                ),
                if (hasDiscrepancy) ...[
                  const SizedBox(width: 12),
                  _QtyBadge(
                    label: '差異',
                    qty: line.discrepancy,
                    color: theme.colorScheme.error,
                    showSign: true,
                  ),
                ],
              ],
            ),
            if (line.discrepancyNote != null) ...[
              const SizedBox(height: 8),
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.info_outline,
                      size: 14, color: theme.colorScheme.error),
                  const SizedBox(width: 4),
                  Expanded(
                    child: Text(
                      line.discrepancyNote!,
                      style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.error,
                      ),
                    ),
                  ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _QtyBadge extends StatelessWidget {
  const _QtyBadge({
    required this.label,
    required this.qty,
    required this.color,
    this.showSign = false,
  });

  final String label;
  final int qty;
  final Color color;
  final bool showSign;

  @override
  Widget build(BuildContext context) {
    final sign = showSign && qty > 0 ? '+' : '';
    return Column(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        Text(
          '$sign$qty',
          style: Theme.of(context)
              .textTheme
              .titleMedium
              ?.copyWith(color: color, fontWeight: FontWeight.bold),
        ),
        Text(
          label,
          style: Theme.of(context)
              .textTheme
              .labelSmall
              ?.copyWith(color: color),
        ),
      ],
    );
  }
}
