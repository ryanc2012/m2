import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:msal_auth/msal_auth.dart';

// Placeholder client ID — replace with real Azure App Registration in CI/CD config.
const _kClientId = 'PLACEHOLDER_CLIENT_ID';
const _kTenantId = 'PLACEHOLDER_TENANT_ID';
const _kScopes = ['User.Read'];

/// Represents an authenticated staff user.
class StaffUser {
  const StaffUser({required this.displayName, required this.accountId});
  final String displayName;
  final String accountId;
}

/// MSAL auth service for POS shared-device mode (ADR-018/ADR-019).
/// Uses broker account-switch so each staff member logs into their own
/// Entra ID account on the shared tablet.
class AuthService {
  AuthService._();

  SingleAccountPca? _pca;

  Future<void> init() async {
    _pca = await SingleAccountPca.create(
      clientId: _kClientId,
      androidConfig: AndroidConfig(
        configFilePath: 'assets/msal_config.json',
        tenantId: _kTenantId,
      ),
      iosConfig: IosConfig(
        authority: 'https://login.microsoftonline.com/$_kTenantId',
        tenantType: TenantType.entraIDAndMicrosoftAccount,
        broker: true,
      ),
    );
  }

  Future<StaffUser?> signIn() async {
    try {
      final result = await _pca?.acquireToken(scopes: _kScopes);
      if (result == null) return null;
      return StaffUser(
        displayName: result.account.username,
        accountId: result.account.id,
      );
    } catch (_) {
      return null;
    }
  }

  Future<void> signOut() async {
    await _pca?.signOut();
  }

  Future<StaffUser?> getCurrentUser() async {
    try {
      final account = await _pca?.getCurrentAccount();
      if (account == null) return null;
      return StaffUser(
        displayName: account.username,
        accountId: account.id,
      );
    } catch (_) {
      return null;
    }
  }
}

final authServiceProvider = Provider<AuthService>((_) => AuthService._());

final authStateProvider = FutureProvider<StaffUser?>((ref) async {
  final service = ref.read(authServiceProvider);
  await service.init();
  return service.getCurrentUser();
});
