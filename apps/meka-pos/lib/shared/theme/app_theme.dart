import 'package:flutter/material.dart';

/// Meka POS brand palette.
abstract final class AppColors {
  static const primary = Color(0xFF1A237E);    // deep indigo — corporate
  static const secondary = Color(0xFF0288D1);  // blue accent
  static const error = Color(0xFFB71C1C);
  static const surface = Color(0xFFF5F5F5);
  static const darkSurface = Color(0xFF121212);
  static const onPrimary = Colors.white;
}

abstract final class AppTheme {
  static ThemeData get light => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: AppColors.primary,
          brightness: Brightness.light,
        ),
        // Tablet-first: generous tap targets (min 48dp enforced by M3 defaults).
        cardTheme: const CardTheme(elevation: 2),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            minimumSize: const Size(200, 56),
            textStyle: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
          ),
        ),
      );

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: AppColors.primary,
          brightness: Brightness.dark,
        ),
        scaffoldBackgroundColor: AppColors.darkSurface,
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            minimumSize: const Size(200, 56),
            textStyle: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
          ),
        ),
      );
}
