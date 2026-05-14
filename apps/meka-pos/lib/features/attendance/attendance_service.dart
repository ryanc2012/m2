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
    this.hoursWorkedToday,
  });

  final String id;
  final String employeeId;
  final AttendanceType type;
  final DateTime timestamp;

  /// Only populated in clock-out response.
  final double? hoursWorkedToday;

  factory AttendanceRecord.fromJson(Map<String, dynamic> json) => AttendanceRecord(
        id: json['id'] as String,
        employeeId: json['employeeId'] as String,
        type: json['type'] == 'clockIn' ? AttendanceType.clockIn : AttendanceType.clockOut,
        timestamp: DateTime.parse(json['timestamp'] as String),
        hoursWorkedToday: (json['hoursWorkedToday'] as num?)?.toDouble(),
      );
}

class AttendanceStatus {
  const AttendanceStatus({
    required this.isClockedIn,
    this.lastClockIn,
    this.hoursWorkedToday,
  });

  final bool isClockedIn;
  final DateTime? lastClockIn;
  final double? hoursWorkedToday;

  factory AttendanceStatus.fromJson(Map<String, dynamic> json) => AttendanceStatus(
        isClockedIn: json['isClockedIn'] as bool? ?? false,
        lastClockIn: json['lastClockIn'] != null
            ? DateTime.parse(json['lastClockIn'] as String)
            : null,
        hoursWorkedToday: (json['hoursWorkedToday'] as num?)?.toDouble(),
      );
}

class AttendanceService {
  AttendanceService(this._dio);
  final Dio _dio;

  /// GET /api/v1/attendance/status/{staffId}
  Future<AttendanceStatus> getStatus(String staffId) async {
    final res = await _dio.get('/api/v1/attendance/status/$staffId');
    return AttendanceStatus.fromJson(res.data as Map<String, dynamic>);
  }

  /// POST /api/v1/attendance/clock-in
  Future<AttendanceRecord> clockIn(String staffId) async {
    final res = await _dio.post('/api/v1/attendance/clock-in', data: {
      'staffId': staffId,
      'timestamp': DateTime.now().toIso8601String(),
    });
    return AttendanceRecord.fromJson(res.data as Map<String, dynamic>);
  }

  /// POST /api/v1/attendance/clock-out
  Future<AttendanceRecord> clockOut(String staffId) async {
    final res = await _dio.post('/api/v1/attendance/clock-out', data: {
      'staffId': staffId,
      'timestamp': DateTime.now().toIso8601String(),
    });
    return AttendanceRecord.fromJson(res.data as Map<String, dynamic>);
  }
}

final attendanceServiceProvider = Provider<AttendanceService>(
  (ref) => AttendanceService(ref.read(apiClientProvider)),
);
