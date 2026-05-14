// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'Meka Promos';

  @override
  String get signIn => 'Sign In';

  @override
  String get signOut => 'Sign Out';

  @override
  String get language => 'Language';

  @override
  String get home => 'Home';

  @override
  String get promotions => 'Promotions';

  @override
  String get myQr => 'My QR';

  @override
  String get notifications => 'Notifications';

  @override
  String get profile => 'Profile';

  @override
  String get errorOccurred => 'An error occurred. Please try again.';

  @override
  String get phoneNumber => 'Phone Number';

  @override
  String get sendOtp => 'Send OTP';

  @override
  String get otpVerification => 'OTP Verification';

  @override
  String otpSentTo(String phone) {
    return 'OTP sent to $phone';
  }

  @override
  String resendIn(int seconds) {
    return 'Resend in ${seconds}s';
  }

  @override
  String get resendOtp => 'Resend OTP';

  @override
  String get profileSetup => 'Profile Setup';

  @override
  String get firstNameZht => 'First Name (Traditional Chinese)';

  @override
  String get lastNameZht => 'Last Name (Traditional Chinese)';

  @override
  String get firstNameEn => 'First Name (English)';

  @override
  String get lastNameEn => 'Last Name (English)';

  @override
  String get emailAddress => 'Email Address';

  @override
  String get completeRegistration => 'Complete Registration';

  @override
  String get editProfile => 'Edit Profile';

  @override
  String get save => 'Save';

  @override
  String get cancel => 'Cancel';

  @override
  String get memberCard => 'Member Card';

  @override
  String get memberSince => 'Member Since';

  @override
  String get phone => 'Phone';

  @override
  String get registrationTitle => 'Join Meka';

  @override
  String get registrationSubtitle => 'Enter your phone number to get started';

  @override
  String get coupons => 'Coupons';

  @override
  String get loginTitle => 'Member Login';

  @override
  String get newMemberRegister => 'New member? Register';
}
