import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

// Base URL placeholder — injected via --dart-define or flavour config.
const _kBaseUrl = 'https://api.placeholder.mekapos.com';

Dio _buildDio() {
  final dio = Dio(
    BaseOptions(
      baseUrl: _kBaseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      headers: {'Accept': 'application/json'},
    ),
  );

  // Auth interceptor stub — will be wired to MSAL token in Sprint 2.
  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (options, handler) {
        // TODO(Sprint 2): attach Bearer token from MSAL token cache.
        handler.next(options);
      },
      onError: (error, handler) {
        // TODO(Sprint 2): handle 401 → trigger silent token refresh.
        handler.next(error);
      },
    ),
  );

  return dio;
}

final apiClientProvider = Provider<Dio>((_) => _buildDio());
