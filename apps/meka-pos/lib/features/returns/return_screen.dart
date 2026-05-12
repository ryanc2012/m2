import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'return_service.dart';

class ReturnScreen extends ConsumerStatefulWidget {
  const ReturnScreen({super.key});

  @override
  ConsumerState<ReturnScreen> createState() => _ReturnScreenState();
}

class _ReturnScreenState extends ConsumerState<ReturnScreen> {
  final _txCtrl = TextEditingController();
  OriginalTransaction? _transaction;
  List<ReturnableItem> _items = [];
  bool _loading = false;
  bool _submitting = false;
  String? _error;
  String? _successMessage;

  @override
  void dispose() {
    _txCtrl.dispose();
    super.dispose();
  }

  Future<void> _lookupTransaction() async {
    final id = _txCtrl.text.trim();
    if (id.isEmpty) return;
    setState(() {
      _loading = true;
      _error = null;
      _transaction = null;
      _successMessage = null;
    });
    try {
      final service = ref.read(returnServiceProvider);
      final tx = await service.getTransaction(id);
      setState(() {
        _transaction = tx;
        _items = tx.items.map((i) => i.copyWith(selected: false)).toList();
      });
    } catch (e) {
      setState(() => _error = '找不到交易記錄，請確認交易 ID。');
    } finally {
      setState(() => _loading = false);
    }
  }

  Future<void> _submitReturn() async {
    final selected = _items.where((i) => i.selected).map((i) => i.id).toList();
    if (selected.isEmpty) return;
    setState(() => _submitting = true);
    try {
      final service = ref.read(returnServiceProvider);
      await service.submitReturn(
        transactionId: _transaction!.transactionId,
        itemIds: selected,
      );
      setState(() {
        _successMessage = '退貨申請已提交';
        _transaction = null;
        _items = [];
        _txCtrl.clear();
      });
    } catch (e) {
      setState(() => _error = '退貨提交失敗，請重試。');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('退貨處理', style: theme.textTheme.titleLarge),
          const SizedBox(height: 16),

          // Transaction ID input
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _txCtrl,
                  decoration: const InputDecoration(
                    labelText: '輸入交易 ID',
                    prefixIcon: Icon(Icons.receipt_outlined),
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _lookupTransaction(),
                ),
              ),
              const SizedBox(width: 12),
              FilledButton(
                onPressed: _loading ? null : _lookupTransaction,
                child: _loading
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Text('查詢'),
              ),
            ],
          ),

          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          if (_successMessage != null) ...[
            const SizedBox(height: 12),
            Row(
              children: [
                Icon(Icons.check_circle, color: theme.colorScheme.primary),
                const SizedBox(width: 8),
                Text(_successMessage!,
                    style: TextStyle(color: theme.colorScheme.primary)),
              ],
            ),
          ],

          if (_transaction != null) ...[
            const SizedBox(height: 24),
            Text('原始交易', style: theme.textTheme.titleMedium),
            const SizedBox(height: 4),
            Text('交易 ID: ${_transaction!.transactionId}',
                style: theme.textTheme.bodySmall),
            const SizedBox(height: 12),
            Text('選擇退貨商品：', style: theme.textTheme.bodyMedium),
            const SizedBox(height: 8),
            ...List.generate(_items.length, (idx) {
              final item = _items[idx];
              return CheckboxListTile(
                value: item.selected,
                title: Text('${item.name} × ${item.quantity}'),
                subtitle: Text('RM ${item.subtotal.toStringAsFixed(2)}'),
                onChanged: (v) {
                  setState(() {
                    _items[idx] = item.copyWith(selected: v ?? false);
                  });
                },
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                  side: BorderSide(color: theme.colorScheme.outlineVariant),
                ),
              );
            }),
            const SizedBox(height: 16),
            FilledButton.icon(
              onPressed: (_submitting || !_items.any((i) => i.selected))
                  ? null
                  : _submitReturn,
              icon: _submitting
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.assignment_return),
              label: const Text('提交退貨'),
              style: FilledButton.styleFrom(
                minimumSize: const Size(double.infinity, 48),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
