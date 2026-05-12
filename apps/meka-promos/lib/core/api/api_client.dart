import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

const _kBaseUrl = 'https://api.placeholder.mekapromos.com';

Dio _buildDio() {
  final dio = Dio(
    BaseOptions(
      baseUrl: _kBaseUrl,
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      headers: {'Accept': 'application/json'},
    ),
  );

  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (options, handler) {
        // TODO(Sprint 2): attach Bearer token from MSAL token cache.
        handler.next(options);
      },
      onError: (error, handler) {
        // TODO(Sprint 2): handle 401 → silent token refresh.
        handler.next(error);
      },
    ),
  );

  return dio;
}

final apiClientProvider = Provider<Dio>((_) => _buildDio());
