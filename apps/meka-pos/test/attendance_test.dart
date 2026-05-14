import 'package:flutter_test/flutter_test.dart';
import 'package:meka_pos/features/attendance/attendance_service.dart';

void main() {
  group('AttendanceStatus', () {
    test('fromJson sets isClockedIn from JSON boolean', () {
      final status = AttendanceStatus.fromJson({
        'isClockedIn': true,
        'lastClockIn': '2026-05-14T08:00:00.000Z',
        'hoursWorkedToday': 4.5,
      });

      expect(status.isClockedIn, isTrue);
      expect(status.lastClockIn, isNotNull);
      expect(status.hoursWorkedToday, closeTo(4.5, 0.001));
    });

    test('fromJson defaults isClockedIn to false when field absent', () {
      final status = AttendanceStatus.fromJson({});

      expect(status.isClockedIn, isFalse);
      expect(status.lastClockIn, isNull);
      expect(status.hoursWorkedToday, isNull);
    });

    test('AttendanceRecord.fromJson parses clockIn type correctly', () {
      final record = AttendanceRecord.fromJson({
        'id': 'rec-001',
        'employeeId': 'emp-42',
        'type': 'clockIn',
        'timestamp': '2026-05-14T09:00:00.000Z',
      });

      expect(record.id, 'rec-001');
      expect(record.employeeId, 'emp-42');
      expect(record.type, AttendanceType.clockIn);
      expect(record.hoursWorkedToday, isNull);
    });

    test('AttendanceRecord.fromJson parses clockOut with hoursWorkedToday', () {
      final record = AttendanceRecord.fromJson({
        'id': 'rec-002',
        'employeeId': 'emp-42',
        'type': 'clockOut',
        'timestamp': '2026-05-14T17:00:00.000Z',
        'hoursWorkedToday': 8.0,
      });

      expect(record.type, AttendanceType.clockOut);
      expect(record.hoursWorkedToday, closeTo(8.0, 0.001));
    });
  });
}
