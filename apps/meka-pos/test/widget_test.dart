import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:meka_pos/features/sales/cart_provider.dart';

void main() {
  group('CartProvider', () {
    test('initial cart is empty with zero subtotal', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      expect(container.read(cartProvider).items, isEmpty);
      expect(container.read(cartProvider).subtotal, 0.0);
      expect(container.read(cartProvider).total, 0.0);
    });

    test('addItem increases items and updates subtotal', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(cartProvider.notifier).addItem(
            const CartItem(id: 'TEST001', name: 'Test Item', price: 99.90, quantity: 1),
          );

      expect(container.read(cartProvider).items.length, 1);
      expect(container.read(cartProvider).subtotal, closeTo(99.90, 0.001));
    });

    test('addItem same id increments quantity instead of duplicating', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(cartProvider.notifier).addItem(
            const CartItem(id: 'P001', name: 'Widget', price: 10.0, quantity: 1),
          );
      container.read(cartProvider.notifier).addItem(
            const CartItem(id: 'P001', name: 'Widget', price: 10.0, quantity: 1),
          );

      expect(container.read(cartProvider).items.length, 1);
      expect(container.read(cartProvider).items.first.quantity, 2);
      expect(container.read(cartProvider).subtotal, closeTo(20.0, 0.001));
    });

    test('clearCart resets state to empty', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(cartProvider.notifier).addItem(
            const CartItem(id: 'P001', name: 'Product', price: 50.0, quantity: 1),
          );
      expect(container.read(cartProvider).items, isNotEmpty);

      container.read(cartProvider.notifier).clearCart();
      expect(container.read(cartProvider).items, isEmpty);
      expect(container.read(cartProvider).subtotal, 0.0);
    });

    test('applyDiscount reduces total', () {
      final container = ProviderContainer();
      addTearDown(container.dispose);

      container.read(cartProvider.notifier).addItem(
            const CartItem(id: 'P001', name: 'Item', price: 100.0, quantity: 1),
          );
      container.read(cartProvider.notifier).applyDiscount(20.0);

      expect(container.read(cartProvider).total, closeTo(80.0, 0.001));
    });
  });
}
