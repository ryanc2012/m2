import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';

class MemberInfo {
  const MemberInfo({
    required this.id,
    required this.name,
    required this.tier,
    required this.qrCode,
  });

  final String id;
  final String name;

  /// Membership tier, e.g. "Silver", "Gold", "Platinum".
  final String tier;
  final String qrCode;

  factory MemberInfo.fromJson(Map<String, dynamic> json) => MemberInfo(
        id: json['id'] as String,
        name: json['name'] as String,
        tier: json['tier'] as String,
        qrCode: json['qrCode'] as String,
      );
}

/// Stub service — calls MekaPosBff GET /members/qr/{code}.
class MemberLookupService {
  MemberLookupService(this._dio);
  final Dio _dio;

  Future<MemberInfo> lookupByQr(String qrCode) async {
    final res = await _dio.get('/members/qr/$qrCode');
    return MemberInfo.fromJson(res.data as Map<String, dynamic>);
  }
}

final memberLookupServiceProvider = Provider<MemberLookupService>(
  (ref) => MemberLookupService(ref.read(apiClientProvider)),
);
