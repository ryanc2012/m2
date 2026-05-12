import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/profile/profile_screen.dart';
import '../../features/profile/profile_service.dart';
import '../../features/promotions/promotions_screen.dart';
import '../../features/coupons/coupons_screen.dart';
import '../../screens/notifications_screen.dart';
import 'package:flutter_gen/gen_l10n/app_localizations.dart';

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  int _selectedIndex = 0;

  Future<void> _signOut() async {
    ref.read(memberSessionProvider.notifier).state = null;
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    final navItems = [
      NavigationDestination(
        icon: const Icon(Icons.local_offer_outlined),
        label: l10n.promotions,
      ),
      NavigationDestination(
        icon: const Icon(Icons.confirmation_number_outlined),
        label: l10n.coupons,
      ),
      NavigationDestination(
        icon: const Icon(Icons.qr_code_outlined),
        label: l10n.myQr,
      ),
      NavigationDestination(
        icon: const Icon(Icons.notifications_outlined),
        label: l10n.notifications,
      ),
      NavigationDestination(
        icon: const Icon(Icons.person_outline),
        label: l10n.profile,
      ),
    ];

    final bodies = [
      const PromotionsScreen(),
      const CouponsScreen(),
      // My QR tab — show profile screen which includes the QR card prominently
      const ProfileScreen(),
      const NotificationsScreen(),
      const ProfileScreen(),
    ];

    return Scaffold(
      appBar: AppBar(
        title: const Text('Meka Promos'),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: l10n.signOut,
            onPressed: _signOut,
          ),
        ],
      ),
      body: bodies[_selectedIndex],
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (i) => setState(() => _selectedIndex = i),
        destinations: navItems,
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
            '$label — Sprint 3',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  color: Theme.of(context).colorScheme.outline,
                ),
          ),
        ],
      ),
    );
  }
}

