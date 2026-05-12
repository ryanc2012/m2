import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

import 'promotions_service.dart';
import 'promotion_detail_screen.dart';

class PromotionsScreen extends ConsumerWidget {
  const PromotionsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context)!;
    final promotionsAsync = ref.watch(activePromotionsProvider);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.promotions)),
      body: promotionsAsync.when(
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
                onPressed: () => ref.invalidate(activePromotionsProvider),
                child: const Text('重試'),
              ),
            ],
          ),
        ),
        data: (promotions) => promotions.isEmpty
            ? Center(
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(Icons.local_offer_outlined,
                        size: 64,
                        color: Theme.of(context).colorScheme.outline),
                    const SizedBox(height: 12),
                    const Text('暫無優惠'),
                  ],
                ),
              )
            : ListView.builder(
                padding: const EdgeInsets.symmetric(vertical: 8),
                itemCount: promotions.length,
                itemBuilder: (_, idx) =>
                    _PromotionBannerCard(promotion: promotions[idx]),
              ),
      ),
    );
  }
}

class _PromotionBannerCard extends StatelessWidget {
  const _PromotionBannerCard({required this.promotion});
  final Promotion promotion;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      margin: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => Navigator.of(context).push(
          MaterialPageRoute(
            builder: (_) => PromotionDetailScreen(promotionId: promotion.id),
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Banner area
            Container(
              height: 100,
              color: theme.colorScheme.primaryContainer,
              child: Stack(
                children: [
                  Center(
                    child: Icon(Icons.local_offer,
                        size: 48,
                        color: theme.colorScheme.onPrimaryContainer
                            .withOpacity(0.3)),
                  ),
                  if (!promotion.isActive)
                    Positioned(
                      top: 8,
                      right: 8,
                      child: Chip(
                        label: const Text('已結束'),
                        backgroundColor: theme.colorScheme.errorContainer,
                        labelStyle: TextStyle(
                            color: theme.colorScheme.onErrorContainer,
                            fontSize: 11),
                      ),
                    ),
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(promotion.nameZht,
                      style: theme.textTheme.titleMedium),
                  Text(promotion.nameEn,
                      style: theme.textTheme.bodySmall
                          ?.copyWith(color: theme.colorScheme.outline)),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      Icon(Icons.calendar_today_outlined,
                          size: 14, color: theme.colorScheme.outline),
                      const SizedBox(width: 4),
                      Text(
                        '${_formatDate(promotion.startDate)} – ${_formatDate(promotion.endDate)}',
                        style: theme.textTheme.bodySmall
                            ?.copyWith(color: theme.colorScheme.outline),
                      ),
                      const Spacer(),
                      Chip(
                        label: Text(promotion.type.label,
                            style: const TextStyle(fontSize: 11)),
                        padding: EdgeInsets.zero,
                        visualDensity: VisualDensity.compact,
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _formatDate(DateTime dt) =>
      '${dt.year}-${dt.month.toString().padLeft(2, '0')}-'
      '${dt.day.toString().padLeft(2, '0')}';
}
