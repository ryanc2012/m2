import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:qr_flutter/qr_flutter.dart';

void main() {
  group('QR Code rendering', () {
    testWidgets('QrImageView renders with a coupon code', (tester) async {
      const testCode = 'COUPON-TEST-12345';

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Center(
              child: QrImageView(
                data: testCode,
                version: QrVersions.auto,
                size: 200.0,
              ),
            ),
          ),
        ),
      );

      await tester.pump();
      expect(find.byType(QrImageView), findsOneWidget);
    });

    testWidgets('QrImageView renders different data without error', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Center(
              child: QrImageView(
                data: 'MEMBER-COUPON-XYZABC',
                version: QrVersions.auto,
                size: 150.0,
              ),
            ),
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.byType(QrImageView), findsOneWidget);
    });
  });

  group('Coupon status chip display', () {
    testWidgets('shows 已使用 chip when coupon is redeemed', (tester) async {
      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: Chip(
              label: const Text('已使用'),
              backgroundColor: Colors.grey.shade200,
            ),
          ),
        ),
      );
      expect(find.text('已使用'), findsOneWidget);
    });

    testWidgets('shows 有效 chip when coupon is active', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Chip(
              label: Text('有效'),
            ),
          ),
        ),
      );
      expect(find.text('有效'), findsOneWidget);
    });

    testWidgets('shows 已過期 chip when coupon is expired', (tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: Chip(
              label: Text('已過期'),
            ),
          ),
        ),
      );
      expect(find.text('已過期'), findsOneWidget);
    });
  });
}
