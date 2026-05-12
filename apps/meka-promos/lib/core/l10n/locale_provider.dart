import 'package:flutter_riverpod/flutter_riverpod.dart';

// Persisted locale preference — defaults to ZHT (Traditional Chinese).
// ADR-022: member app supports ZHT / ZHS / EN.
final localeProvider = StateProvider<dynamic>((_) => null); // null = system default
