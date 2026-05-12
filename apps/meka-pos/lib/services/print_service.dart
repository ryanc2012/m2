import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';

import '../features/sales/sales_service.dart';

/// Generates a ZHT PDF receipt and sends it to the platform print dialog.
class PrintService {
  /// Builds a receipt PDF for [tx] and opens the system print/preview dialog.
  Future<void> printReceiptAsync(SaleTransaction tx) async {
    final doc = pw.Document();

    // Noto Sans TC covers Traditional Chinese characters (ADR-022: POS is ZHT-only).
    final font = await PdfGoogleFonts.notoSansTCRegular();
    final fontBold = await PdfGoogleFonts.notoSansTCBold();

    doc.addPage(
      pw.Page(
        pageFormat: PdfPageFormat.roll80,
        margin: const pw.EdgeInsets.all(12),
        build: (pw.Context context) {
          return pw.Column(
            crossAxisAlignment: pw.CrossAxisAlignment.stretch,
            children: [
              pw.Center(
                child: pw.Text(
                  'Meka 門市',
                  style: pw.TextStyle(font: fontBold, fontSize: 18),
                ),
              ),
              pw.SizedBox(height: 4),
              pw.Center(
                child: pw.Text(
                  '銷售收據',
                  style: pw.TextStyle(font: font, fontSize: 12),
                ),
              ),
              pw.Divider(height: 12),
              _row(font, fontBold, '交易編號', tx.transactionId),
              _row(font, fontBold, '日期', _formatDate(tx.createdAt)),
              _row(font, fontBold, '付款方式', tx.paymentMethod.label),
              if (tx.memberQrCode != null)
                _row(font, fontBold, '會員', tx.memberQrCode!),
              pw.Divider(height: 12),
              pw.Text('商品', style: pw.TextStyle(font: fontBold, fontSize: 10)),
              pw.SizedBox(height: 4),
              ...tx.items.map(
                (item) => pw.Padding(
                  padding: const pw.EdgeInsets.symmetric(vertical: 2),
                  child: pw.Row(
                    children: [
                      pw.Expanded(
                        child: pw.Text(
                          '${item.name} × ${item.quantity}',
                          style: pw.TextStyle(font: font, fontSize: 10),
                        ),
                      ),
                      pw.Text(
                        'RM ${item.subtotal.toStringAsFixed(2)}',
                        style: pw.TextStyle(font: font, fontSize: 10),
                      ),
                    ],
                  ),
                ),
              ),
              pw.Divider(height: 12),
              _row(font, fontBold, '小計', 'RM ${tx.subtotal.toStringAsFixed(2)}'),
              if (tx.discountAmount > 0)
                _row(font, fontBold, '折扣',
                    '- RM ${tx.discountAmount.toStringAsFixed(2)}'),
              _row(font, fontBold, '總計', 'RM ${tx.total.toStringAsFixed(2)}',
                  bold: true),
              pw.SizedBox(height: 16),
              pw.Center(
                child: pw.Text(
                  '謝謝惠顧！',
                  style: pw.TextStyle(font: font, fontSize: 12),
                ),
              ),
            ],
          );
        },
      ),
    );

    await Printing.layoutPdf(
      onLayout: (_) async => doc.save(),
      name: '收據_${tx.transactionId}',
    );
  }

  pw.Widget _row(
    pw.Font font,
    pw.Font fontBold,
    String label,
    String value, {
    bool bold = false,
  }) {
    return pw.Padding(
      padding: const pw.EdgeInsets.symmetric(vertical: 2),
      child: pw.Row(
        mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
        children: [
          pw.Text(label, style: pw.TextStyle(font: font, fontSize: 10)),
          pw.Text(
            value,
            style: pw.TextStyle(font: bold ? fontBold : font, fontSize: 10),
          ),
        ],
      ),
    );
  }

  String _formatDate(DateTime dt) =>
      '${dt.year}-${dt.month.toString().padLeft(2, '0')}-'
      '${dt.day.toString().padLeft(2, '0')} '
      '${dt.hour.toString().padLeft(2, '0')}:'
      '${dt.minute.toString().padLeft(2, '0')}';
}

final printServiceProvider = Provider<PrintService>((_) => PrintService());
