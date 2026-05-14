// Demo mode bypasses all API calls and MSAL authentication so the app can be
// run for presentation purposes without a live backend.
//
// Enable with:  flutter run --dart-define=DEMO_MODE=true
//
// When kDemoMode is true, ProviderScope in main.dart injects static fixture
// data for memberSessionProvider, memberProfileProvider,
// activePromotionsProvider, and myCouponsProvider.  The router auth guard sees
// a pre-populated session and navigates directly to '/' without hitting
// /registration.

const kDemoMode = bool.fromEnvironment('DEMO_MODE', defaultValue: false);
