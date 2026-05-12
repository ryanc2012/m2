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
    NavigationDestination(icon: Icon(Icons.local_offer_outlined), label: 'Promotions'),
    NavigationDestination(icon: Icon(Icons.qr_code_outlined), label: 'My QR'),
    NavigationDestination(icon: Icon(Icons.notifications_outlined), label: 'Notifications'),
    NavigationDestination(icon: Icon(Icons.person_outline), label: 'Profile'),
  ];

  static const _bodies = [
    _PlaceholderBody(label: 'Promotions'),
    _PlaceholderBody(label: 'My QR'),
    _PlaceholderBody(label: 'Notifications'),
    _PlaceholderBody(label: 'Profile'),
  ];

  Future<void> _signOut() async {
    await ref.read(authServiceProvider).signOut();
    ref.invalidate(authStateProvider);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Meka Promos'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Sign Out',
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
            '$label — Sprint 2/3',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: Theme.of(context).colorScheme.outline,
                ),
          ),
        ],
      ),
    );
  }
}
