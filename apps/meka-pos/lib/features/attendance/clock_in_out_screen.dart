import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'attendance_service.dart';

class ClockInOutScreen extends ConsumerStatefulWidget {
  const ClockInOutScreen({super.key});

  @override
  ConsumerState<ClockInOutScreen> createState() => _ClockInOutScreenState();
}

class _ClockInOutScreenState extends ConsumerState<ClockInOutScreen> {
  final _empCtrl = TextEditingController();
  AttendanceRecord? _lastRecord;
  bool _loading = false;
  bool _clocking = false;
  String? _error;
  String? _successMessage;

  @override
  void dispose() {
    _empCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadLastRecord() async {
    final id = _empCtrl.text.trim();
    if (id.isEmpty) return;
    setState(() => _loading = true);
    try {
      final service = ref.read(attendanceServiceProvider);
      final record = await service.getLastRecord(id);
      setState(() => _lastRecord = record);
    } catch (_) {
      // No record yet — not an error
      setState(() => _lastRecord = null);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _clockIn() async {
    final id = _empCtrl.text.trim();
    if (id.isEmpty) {
      setState(() => _error = '請輸入員工 ID');
      return;
    }
    setState(() {
      _clocking = true;
      _error = null;
      _successMessage = null;
    });
    try {
      final service = ref.read(attendanceServiceProvider);
      final record = await service.clockIn(id);
      setState(() {
        _lastRecord = record;
        _successMessage = '上班打卡成功 — ${_formatTime(record.timestamp)}';
      });
    } catch (e) {
      setState(() => _error = '打卡失敗，請重試。');
    } finally {
      if (mounted) setState(() => _clocking = false);
    }
  }

  Future<void> _clockOut() async {
    final id = _empCtrl.text.trim();
    if (id.isEmpty) {
      setState(() => _error = '請輸入員工 ID');
      return;
    }
    setState(() {
      _clocking = true;
      _error = null;
      _successMessage = null;
    });
    try {
      final service = ref.read(attendanceServiceProvider);
      final record = await service.clockOut(id);
      setState(() {
        _lastRecord = record;
        _successMessage = '下班打卡成功 — ${_formatTime(record.timestamp)}';
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

          // Employee ID input
          TextField(
            controller: _empCtrl,
            keyboardType: TextInputType.number,
            decoration: const InputDecoration(
              labelText: '員工 ID',
              prefixIcon: Icon(Icons.badge_outlined),
              border: OutlineInputBorder(),
            ),
            onSubmitted: (_) => _loadLastRecord(),
          ),
          const SizedBox(height: 8),
          TextButton(
            onPressed: _loading ? null : _loadLastRecord,
            child: const Text('查詢上次打卡記錄'),
          ),

          // Last record display
          if (_lastRecord != null) ...[
            const SizedBox(height: 12),
            Card(
              child: ListTile(
                leading: Icon(
                  _lastRecord!.type == AttendanceType.clockIn
                      ? Icons.login
                      : Icons.logout,
                  color: _lastRecord!.type == AttendanceType.clockIn
                      ? theme.colorScheme.primary
                      : theme.colorScheme.secondary,
                ),
                title: Text(
                  _lastRecord!.type == AttendanceType.clockIn ? '上班打卡' : '下班打卡',
                ),
                subtitle: Text(_formatTime(_lastRecord!.timestamp)),
              ),
            ),
          ],

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
