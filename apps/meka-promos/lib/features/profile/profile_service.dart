import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';

class MemberProfile {
  const MemberProfile({
    required this.id,
    required this.phone,
    required this.firstNameZht,
    required this.lastNameZht,
    required this.firstNameEn,
    required this.lastNameEn,
    required this.email,
    required this.qrCode,
    required this.memberSince,
  });

  final String id;
  final String phone;
  final String firstNameZht;
  final String lastNameZht;
  final String firstNameEn;
  final String lastNameEn;
  final String email;

  /// Reference ID embedded in QR code — scanned by POS for server-side validation (ADR-019).
  final String qrCode;
  final DateTime memberSince;

  String get displayNameEn => '$firstNameEn $lastNameEn';
  String get displayNameZht => '$lastNameZht$firstNameZht';

  MemberProfile copyWith({
    String? firstNameZht,
    String? lastNameZht,
    String? firstNameEn,
    String? lastNameEn,
    String? email,
  }) {
    return MemberProfile(
      id: id,
      phone: phone,
      firstNameZht: firstNameZht ?? this.firstNameZht,
      lastNameZht: lastNameZht ?? this.lastNameZht,
      firstNameEn: firstNameEn ?? this.firstNameEn,
      lastNameEn: lastNameEn ?? this.lastNameEn,
      email: email ?? this.email,
      qrCode: qrCode,
      memberSince: memberSince,
    );
  }

  factory MemberProfile.fromJson(Map<String, dynamic> json) => MemberProfile(
        id: json['id'] as String,
        phone: json['phone'] as String,
        firstNameZht: json['firstNameZht'] as String,
        lastNameZht: json['lastNameZht'] as String,
        firstNameEn: json['firstNameEn'] as String,
        lastNameEn: json['lastNameEn'] as String,
        email: json['email'] as String,
        qrCode: json['qrCode'] as String,
        memberSince: DateTime.parse(json['memberSince'] as String),
      );
}

class ProfileService {
  ProfileService(this._dio);
  final Dio _dio;

  /// GET /members/me
  Future<MemberProfile> getProfile() async {
    final res = await _dio.get('/members/me');
    return MemberProfile.fromJson(res.data as Map<String, dynamic>);
  }

  /// PUT /members/me
  Future<MemberProfile> updateProfile(MemberProfile profile) async {
    final res = await _dio.put('/members/me', data: {
      'firstNameZht': profile.firstNameZht,
      'lastNameZht': profile.lastNameZht,
      'firstNameEn': profile.firstNameEn,
      'lastNameEn': profile.lastNameEn,
      'email': profile.email,
    });
    return MemberProfile.fromJson(res.data as Map<String, dynamic>);
  }
}

final profileServiceProvider = Provider<ProfileService>(
  (ref) => ProfileService(ref.read(apiClientProvider)),
);

final memberProfileProvider = FutureProvider<MemberProfile>((ref) async {
  return ref.read(profileServiceProvider).getProfile();
});

/// Post-registration session state. Set after successful OTP registration;
/// cleared on sign-out. Used by go_router for auth guard.
final memberSessionProvider = StateProvider<MemberProfile?>((_) => null);
