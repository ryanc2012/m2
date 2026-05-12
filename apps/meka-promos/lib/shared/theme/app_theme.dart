import 'package:flutter/material.dart';

abstract final class AppColors {
  static const primary = Color(0xFF1A237E);
  static const secondary = Color(0xFF0288D1);
  static const error = Color(0xFFB71C1C);
  static const surface = Color(0xFFF5F5F5);
  static const darkSurface = Color(0xFF121212);
}

abstract final class AppTheme {
  static ThemeData get light => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: AppColors.primary,
          brightness: Brightness.light,
        ),
      );

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: AppColors.primary,
          brightness: Brightness.dark,
        ),
        scaffoldBackgroundColor: AppColors.darkSurface,
      );
}
