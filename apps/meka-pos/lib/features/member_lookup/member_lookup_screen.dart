import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'member_lookup_service.dart';
import '../sales/cart_provider.dart';

class MemberLookupScreen extends ConsumerStatefulWidget {
  const MemberLookupScreen({super.key});

  @override
  ConsumerState<MemberLookupScreen> createState() => _MemberLookupScreenState();
}

class _MemberLookupScreenState extends ConsumerState<MemberLookupScreen> {
  final _ctrl = TextEditingController();
  MemberInfo? _member;
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  Future<void> _lookup() async {
    final code = _ctrl.text.trim();
    if (code.isEmpty) return;
    setState(() {
      _loading = true;
      _error = null;
      _member = null;
    });
    try {
      final service = ref.read(memberLookupServiceProvider);
      final info = await service.lookupByQr(code);
      setState(() => _member = info);
    } catch (e) {
      setState(() => _error = '找不到會員，請確認 QR 碼。');
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final cart = ref.watch(cartProvider);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('會員查詢', style: theme.textTheme.titleLarge),
          const SizedBox(height: 16),

          // Camera placeholder
          Container(
            height: 160,
            decoration: BoxDecoration(
              color: theme.colorScheme.surfaceVariant,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: theme.colorScheme.outlineVariant),
            ),
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.qr_code_scanner, size: 48,
                      color: theme.colorScheme.outline),
                  const SizedBox(height: 8),
                  Text('相機掃描 — 即將推出',
                      style: theme.textTheme.bodyMedium
                          ?.copyWith(color: theme.colorScheme.outline)),
                ],
              ),
            ),
          ),

          const SizedBox(height: 16),
          const Row(children: [
            Expanded(child: Divider()),
            Padding(
              padding: EdgeInsets.symmetric(horizontal: 12),
              child: Text('或手動輸入'),
            ),
            Expanded(child: Divider()),
          ]),
          const SizedBox(height: 16),

          // Manual entry
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _ctrl,
                  decoration: const InputDecoration(
                    labelText: '輸入會員 QR 碼',
                    prefixIcon: Icon(Icons.qr_code),
                    border: OutlineInputBorder(),
                  ),
                  onSubmitted: (_) => _lookup(),
                ),
              ),
              const SizedBox(width: 12),
              FilledButton(
                onPressed: _loading ? null : _lookup,
                child: _loading
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Text('查詢'),
              ),
            ],
          ),

          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
          ],

          if (_member != null) ...[
            const SizedBox(height: 20),
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    CircleAvatar(
                      radius: 32,
                      child: Text(_member!.name.characters.first,
                          style: const TextStyle(fontSize: 24)),
                    ),
                    const SizedBox(height: 12),
                    Text(_member!.name, style: theme.textTheme.titleMedium),
                    const SizedBox(height: 4),
                    Chip(
                      label: Text(_member!.tier),
                      avatar: const Icon(Icons.star, size: 16),
                    ),
                    const SizedBox(height: 12),
                    if (cart.memberQrCode == _member!.qrCode)
                      Row(
                        mainAxisAlignment: MainAxisAlignment.center,
                        children: [
                          Icon(Icons.check_circle,
                              color: theme.colorScheme.primary),
                          const SizedBox(width: 8),
                          const Text('已加入銷售'),
                        ],
                      )
                    else
                      FilledButton.icon(
                        onPressed: () {
                          ref.read(cartProvider.notifier).setMember(
                                qrCode: _member!.qrCode,
                                name: _member!.name,
                              );
                          ScaffoldMessenger.of(context).showSnackBar(
                            SnackBar(content: Text('已將 ${_member!.name} 加入銷售')),
                          );
                        },
                        icon: const Icon(Icons.link),
                        label: const Text('關聯至當前銷售'),
                      ),
                  ],
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}
