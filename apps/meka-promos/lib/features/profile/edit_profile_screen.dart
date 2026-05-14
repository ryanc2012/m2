import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'package:meka_promos/core/l10n/app_localizations.dart';
import 'profile_service.dart';

class EditProfileScreen extends ConsumerStatefulWidget {
  const EditProfileScreen({super.key});

  @override
  ConsumerState<EditProfileScreen> createState() => _EditProfileScreenState();
}

class _EditProfileScreenState extends ConsumerState<EditProfileScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameZhtCtrl = TextEditingController();
  final _lastNameZhtCtrl = TextEditingController();
  final _firstNameEnCtrl = TextEditingController();
  final _lastNameEnCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  bool _loading = false;
  bool _initialized = false;

  @override
  void dispose() {
    _firstNameZhtCtrl.dispose();
    _lastNameZhtCtrl.dispose();
    _firstNameEnCtrl.dispose();
    _lastNameEnCtrl.dispose();
    _emailCtrl.dispose();
    super.dispose();
  }

  void _populateFromProfile(MemberProfile p) {
    if (_initialized) return;
    _firstNameZhtCtrl.text = p.firstNameZht;
    _lastNameZhtCtrl.text = p.lastNameZht;
    _firstNameEnCtrl.text = p.firstNameEn;
    _lastNameEnCtrl.text = p.lastNameEn;
    _emailCtrl.text = p.email;
    _initialized = true;
  }

  String? _required(String? value) =>
      (value == null || value.trim().isEmpty) ? 'Required' : null;

  String? _validateEmail(String? value) {
    if (value == null || value.trim().isEmpty) return 'Required';
    if (!RegExp(r'^[^@]+@[^@]+\.[^@]+').hasMatch(value.trim())) {
      return 'Enter a valid email address';
    }
    return null;
  }

  Future<void> _save(MemberProfile current) async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      final updated = current.copyWith(
        firstNameZht: _firstNameZhtCtrl.text.trim(),
        lastNameZht: _lastNameZhtCtrl.text.trim(),
        firstNameEn: _firstNameEnCtrl.text.trim(),
        lastNameEn: _lastNameEnCtrl.text.trim(),
        email: _emailCtrl.text.trim(),
      );
      final saved = await ref.read(profileServiceProvider).updateProfile(updated);
      // Reflect changes in session and profile cache
      ref.read(memberSessionProvider.notifier).state = saved;
      ref.invalidate(memberProfileProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Profile updated.')),
        );
        context.pop();
      }
    } on Exception catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Failed to save: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    // Prefer live session; fall back to API profile
    final session = ref.watch(memberSessionProvider);
    final profileAsync = ref.watch(memberProfileProvider);

    final MemberProfile? current = session ?? profileAsync.valueOrNull;

    if (current == null && profileAsync.isLoading) {
      return Scaffold(
        appBar: AppBar(title: Text(l10n.editProfile)),
        body: const Center(child: CircularProgressIndicator()),
      );
    }

    if (current == null) {
      return Scaffold(
        appBar: AppBar(title: Text(l10n.editProfile)),
        body: Center(child: Text(l10n.errorOccurred)),
      );
    }

    _populateFromProfile(current);

    return Scaffold(
      appBar: AppBar(
        title: Text(l10n.editProfile),
        actions: [
          if (_loading)
            const Padding(
              padding: EdgeInsets.all(16),
              child: SizedBox(
                width: 20,
                height: 20,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            )
          else
            TextButton(
              onPressed: () => _save(current),
              child: Text(l10n.save),
            ),
        ],
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: Form(
                key: _formKey,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    // Phone — read-only
                    TextFormField(
                      initialValue: current.phone,
                      readOnly: true,
                      decoration: InputDecoration(
                        labelText: l10n.phone,
                        prefixIcon: const Icon(Icons.phone_outlined),
                        border: const OutlineInputBorder(),
                        filled: true,
                      ),
                    ),
                    const SizedBox(height: 16),

                    // ZHT Name row
                    Row(
                      children: [
                        Expanded(
                          child: TextFormField(
                            controller: _lastNameZhtCtrl,
                            decoration: InputDecoration(
                              labelText: l10n.lastNameZht,
                              border: const OutlineInputBorder(),
                            ),
                            validator: _required,
                            textInputAction: TextInputAction.next,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: TextFormField(
                            controller: _firstNameZhtCtrl,
                            decoration: InputDecoration(
                              labelText: l10n.firstNameZht,
                              border: const OutlineInputBorder(),
                            ),
                            validator: _required,
                            textInputAction: TextInputAction.next,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),

                    // EN Name row
                    Row(
                      children: [
                        Expanded(
                          child: TextFormField(
                            controller: _firstNameEnCtrl,
                            decoration: InputDecoration(
                              labelText: l10n.firstNameEn,
                              border: const OutlineInputBorder(),
                            ),
                            validator: _required,
                            textInputAction: TextInputAction.next,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: TextFormField(
                            controller: _lastNameEnCtrl,
                            decoration: InputDecoration(
                              labelText: l10n.lastNameEn,
                              border: const OutlineInputBorder(),
                            ),
                            validator: _required,
                            textInputAction: TextInputAction.next,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 16),

                    TextFormField(
                      controller: _emailCtrl,
                      keyboardType: TextInputType.emailAddress,
                      decoration: InputDecoration(
                        labelText: l10n.emailAddress,
                        prefixIcon: const Icon(Icons.email_outlined),
                        border: const OutlineInputBorder(),
                      ),
                      validator: _validateEmail,
                      textInputAction: TextInputAction.done,
                      onFieldSubmitted: (_) => _save(current),
                    ),
                    const SizedBox(height: 28),

                    ElevatedButton.icon(
                      onPressed: _loading ? null : () => _save(current),
                      icon: const Icon(Icons.save_outlined),
                      label: Text(l10n.save),
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
                    ),
                    const SizedBox(height: 12),
                    OutlinedButton(
                      onPressed: () => context.pop(),
                      child: Text(l10n.cancel),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
