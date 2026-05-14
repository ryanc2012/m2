import 'package:dio/dio.dart';

import '../../features/profile/profile_service.dart';
import '../../features/registration/registration_service.dart';
import 'demo_data.dart';

/// Fake [RegistrationService] for demo mode.
/// All network calls are no-ops or return hard-coded demo values.
class DemoRegistrationService extends RegistrationService {
  DemoRegistrationService() : super(Dio());

  @override
  Future<void> sendOtp(String phone) async {}

  @override
  Future<String> verifyOtp({required String phone, required String otp}) async {
    return 'DEMO-TOKEN-001';
  }

  @override
  Future<void> register({
    required String verificationToken,
    required String firstNameZht,
    required String lastNameZht,
    required String firstNameEn,
    required String lastNameEn,
    required String email,
  }) async {}

  @override
  Future<String> findMemberByPhone(String phone) async {
    return 'demo-member-001';
  }

  @override
  Future<void> generateOtpById(String memberId) async {}

  @override
  Future<Map<String, dynamic>> validateOtpById({
    required String memberId,
    required String otp,
  }) async {
    return {'id': 'demo-member-001', 'phone': demoProfile.phone};
  }
}

/// Fake [ProfileService] for demo mode.
class DemoProfileService extends ProfileService {
  DemoProfileService() : super(Dio());

  @override
  Future<MemberProfile> getProfile() async => demoProfile;
}
