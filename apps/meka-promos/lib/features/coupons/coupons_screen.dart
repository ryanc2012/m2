import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:meka_promos/core/l10n/app_localizations.dart';

import 'coupons_service.dart';
import 'coupon_detail_screen.dart';

class CouponsScreen extends ConsumerWidget {
  const CouponsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    final couponsAsync = ref.watch(myCouponsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.coupons)),
      body: couponsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 48),
              const SizedBox(height: 12),
              const Text('載入失敗，請重試'),
              const SizedBox(height: 8),
              FilledButton(
                onPressed: () => ref.invalidate(myCouponsProvider),
                child: const Text('重試'),
              ),
            ],
          ),
        ),
        data: (coupons) {
          if (coupons.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.confirmation_number_outlined,
                      size: 64,
                      color: Theme.of(context).colorScheme.outline),
                  const SizedBox(height: 12),
                  const Text('暫無優惠券'),
                ],
              ),
            );
          }

          final active = coupons.where((c) => c.isActive).toList();
          final redeemed = coupons.where((c) => c.isRedeemed).toList();
          final expired = coupons.where((c) => c.isExpired).toList();

          return ListView(
            padding: const EdgeInsets.symmetric(vertical: 8),
            children: [
              if (active.isNotEmpty) ...[
                _SectionHeader(title: '有效優惠券 (${active.length})'),
                ...active.map((c) => _CouponCard(coupon: c)),
              ],
              if (redeemed.isNotEmpty) ...[
                _SectionHeader(title: '已使用 (${redeemed.length})'),
                ...redeemed.map((c) => _CouponCard(coupon: c)),
              ],
              if (expired.isNotEmpty) ...[
                _SectionHeader(title: '已過期 (${expired.length})'),
                ...expired.map((c) => _CouponCard(coupon: c)),
              ],
            ],
          );
        },
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.title});
  final String title;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
      child: Text(title,
          style: Theme.of(context).textTheme.labelLarge?.copyWith(
                color: Theme.of(context).colorScheme.outline,
              )),
    );
  }
}

class _CouponCard extends StatelessWidget {
  const _CouponCard({required this.coupon});
  final Coupon coupon;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isGreyed = !coupon.isActive;

    return Opacity(
      opacity: isGreyed ? 0.5 : 1.0,
      child: Card(
        margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        child: ListTile(
          leading: CircleAvatar(
            backgroundColor: isGreyed
                ? theme.colorScheme.surfaceVariant
                : theme.colorScheme.primaryContainer,
            child: Icon(
              coupon.isRedeemed
                  ? Icons.check_circle
                  : coupon.isExpired
                      ? Icons.schedule
                      : Icons.confirmation_number,
              color: isGreyed
                  ? theme.colorScheme.outline
                  : theme.colorScheme.primary,
            ),
          ),
          title: Text(coupon.promotionNameZht),
          subtitle: Text(
            coupon.isActive
                ? '到期：${_formatDate(coupon.expiresAt)}'
                : coupon.isRedeemed
                    ? '已使用'
                    : '已過期',
            style: theme.textTheme.bodySmall,
          ),
          trailing: coupon.isActive
              ? const Icon(Icons.qr_code)
              : null,
          onTap: coupon.isActive
              ? () => Navigator.of(context).push(
                    MaterialPageRoute(
                      builder: (_) => CouponDetailScreen(couponId: coupon.id),
                    ),
                  )
              : null,
        ),
      ),
    );
  }

  String _formatDate(DateTime dt) =>
      '${dt.year}-${dt.month.toString().padLeft(2, '0')}-'
      '${dt.day.toString().padLeft(2, '0')}';
}
