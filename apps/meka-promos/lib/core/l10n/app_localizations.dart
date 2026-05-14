import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_zh.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
      : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
    delegate,
    GlobalMaterialLocalizations.delegate,
    GlobalCupertinoLocalizations.delegate,
    GlobalWidgetsLocalizations.delegate,
  ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale.fromSubtags(languageCode: 'zh', scriptCode: 'Hans'),
    Locale('zh')
  ];

  /// Application title
  ///
  /// In zh, this message translates to:
  /// **'Meka 優惠'**
  String get appTitle;

  /// Sign-in button label
  ///
  /// In zh, this message translates to:
  /// **'登入'**
  String get signIn;

  /// Sign-out button
  ///
  /// In zh, this message translates to:
  /// **'登出'**
  String get signOut;

  /// Language label
  ///
  /// In zh, this message translates to:
  /// **'語言'**
  String get language;

  /// Home nav item
  ///
  /// In zh, this message translates to:
  /// **'首頁'**
  String get home;

  /// Promotions nav item
  ///
  /// In zh, this message translates to:
  /// **'優惠'**
  String get promotions;

  /// My QR nav item
  ///
  /// In zh, this message translates to:
  /// **'我的QR碼'**
  String get myQr;

  /// Notifications nav item
  ///
  /// In zh, this message translates to:
  /// **'通知'**
  String get notifications;

  /// Profile nav item
  ///
  /// In zh, this message translates to:
  /// **'個人資料'**
  String get profile;

  /// Generic error message
  ///
  /// In zh, this message translates to:
  /// **'發生錯誤，請重試。'**
  String get errorOccurred;

  /// Phone number field label
  ///
  /// In zh, this message translates to:
  /// **'電話號碼'**
  String get phoneNumber;

  /// Send OTP button
  ///
  /// In zh, this message translates to:
  /// **'發送OTP'**
  String get sendOtp;

  /// OTP verification screen title
  ///
  /// In zh, this message translates to:
  /// **'OTP驗證'**
  String get otpVerification;

  /// OTP sent confirmation
  ///
  /// In zh, this message translates to:
  /// **'OTP已發送至 {phone}'**
  String otpSentTo(String phone);

  /// Resend OTP countdown
  ///
  /// In zh, this message translates to:
  /// **'{seconds}秒後重發'**
  String resendIn(int seconds);

  /// Resend OTP button
  ///
  /// In zh, this message translates to:
  /// **'重新發送OTP'**
  String get resendOtp;

  /// Profile setup screen title
  ///
  /// In zh, this message translates to:
  /// **'設定個人資料'**
  String get profileSetup;

  /// First name ZHT field label
  ///
  /// In zh, this message translates to:
  /// **'名字（繁體中文）'**
  String get firstNameZht;

  /// Last name ZHT field label
  ///
  /// In zh, this message translates to:
  /// **'姓氏（繁體中文）'**
  String get lastNameZht;

  /// First name EN field label
  ///
  /// In zh, this message translates to:
  /// **'名字（英文）'**
  String get firstNameEn;

  /// Last name EN field label
  ///
  /// In zh, this message translates to:
  /// **'姓氏（英文）'**
  String get lastNameEn;

  /// Email address field label
  ///
  /// In zh, this message translates to:
  /// **'電子郵件'**
  String get emailAddress;

  /// Complete registration button
  ///
  /// In zh, this message translates to:
  /// **'完成登記'**
  String get completeRegistration;

  /// Edit profile screen title / button
  ///
  /// In zh, this message translates to:
  /// **'編輯個人資料'**
  String get editProfile;

  /// Save button
  ///
  /// In zh, this message translates to:
  /// **'儲存'**
  String get save;

  /// Cancel button
  ///
  /// In zh, this message translates to:
  /// **'取消'**
  String get cancel;

  /// Member card / QR section label
  ///
  /// In zh, this message translates to:
  /// **'會員卡'**
  String get memberCard;

  /// Member since label
  ///
  /// In zh, this message translates to:
  /// **'入會日期'**
  String get memberSince;

  /// Phone label on profile
  ///
  /// In zh, this message translates to:
  /// **'電話'**
  String get phone;

  /// Registration screen title
  ///
  /// In zh, this message translates to:
  /// **'加入Meka'**
  String get registrationTitle;

  /// Registration screen subtitle
  ///
  /// In zh, this message translates to:
  /// **'輸入您的電話號碼以開始'**
  String get registrationSubtitle;

  /// Coupons nav item
  ///
  /// In zh, this message translates to:
  /// **'優惠券'**
  String get coupons;

  /// Login screen title for returning members
  ///
  /// In zh, this message translates to:
  /// **'會員登入'**
  String get loginTitle;

  /// Link to registration from login screen
  ///
  /// In zh, this message translates to:
  /// **'新會員？立即註冊'**
  String get newMemberRegister;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'zh'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when language+script codes are specified.
  switch (locale.languageCode) {
    case 'zh':
      {
        switch (locale.scriptCode) {
          case 'Hans':
            return AppLocalizationsZhHans();
        }
        break;
      }
  }

  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'zh':
      return AppLocalizationsZh();
  }

  throw FlutterError(
      'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
      'an issue with the localizations generation tool. Please file an issue '
      'on GitHub with a reproducible sample app and the gen-l10n configuration '
      'that was used.');
}
