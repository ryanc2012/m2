import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:qr_flutter/qr_flutter.dart';

import 'coupons_service.dart';

class CouponDetailScreen extends ConsumerWidget {
  const CouponDetailScreen({super.key, required this.couponId});

  final String couponId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final couponAsync = ref.watch(couponDetailProvider(couponId));

    return Scaffold(
      appBar: AppBar(title: const Text('優惠券')),
      body: couponAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 48),
              const SizedBox(height: 12),
              const Text('載入失敗'),
              TextButton(
                onPressed: () => ref.invalidate(couponDetailProvider(couponId)),
                child: const Text('重試'),
              ),
            ],
          ),
        ),
        data: (coupon) => _CouponDetailBody(coupon: coupon),
      ),
    );
  }
}

class _CouponDetailBody extends StatelessWidget {
  const _CouponDetailBody({required this.coupon});
  final Coupon coupon;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Text(coupon.promotionNameZht, style: theme.textTheme.headlineSmall),
          Text(coupon.promotionNameEn,
              style: theme.textTheme.titleMedium
                  ?.copyWith(color: theme.colorScheme.outline)),
          const SizedBox(height: 24),

          // QR code — courier scans this at POS
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16),
              boxShadow: [
                BoxShadow(
                  color: Colors.black12,
                  blurRadius: 8,
                  spreadRadius: 2,
                ),
              ],
            ),
            child: QrImageView(
              data: coupon.code,
              version: QrVersions.auto,
              size: 220,
            ),
          ),

          const SizedBox(height: 16),

          Text(
            coupon.code,
            style: theme.textTheme.bodySmall?.copyWith(
              letterSpacing: 1.5,
              color: theme.colorScheme.outline,
            ),
          ),

          const SizedBox(height: 24),

          // Status chip
          if (coupon.isActive)
            Chip(
              avatar: const Icon(Icons.check_circle, size: 16),
              label: const Text('有效'),
              backgroundColor: theme.colorScheme.primaryContainer,
              labelStyle: TextStyle(color: theme.colorScheme.primary),
            )
          else if (coupon.isRedeemed)
            Chip(
              avatar: const Icon(Icons.check, size: 16),
              label: const Text('已使用'),
              backgroundColor: theme.colorScheme.surfaceVariant,
            )
          else
            Chip(
              avatar: const Icon(Icons.schedule, size: 16),
              label: const Text('已過期'),
              backgroundColor: theme.colorScheme.errorContainer,
              labelStyle: TextStyle(color: theme.colorScheme.error),
            ),

          const SizedBox(height: 16),

          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(Icons.event_outlined,
                  size: 16, color: theme.colorScheme.outline),
              const SizedBox(width: 4),
              Text(
                '到期日：${_formatDate(coupon.expiresAt)}',
                style: theme.textTheme.bodyMedium
                    ?.copyWith(color: theme.colorScheme.outline),
              ),
            ],
          ),

          if (coupon.isActive) ...[
            const SizedBox(height: 16),
            Text(
              '請將 QR 碼展示給收銀員掃描',
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: theme.colorScheme.outline),
            ),
          ],
        ],
      ),
    );
  }

  String _formatDate(DateTime dt) =>
      '${dt.year}-${dt.month.toString().padLeft(2, '0')}-'
      '${dt.day.toString().padLeft(2, '0')}';
}
