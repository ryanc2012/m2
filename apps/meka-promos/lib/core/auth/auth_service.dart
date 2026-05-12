import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:msal_auth/msal_auth.dart';

const _kClientId = 'PLACEHOLDER_CLIENT_ID';
const _kTenantId = 'PLACEHOLDER_TENANT_ID';
const _kScopes = ['User.Read', 'openid', 'profile'];

class MemberUser {
  const MemberUser({required this.displayName, required this.accountId});
  final String displayName;
  final String accountId;
}

/// Standard (non-shared-device) MSAL auth for the consumer member app.
class AuthService {
  AuthService._();

  MultipleAccountPca? _pca;

  Future<void> init() async {
    _pca = await MultipleAccountPca.create(
      clientId: _kClientId,
      androidConfig: AndroidConfig(
        configFilePath: 'assets/msal_config.json',
        tenantId: _kTenantId,
      ),
      iosConfig: IosConfig(
        authority: 'https://login.microsoftonline.com/$_kTenantId',
        tenantType: TenantType.entraIDAndMicrosoftAccount,
        broker: false,
      ),
    );
  }

  Future<MemberUser?> signIn() async {
    try {
      final result = await _pca?.acquireToken(scopes: _kScopes);
      if (result == null) return null;
      return MemberUser(
        displayName: result.account.username,
        accountId: result.account.id,
      );
    } catch (_) {
      return null;
    }
  }

  Future<void> signOut() async {
    final accounts = await _pca?.getAccounts() ?? [];
    for (final account in accounts) {
      await _pca?.removeAccount(account);
    }
  }

  Future<MemberUser?> getCurrentUser() async {
    try {
      final accounts = await _pca?.getAccounts() ?? [];
      if (accounts.isEmpty) return null;
      return MemberUser(
        displayName: accounts.first.username,
        accountId: accounts.first.id,
      );
    } catch (_) {
      return null;
    }
  }
}

final authServiceProvider = Provider<AuthService>((_) => AuthService._());

final authStateProvider = FutureProvider<MemberUser?>((ref) async {
  final service = ref.read(authServiceProvider);
  await service.init();
  return service.getCurrentUser();
});
