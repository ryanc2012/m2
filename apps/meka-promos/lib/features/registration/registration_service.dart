import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';

class RegistrationService {
  RegistrationService(this._dio);
  final Dio _dio;

  /// POST /members/otp/send — sends OTP to the given phone number.
  Future<void> sendOtp(String phone) async {
    await _dio.post('/members/otp/send', data: {'phone': phone});
  }

  /// POST /members/otp/verify — verifies OTP; returns a short-lived verification token.
  Future<String> verifyOtp({
    required String phone,
    required String otp,
  }) async {
    final res = await _dio.post(
      '/members/otp/verify',
      data: {'phone': phone, 'otp': otp},
    );
    return res.data['verificationToken'] as String;
  }

  /// POST /members/register — creates the member profile.
  Future<void> register({
    required String verificationToken,
    required String firstNameZht,
    required String lastNameZht,
    required String firstNameEn,
    required String lastNameEn,
    required String email,
  }) async {
    await _dio.post('/members/register', data: {
      'verificationToken': verificationToken,
      'firstNameZht': firstNameZht,
      'lastNameZht': lastNameZht,
      'firstNameEn': firstNameEn,
      'lastNameEn': lastNameEn,
      'email': email,
    });
  }
}

final registrationServiceProvider = Provider<RegistrationService>(
  (ref) => RegistrationService(ref.read(apiClientProvider)),
);

/// Tracks the phone number across the registration flow screens.
final registrationPhoneProvider = StateProvider<String>((_) => '');
