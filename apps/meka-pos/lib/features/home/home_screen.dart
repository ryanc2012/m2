import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_service.dart';

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  int _selectedIndex = 0;

  static const _navItems = [
    NavigationDestination(icon: Icon(Icons.home_outlined), label: '首頁'),
    NavigationDestination(icon: Icon(Icons.point_of_sale_outlined), label: '銷售'),
    NavigationDestination(icon: Icon(Icons.access_time_outlined), label: '考勤'),
    NavigationDestination(icon: Icon(Icons.inventory_2_outlined), label: '收貨'),
  ];

  // Placeholder bodies — filled in Sprint 2/3.
  static const _bodies = [
    _PlaceholderBody(label: '首頁'),
    _PlaceholderBody(label: '銷售'),
    _PlaceholderBody(label: '考勤'),
    _PlaceholderBody(label: '收貨'),
  ];

  Future<void> _signOut() async {
    await ref.read(authServiceProvider).signOut();
    ref.invalidate(authStateProvider);
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authStateProvider);
    final userName = authState.valueOrNull?.displayName ?? '';

    return Scaffold(
      appBar: AppBar(
        title: const Text('Meka POS'),
        actions: [
          if (userName.isNotEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: Center(child: Text(userName)),
            ),
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: '登出',
            onPressed: _signOut,
          ),
        ],
      ),
      body: _bodies[_selectedIndex],
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (i) => setState(() => _selectedIndex = i),
        destinations: _navItems,
      ),
    );
  }
}

class _PlaceholderBody extends StatelessWidget {
  const _PlaceholderBody({required this.label});
  final String label;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(Icons.construction, size: 64, color: Theme.of(context).colorScheme.outline),
          const SizedBox(height: 16),
          Text(
            '$label — Sprint 2/3 予定',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: Theme.of(context).colorScheme.outline,
                ),
          ),
        ],
      ),
    );
  }
}
