import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import 'package:meka_promos/core/l10n/app_localizations.dart';
import 'registration_service.dart';
import '../profile/profile_service.dart';

class ProfileSetupScreen extends ConsumerStatefulWidget {
  const ProfileSetupScreen({super.key, required this.verificationToken});
  final String verificationToken;

  @override
  ConsumerState<ProfileSetupScreen> createState() => _ProfileSetupScreenState();
}

class _ProfileSetupScreenState extends ConsumerState<ProfileSetupScreen> {
  final _formKey = GlobalKey<FormState>();
  final _firstNameZhtCtrl = TextEditingController();
  final _lastNameZhtCtrl = TextEditingController();
  final _firstNameEnCtrl = TextEditingController();
  final _lastNameEnCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  bool _loading = false;

  @override
  void dispose() {
    _firstNameZhtCtrl.dispose();
    _lastNameZhtCtrl.dispose();
    _firstNameEnCtrl.dispose();
    _lastNameEnCtrl.dispose();
    _emailCtrl.dispose();
    super.dispose();
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

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _loading = true);
    try {
      await ref.read(registrationServiceProvider).register(
            verificationToken: widget.verificationToken,
            firstNameZht: _firstNameZhtCtrl.text.trim(),
            lastNameZht: _lastNameZhtCtrl.text.trim(),
            firstNameEn: _firstNameEnCtrl.text.trim(),
            lastNameEn: _lastNameEnCtrl.text.trim(),
            email: _emailCtrl.text.trim(),
          );
      // Stub: create a local session profile so the router auth-guard passes.
      final phone = ref.read(registrationPhoneProvider);
      final profile = MemberProfile(
        id: 'stub-id',
        phone: phone,
        firstNameZht: _firstNameZhtCtrl.text.trim(),
        lastNameZht: _lastNameZhtCtrl.text.trim(),
        firstNameEn: _firstNameEnCtrl.text.trim(),
        lastNameEn: _lastNameEnCtrl.text.trim(),
        email: _emailCtrl.text.trim(),
        qrCode: 'MEKA:$phone:stub',
        memberSince: DateTime.now(),
      );
      ref.read(memberSessionProvider.notifier).state = profile;
      if (mounted) context.go('/');
    } on Exception catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Registration failed: $e')),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context)!;

    return Scaffold(
      appBar: AppBar(title: Text(l10n.profileSetup)),
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
                    Text(
                      l10n.profileSetup,
                      style: Theme.of(context)
                          .textTheme
                          .headlineSmall
                          ?.copyWith(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 24),

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
                      onFieldSubmitted: (_) => _submit(),
                    ),
                    const SizedBox(height: 28),

                    ElevatedButton.icon(
                      onPressed: _loading ? null : _submit,
                      icon: _loading
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.check_circle_outline),
                      label: Text(l10n.completeRegistration),
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 14),
                      ),
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
