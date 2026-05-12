import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'promotions_service.dart';

class PromotionDetailScreen extends ConsumerWidget {
  const PromotionDetailScreen({super.key, required this.promotionId});

  final String promotionId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final promotionAsync = ref.watch(promotionDetailProvider(promotionId));

    return Scaffold(
      appBar: AppBar(title: const Text('優惠詳情')),
      body: promotionAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 48),
              const SizedBox(height: 12),
              const Text('載入失敗'),
              TextButton(
                onPressed: () =>
                    ref.invalidate(promotionDetailProvider(promotionId)),
                child: const Text('重試'),
              ),
            ],
          ),
        ),
        data: (promotion) => _PromotionDetailBody(promotion: promotion),
      ),
    );
  }
}

class _PromotionDetailBody extends ConsumerStatefulWidget {
  const _PromotionDetailBody({required this.promotion});
  final Promotion promotion;

  @override
  ConsumerState<_PromotionDetailBody> createState() =>
      _PromotionDetailBodyState();
}

class _PromotionDetailBodyState
    extends ConsumerState<_PromotionDetailBody> {
  bool _gettingCoupon = false;
  String? _couponMessage;

  Future<void> _getCoupon() async {
    setState(() {
      _gettingCoupon = true;
      _couponMessage = null;
    });
    try {
      final service = ref.read(promotionsServiceProvider);
      await service.getCoupon(widget.promotion.id);
      setState(() => _couponMessage = '優惠券已發放，請到「優惠券」標籤查看。');
    } catch (_) {
      setState(() => _couponMessage = '發放失敗，請重試。');
    } finally {
      if (mounted) setState(() => _gettingCoupon = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final p = widget.promotion;

    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header banner
          Container(
            width: double.infinity,
            height: 160,
            color: theme.colorScheme.primaryContainer,
            child: Center(
              child: Icon(Icons.local_offer,
                  size: 72,
                  color: theme.colorScheme.onPrimaryContainer.withOpacity(0.35)),
            ),
          ),

          Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(p.nameZht, style: theme.textTheme.headlineSmall),
                Text(p.nameEn,
                    style: theme.textTheme.titleMedium
                        ?.copyWith(color: theme.colorScheme.outline)),
                const SizedBox(height: 16),

                _DetailRow(
                  icon: Icons.category_outlined,
                  label: '優惠類型',
                  value: p.type.label,
                ),
                _DetailRow(
                  icon: Icons.calendar_today_outlined,
                  label: '開始日期',
                  value: _formatDate(p.startDate),
                ),
                _DetailRow(
                  icon: Icons.event_outlined,
                  label: '截止日期',
                  value: _formatDate(p.endDate),
                ),
                _DetailRow(
                  icon: Icons.check_circle_outline,
                  label: '狀態',
                  value: p.isActive ? '進行中' : '已結束',
                ),

                if (p.description != null) ...[
                  const SizedBox(height: 16),
                  Text('說明', style: theme.textTheme.titleSmall),
                  const SizedBox(height: 4),
                  Text(p.description!),
                ],

                if (_couponMessage != null) ...[
                  const SizedBox(height: 16),
                  Text(_couponMessage!,
                      style: TextStyle(
                        color: _couponMessage!.contains('失敗')
                            ? theme.colorScheme.error
                            : theme.colorScheme.primary,
                      )),
                ],

                const SizedBox(height: 24),

                if (p.isActive)
                  FilledButton.icon(
                    onPressed: _gettingCoupon ? null : _getCoupon,
                    icon: _gettingCoupon
                        ? const SizedBox(
                            width: 18,
                            height: 18,
                            child: CircularProgressIndicator(strokeWidth: 2),
                          )
                        : const Icon(Icons.card_giftcard),
                    label: const Text('領取優惠券'),
                    style: FilledButton.styleFrom(
                      minimumSize: const Size(double.infinity, 48),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime dt) =>
      '${dt.year}-${dt.month.toString().padLeft(2, '0')}-'
      '${dt.day.toString().padLeft(2, '0')}';
}

class _DetailRow extends StatelessWidget {
  const _DetailRow({
    required this.icon,
    required this.label,
    required this.value,
  });
  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          Icon(icon, size: 18, color: Theme.of(context).colorScheme.outline),
          const SizedBox(width: 8),
          Text('$label：', style: Theme.of(context).textTheme.bodySmall),
          Text(value, style: Theme.of(context).textTheme.bodyMedium),
        ],
      ),
    );
  }
}
