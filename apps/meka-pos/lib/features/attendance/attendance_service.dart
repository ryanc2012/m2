import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/api/api_client.dart';

enum AttendanceType { clockIn, clockOut }

class AttendanceRecord {
  const AttendanceRecord({
    required this.id,
    required this.employeeId,
    required this.type,
    required this.timestamp,
  });

  final String id;
  final String employeeId;
  final AttendanceType type;
  final DateTime timestamp;

  factory AttendanceRecord.fromJson(Map<String, dynamic> json) => AttendanceRecord(
        id: json['id'] as String,
        employeeId: json['employeeId'] as String,
        type: json['type'] == 'clockIn' ? AttendanceType.clockIn : AttendanceType.clockOut,
        timestamp: DateTime.parse(json['timestamp'] as String),
      );
}

/// Stub service — calls MekaPosBff /attendance endpoints.
class AttendanceService {
  AttendanceService(this._dio);
  final Dio _dio;

  /// POST /attendance/clock-in
  Future<AttendanceRecord> clockIn(String employeeId) async {
    final res = await _dio.post('/attendance/clock-in', data: {'employeeId': employeeId});
    return AttendanceRecord.fromJson(res.data as Map<String, dynamic>);
  }

  /// POST /attendance/clock-out
  Future<AttendanceRecord> clockOut(String employeeId) async {
    final res = await _dio.post('/attendance/clock-out', data: {'employeeId': employeeId});
    return AttendanceRecord.fromJson(res.data as Map<String, dynamic>);
  }

  /// GET /attendance/last?employeeId={id}
  Future<AttendanceRecord?> getLastRecord(String employeeId) async {
    final res = await _dio.get('/attendance/last', queryParameters: {'employeeId': employeeId});
    if (res.data == null) return null;
    return AttendanceRecord.fromJson(res.data as Map<String, dynamic>);
  }
}

final attendanceServiceProvider = Provider<AttendanceService>(
  (ref) => AttendanceService(ref.read(apiClientProvider)),
);
