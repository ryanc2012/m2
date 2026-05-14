import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';

class RegistrationService {
  RegistrationService(this._dio);
  final Dio _dio;

  /// POST /api/v1/members/otp/send — sends OTP to the given phone number (new member registration).
  Future<void> sendOtp(String phone) async {
    await _dio.post('/api/v1/members/otp/send', data: {'phone': phone});
  }

  /// POST /api/v1/members/otp/verify — verifies OTP; returns a short-lived verification token.
  Future<String> verifyOtp({
    required String phone,
    required String otp,
  }) async {
    final res = await _dio.post(
      '/api/v1/members/otp/verify',
      data: {'phone': phone, 'otp': otp},
    );
    return res.data['verificationToken'] as String;
  }

  /// POST /api/v1/members/register — creates the member profile.
  Future<void> register({
    required String verificationToken,
    required String firstNameZht,
    required String lastNameZht,
    required String firstNameEn,
    required String lastNameEn,
    required String email,
  }) async {
    await _dio.post('/api/v1/members/register', data: {
      'verificationToken': verificationToken,
      'firstNameZht': firstNameZht,
      'lastNameZht': lastNameZht,
      'firstNameEn': firstNameEn,
      'lastNameEn': lastNameEn,
      'email': email,
    });
  }

  /// GET /api/v1/members/lookup?phone={phone} — find a member by phone (used for login flow).
  /// Returns a map with at least `{ "id": "<memberId>" }`.
  Future<String> findMemberByPhone(String phone) async {
    final res = await _dio.get(
      '/api/v1/members/lookup',
      queryParameters: {'phone': phone},
    );
    return res.data['id'] as String;
  }

  /// POST /api/v1/members/{id}/otp/generate — sends OTP to an existing member (login flow).
  Future<void> generateOtpById(String memberId) async {
    await _dio.post('/api/v1/members/$memberId/otp/generate');
  }

  /// POST /api/v1/members/{id}/otp/validate — validates OTP for an existing member (login flow).
  /// Returns the member profile data (or at minimum `{ "id": ... }`) on success.
  Future<Map<String, dynamic>> validateOtpById({
    required String memberId,
    required String otp,
  }) async {
    final res = await _dio.post(
      '/api/v1/members/$memberId/otp/validate',
      data: {'otp': otp},
    );
    return res.data as Map<String, dynamic>;
  }
}

final registrationServiceProvider = Provider<RegistrationService>(
  (ref) => RegistrationService(ref.read(apiClientProvider)),
);

/// Tracks the phone number across the registration flow screens.
final registrationPhoneProvider = StateProvider<String>((_) => '');
