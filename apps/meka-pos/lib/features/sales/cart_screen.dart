import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'cart_provider.dart';
import 'payment_screen.dart';

// Stub product catalogue — replaced by API call in a future sprint.
const _kProducts = [
  CartItem(id: 'P001', name: '棉質T恤 (白色)', price: 59.90, quantity: 1),
  CartItem(id: 'P002', name: '牛仔褲 (藍色)', price: 129.90, quantity: 1),
  CartItem(id: 'P003', name: '運動鞋 (黑色)', price: 199.90, quantity: 1),
  CartItem(id: 'P004', name: '帆布袋', price: 39.90, quantity: 1),
  CartItem(id: 'P005', name: '棒球帽', price: 49.90, quantity: 1),
  CartItem(id: 'P006', name: '休閒短褲', price: 79.90, quantity: 1),
];

class CartScreen extends ConsumerWidget {
  const CartScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cart = ref.watch(cartProvider);
    final notifier = ref.read(cartProvider.notifier);
    final theme = Theme.of(context);

    return Column(
      children: [
        // Member banner
        if (cart.memberQrCode != null)
          _MemberBanner(
            name: cart.memberName ?? cart.memberQrCode!,
            onClear: notifier.clearMember,
          ),

        // Product grid
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
          child: Row(
            children: [
              Text('商品', style: theme.textTheme.titleSmall),
              const Spacer(),
              TextButton.icon(
                icon: const Icon(Icons.qr_code_scanner, size: 18),
                label: const Text('查詢會員'),
                onPressed: () => _openMemberLookup(context, ref),
              ),
            ],
          ),
        ),
        SizedBox(
          height: 148,
          child: ListView.separated(
            scrollDirection: Axis.horizontal,
            padding: const EdgeInsets.symmetric(horizontal: 16),
            itemCount: _kProducts.length,
            separatorBuilder: (_, __) => const SizedBox(width: 8),
            itemBuilder: (_, idx) {
              final p = _kProducts[idx];
              return _ProductCard(
                product: p,
                onAdd: () => notifier.addItem(p),
              );
            },
          ),
        ),

        const Divider(),

        // Cart items
        Expanded(
          child: cart.items.isEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.shopping_cart_outlined,
                          size: 56, color: theme.colorScheme.outline),
                      const SizedBox(height: 8),
                      Text('購物車是空的', style: theme.textTheme.bodyMedium),
                    ],
                  ),
                )
              : ListView.builder(
                  itemCount: cart.items.length,
                  itemBuilder: (_, idx) {
                    final item = cart.items[idx];
                    return _CartItemRow(
                      item: item,
                      onIncrement: () =>
                          notifier.updateQuantity(item.id, item.quantity + 1),
                      onDecrement: () =>
                          notifier.updateQuantity(item.id, item.quantity - 1),
                      onRemove: () => notifier.removeItem(item.id),
                    );
                  },
                ),
        ),

        // Footer total + proceed
        _CartFooter(cart: cart),
      ],
    );
  }

  void _openMemberLookup(BuildContext context, WidgetRef ref) {
    showModalBottomSheet<Map<String, String>>(
      context: context,
      isScrollControlled: true,
      builder: (_) => const _MemberLookupSheet(),
    ).then((result) {
      if (result != null) {
        ref.read(cartProvider.notifier).setMember(
              qrCode: result['qrCode']!,
              name: result['name']!,
            );
      }
    });
  }
}

class _MemberBanner extends StatelessWidget {
  const _MemberBanner({required this.name, required this.onClear});
  final String name;
  final VoidCallback onClear;

  @override
  Widget build(BuildContext context) {
    return Container(
      color: Theme.of(context).colorScheme.primaryContainer,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(
        children: [
          const Icon(Icons.person, size: 18),
          const SizedBox(width: 8),
          Expanded(child: Text('會員：$name')),
          IconButton(
            icon: const Icon(Icons.close, size: 18),
            onPressed: onClear,
            tooltip: '移除會員',
            padding: EdgeInsets.zero,
            constraints: const BoxConstraints(),
          ),
        ],
      ),
    );
  }
}

class _ProductCard extends StatelessWidget {
  const _ProductCard({required this.product, required this.onAdd});
  final CartItem product;
  final VoidCallback onAdd;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return InkWell(
      onTap: onAdd,
      borderRadius: BorderRadius.circular(12),
      child: Container(
        width: 110,
        padding: const EdgeInsets.all(10),
        decoration: BoxDecoration(
          border: Border.all(color: theme.colorScheme.outlineVariant),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Expanded(
              child: Center(
                child: Icon(Icons.shopping_bag_outlined,
                    size: 36, color: theme.colorScheme.primary),
              ),
            ),
            Text(product.name,
                style: theme.textTheme.bodySmall,
                maxLines: 2,
                overflow: TextOverflow.ellipsis),
            const SizedBox(height: 4),
            Text('RM ${product.price.toStringAsFixed(2)}',
                style: theme.textTheme.labelMedium
                    ?.copyWith(color: theme.colorScheme.primary)),
          ],
        ),
      ),
    );
  }
}

class _CartItemRow extends StatelessWidget {
  const _CartItemRow({
    required this.item,
    required this.onIncrement,
    required this.onDecrement,
    required this.onRemove,
  });

  final CartItem item;
  final VoidCallback onIncrement;
  final VoidCallback onDecrement;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      title: Text(item.name),
      subtitle: Text('RM ${item.price.toStringAsFixed(2)}'),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(
            'RM ${item.subtotal.toStringAsFixed(2)}',
            style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
          ),
          const SizedBox(width: 8),
          IconButton(
              icon: const Icon(Icons.remove_circle_outline, size: 20),
              onPressed: onDecrement),
          Text('${item.quantity}'),
          IconButton(
              icon: const Icon(Icons.add_circle_outline, size: 20),
              onPressed: onIncrement),
          IconButton(
              icon: const Icon(Icons.delete_outline, size: 20),
              onPressed: onRemove,
              color: Theme.of(context).colorScheme.error),
        ],
      ),
    );
  }
}

class _CartFooter extends StatelessWidget {
  const _CartFooter({required this.cart});
  final CartState cart;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        boxShadow: [
          BoxShadow(
            color: Colors.black12,
            blurRadius: 4,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (cart.discountAmount > 0)
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text('折扣'),
                  Text('- RM ${cart.discountAmount.toStringAsFixed(2)}',
                      style: TextStyle(color: theme.colorScheme.error)),
                ],
              ),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('總計', style: theme.textTheme.titleMedium),
                Text(
                  'RM ${cart.total.toStringAsFixed(2)}',
                  style: theme.textTheme.titleLarge
                      ?.copyWith(fontWeight: FontWeight.bold),
                ),
              ],
            ),
            const SizedBox(height: 8),
            FilledButton.icon(
              onPressed: cart.items.isEmpty
                  ? null
                  : () => Navigator.of(context).push(
                        MaterialPageRoute(
                          builder: (_) => const PaymentScreen(),
                        ),
                      ),
              icon: const Icon(Icons.payment),
              label: const Text('前往付款'),
              style: FilledButton.styleFrom(
                minimumSize: const Size(double.infinity, 48),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// Inline bottom-sheet for quick member QR entry
class _MemberLookupSheet extends StatefulWidget {
  const _MemberLookupSheet();

  @override
  State<_MemberLookupSheet> createState() => _MemberLookupSheetState();
}

class _MemberLookupSheetState extends State<_MemberLookupSheet> {
  final _ctrl = TextEditingController();

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
        left: 24,
        right: 24,
        top: 24,
        bottom: MediaQuery.of(context).viewInsets.bottom + 24,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('查詢會員', style: Theme.of(context).textTheme.titleLarge),
          const SizedBox(height: 16),
          TextField(
            controller: _ctrl,
            autofocus: true,
            decoration: const InputDecoration(
              labelText: '輸入會員 QR 碼',
              prefixIcon: Icon(Icons.qr_code),
              border: OutlineInputBorder(),
            ),
            onSubmitted: (_) => _submit(context),
          ),
          const SizedBox(height: 8),
          Text(
            '提示：相機掃描功能即將推出',
            style: Theme.of(context).textTheme.bodySmall,
          ),
          const SizedBox(height: 16),
          Row(
            children: [
              Expanded(
                child: OutlinedButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text('取消'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: FilledButton(
                  onPressed: () => _submit(context),
                  child: const Text('查詢'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  void _submit(BuildContext context) {
    final code = _ctrl.text.trim();
    if (code.isEmpty) return;
    // Stub: return code as both qrCode and name placeholder
    Navigator.pop(context, {'qrCode': code, 'name': '會員 ($code)'});
  }
}
