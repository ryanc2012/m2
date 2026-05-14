import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

const _kBaseUrl = String.fromEnvironment(
  'BASE_URL',
  defaultValue: 'https://localhost:5000',
);

String? _cachedToken;

/// Called by AuthService after a successful MSAL token acquisition.
void setAuthToken(String? token) => _cachedToken = token;

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
      onRequest: (options, handler) async {
        if (_cachedToken != null) {
          options.headers['Authorization'] = 'Bearer $_cachedToken';
        }
        handler.next(options);
      },
      onError: (error, handler) {
        if (error.response?.statusCode == 401) {
          // TODO(Sprint 7): trigger silent token refresh via MSAL.
        }
        handler.next(error);
      },
    ),
  );

  return dio;
}

final apiClientProvider = Provider<Dio>((_) => _buildDio());