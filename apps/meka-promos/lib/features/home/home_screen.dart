import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/profile/profile_screen.dart';
import '../../features/profile/profile_service.dart';
import '../../features/promotions/promotions_screen.dart';
import '../../features/coupons/coupons_screen.dart';
import '../../screens/notifications_screen.dart';
import 'package:meka_promos/core/l10n/app_localizations.dart';

class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  int _selectedIndex = 0;

  // Tabs that require authentication (by index)
  static const _authRequiredTabs = {1, 2, 4};

  Future<void> _signOut() async {
    ref.read(memberSessionProvider.notifier).state = null;
  }

  Widget _guardedBody(int index, Widget real) {
    final session = ref.watch(memberSessionProvider);
    if (_authRequiredTabs.contains(index) && session == null) {
      return const _LoginPromptBody();
    }
    return real;
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;
    final session = ref.watch(memberSessionProvider);
    final isAuthed = session != null;

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

    final rawBodies = [
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
          if (isAuthed)
            IconButton(
              icon: const Icon(Icons.logout),
              tooltip: l10n.signOut,
              onPressed: _signOut,
            )
          else
            IconButton(
              icon: const Icon(Icons.login),
              tooltip: '登入 / Sign in',
              onPressed: () => context.go('/registration'),
            ),
        ],
      ),
      body: _guardedBody(_selectedIndex, rawBodies[_selectedIndex]),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _selectedIndex,
        onDestinationSelected: (i) => setState(() => _selectedIndex = i),
        destinations: navItems,
      ),
    );
  }
}

class _LoginPromptBody extends StatelessWidget {
  const _LoginPromptBody();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(32),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.lock_outline,
                size: 64, color: Theme.of(context).colorScheme.outline),
            const SizedBox(height: 16),
            Text(
              '請先登入\nPlease sign in',
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: Theme.of(context).colorScheme.outline,
                  ),
            ),
            const SizedBox(height: 24),
            FilledButton(
              onPressed: () => context.go('/registration'),
              child: const Text('登入 / Sign in'),
            ),
          ],
        ),
      ),
    );
  }
}


