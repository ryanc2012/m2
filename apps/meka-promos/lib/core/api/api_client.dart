import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

const _kBaseUrl = String.fromEnvironment(
  'BASE_URL',
  defaultValue: 'https://localhost:5001',
);

// Injected via --dart-define=API_KEY=... in CI. Dev default is safe/non-secret.
const _kApiKey = String.fromEnvironment(
  'API_KEY',
  defaultValue: 'meka-promos-dev-key',
);

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
        options.headers['X-Api-Key'] = _kApiKey;
        handler.next(options);
      },
      onError: (error, handler) {
        handler.next(error);
      },
    ),
  );

  return dio;
}

final apiClientProvider = Provider<Dio>((_) => _buildDio());