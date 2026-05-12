import 'package:flutter_riverpod/flutter_riverpod.dart';

class CartItem {
  const CartItem({
    required this.id,
    required this.name,
    required this.price,
    required this.quantity,
  });

  final String id;
  final String name;
  final double price;
  final int quantity;

  double get subtotal => price * quantity;

  CartItem copyWith({int? quantity}) => CartItem(
        id: id,
        name: name,
        price: price,
        quantity: quantity ?? this.quantity,
      );
}

class CartState {
  const CartState({
    this.items = const [],
    this.discountAmount = 0,
    this.memberQrCode,
    this.memberName,
  });

  final List<CartItem> items;
  final double discountAmount;

  /// QR code of the associated member (null = no member scanned).
  final String? memberQrCode;
  final String? memberName;

  double get subtotal => items.fold(0.0, (sum, i) => sum + i.subtotal);
  double get total => subtotal - discountAmount;
  int get totalQty => items.fold(0, (sum, i) => sum + i.quantity);

  CartState copyWith({
    List<CartItem>? items,
    double? discountAmount,
    String? memberQrCode,
    String? memberName,
    bool clearMember = false,
  }) {
    return CartState(
      items: items ?? this.items,
      discountAmount: discountAmount ?? this.discountAmount,
      memberQrCode: clearMember ? null : (memberQrCode ?? this.memberQrCode),
      memberName: clearMember ? null : (memberName ?? this.memberName),
    );
  }
}

class CartNotifier extends StateNotifier<CartState> {
  CartNotifier() : super(const CartState());

  void addItem(CartItem item) {
    final idx = state.items.indexWhere((i) => i.id == item.id);
    if (idx >= 0) {
      final updated = [...state.items];
      updated[idx] = updated[idx].copyWith(quantity: updated[idx].quantity + 1);
      state = state.copyWith(items: updated);
    } else {
      state = state.copyWith(items: [...state.items, item]);
    }
  }

  void removeItem(String itemId) {
    state = state.copyWith(
      items: state.items.where((i) => i.id != itemId).toList(),
    );
  }

  void updateQuantity(String itemId, int quantity) {
    if (quantity <= 0) {
      removeItem(itemId);
      return;
    }
    final updated =
        state.items.map((i) => i.id == itemId ? i.copyWith(quantity: quantity) : i).toList();
    state = state.copyWith(items: updated);
  }

  void applyDiscount(double amount) {
    state = state.copyWith(discountAmount: amount);
  }

  void setMember({required String qrCode, required String name}) {
    state = state.copyWith(memberQrCode: qrCode, memberName: name);
  }

  void clearMember() {
    state = state.copyWith(clearMember: true);
  }

  void clear() {
    state = const CartState();
  }
}

final cartProvider = StateNotifierProvider<CartNotifier, CartState>(
  (_) => CartNotifier(),
);
