import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/auth/auth_service.dart';
import 'attendance_service.dart';

class ClockInOutScreen extends ConsumerStatefulWidget {
  const ClockInOutScreen({super.key});

  @override
  ConsumerState<ClockInOutScreen> createState() => _ClockInOutScreenState();
}

class _ClockInOutScreenState extends ConsumerState<ClockInOutScreen> {
  AttendanceStatus? _status;
  bool _loading = false;
  bool _clocking = false;
  String? _error;
  String? _successMessage;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadStatus());
  }

  String get _staffId =>
      ref.read(authStateProvider).value?.accountId ?? 'unknown';

  Future<void> _loadStatus() async {
    setState(() => _loading = true);
    try {
      final service = ref.read(attendanceServiceProvider);
      final status = await service.getStatus(_staffId);
      setState(() => _status = status);
    } catch (_) {
      // No record yet — not an error
      setState(() => _status = null);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _clockIn() async {
    setState(() {
      _clocking = true;
      _error = null;
      _successMessage = null;
    });
    try {
      final service = ref.read(attendanceServiceProvider);
      final record = await service.clockIn(_staffId);
      setState(() {
        _status = AttendanceStatus(
          isClockedIn: true,
          lastClockIn: record.timestamp,
        );
        _successMessage = '上班打卡成功 — ${_formatTime(record.timestamp)}';
      });
    } catch (e) {
      setState(() => _error = '打卡失敗，請重試。');
    } finally {
      if (mounted) setState(() => _clocking = false);
    }
  }

  Future<void> _clockOut() async {
    setState(() {
      _clocking = true;
      _error = null;
      _successMessage = null;
    });
    try {
      final service = ref.read(attendanceServiceProvider);
      final record = await service.clockOut(_staffId);
      setState(() {
        _status = AttendanceStatus(isClockedIn: false);
        _successMessage = record.hoursWorkedToday != null
            ? '下班打卡成功 — ${_formatTime(record.timestamp)}｜今日工時：${record.hoursWorkedToday!.toStringAsFixed(1)}h'
            : '下班打卡成功 — ${_formatTime(record.timestamp)}';
      });
    } catch (e) {
      setState(() => _error = '打卡失敗，請重試。');
    } finally {
      if (mounted) setState(() => _clocking = false);
    }
  }

  String _formatTime(DateTime dt) {
    return '${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')}';
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(24),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Text('考勤打卡', style: theme.textTheme.titleLarge),
          const SizedBox(height: 24),

          // Current status card
          if (_loading)
            const Center(child: CircularProgressIndicator())
          else if (_status != null)
            Card(
              child: ListTile(
                leading: Icon(
                  _status!.isClockedIn ? Icons.login : Icons.logout,
                  color: _status!.isClockedIn
                      ? theme.colorScheme.primary
                      : theme.colorScheme.secondary,
                ),
                title: Text(
                  _status!.isClockedIn ? '目前狀態：已上班' : '目前狀態：未上班',
                ),
                subtitle: _status!.lastClockIn != null
                    ? Text('上班時間：${_formatTime(_status!.lastClockIn!)}')
                    : null,
                trailing: TextButton(
                  onPressed: _loading ? null : _loadStatus,
                  child: const Text('重新整理'),
                ),
              ),
            ),

          if (_error != null) ...[
            const SizedBox(height: 12),
            Text(_error!, style: TextStyle(color: theme.colorScheme.error)),
          ],
          if (_successMessage != null) ...[
            const SizedBox(height: 12),
            Row(
              children: [
                Icon(Icons.check_circle, color: theme.colorScheme.primary),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(_successMessage!,
                      style: TextStyle(color: theme.colorScheme.primary)),
                ),
              ],
            ),
          ],

          const SizedBox(height: 32),

          // Clock In button
          FilledButton.icon(
            onPressed: _clocking ? null : _clockIn,
            icon: const Icon(Icons.login),
            label: const Text('上班打卡', style: TextStyle(fontSize: 18)),
            style: FilledButton.styleFrom(
              minimumSize: const Size(double.infinity, 56),
              backgroundColor: theme.colorScheme.primary,
            ),
          ),
          const SizedBox(height: 16),

          // Clock Out button
          FilledButton.icon(
            onPressed: _clocking ? null : _clockOut,
            icon: const Icon(Icons.logout),
            label: const Text('下班打卡', style: TextStyle(fontSize: 18)),
            style: FilledButton.styleFrom(
              minimumSize: const Size(double.infinity, 56),
              backgroundColor: theme.colorScheme.secondary,
            ),
          ),
        ],
      ),
    );
  }
}
