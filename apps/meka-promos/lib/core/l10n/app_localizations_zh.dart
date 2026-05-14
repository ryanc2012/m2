// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Chinese (`zh`).
class AppLocalizationsZh extends AppLocalizations {
  AppLocalizationsZh([String locale = 'zh']) : super(locale);

  @override
  String get appTitle => 'Meka 優惠';

  @override
  String get signIn => '登入';

  @override
  String get signOut => '登出';

  @override
  String get language => '語言';

  @override
  String get home => '首頁';

  @override
  String get promotions => '優惠';

  @override
  String get myQr => '我的QR碼';

  @override
  String get notifications => '通知';

  @override
  String get profile => '個人資料';

  @override
  String get errorOccurred => '發生錯誤，請重試。';

  @override
  String get phoneNumber => '電話號碼';

  @override
  String get sendOtp => '發送OTP';

  @override
  String get otpVerification => 'OTP驗證';

  @override
  String otpSentTo(String phone) {
    return 'OTP已發送至 $phone';
  }

  @override
  String resendIn(int seconds) {
    return '$seconds秒後重發';
  }

  @override
  String get resendOtp => '重新發送OTP';

  @override
  String get profileSetup => '設定個人資料';

  @override
  String get firstNameZht => '名字（繁體中文）';

  @override
  String get lastNameZht => '姓氏（繁體中文）';

  @override
  String get firstNameEn => '名字（英文）';

  @override
  String get lastNameEn => '姓氏（英文）';

  @override
  String get emailAddress => '電子郵件';

  @override
  String get completeRegistration => '完成登記';

  @override
  String get editProfile => '編輯個人資料';

  @override
  String get save => '儲存';

  @override
  String get cancel => '取消';

  @override
  String get memberCard => '會員卡';

  @override
  String get memberSince => '入會日期';

  @override
  String get phone => '電話';

  @override
  String get registrationTitle => '加入Meka';

  @override
  String get registrationSubtitle => '輸入您的電話號碼以開始';

  @override
  String get coupons => '優惠券';

  @override
  String get loginTitle => '會員登入';

  @override
  String get newMemberRegister => '新會員？立即註冊';
}

/// The translations for Chinese, using the Han script (`zh_Hans`).
class AppLocalizationsZhHans extends AppLocalizationsZh {
  AppLocalizationsZhHans() : super('zh_Hans');

  @override
  String get appTitle => 'Meka 优惠';

  @override
  String get signIn => '登录';

  @override
  String get signOut => '登出';

  @override
  String get language => '语言';

  @override
  String get home => '首页';

  @override
  String get promotions => '优惠';

  @override
  String get myQr => '我的二维码';

  @override
  String get notifications => '通知';

  @override
  String get profile => '个人资料';

  @override
  String get errorOccurred => '发生错误，请重试。';

  @override
  String get phoneNumber => '电话号码';

  @override
  String get sendOtp => '发送OTP';

  @override
  String get otpVerification => 'OTP验证';

  @override
  String otpSentTo(String phone) {
    return 'OTP已发送至 $phone';
  }

  @override
  String resendIn(int seconds) {
    return '$seconds秒后重发';
  }

  @override
  String get resendOtp => '重新发送OTP';

  @override
  String get profileSetup => '设置个人资料';

  @override
  String get firstNameZht => '名字（繁体中文）';

  @override
  String get lastNameZht => '姓氏（繁体中文）';

  @override
  String get firstNameEn => '名字（英文）';

  @override
  String get lastNameEn => '姓氏（英文）';

  @override
  String get emailAddress => '电子邮件';

  @override
  String get completeRegistration => '完成注册';

  @override
  String get editProfile => '编辑个人资料';

  @override
  String get save => '保存';

  @override
  String get cancel => '取消';

  @override
  String get memberCard => '会员卡';

  @override
  String get memberSince => '入会日期';

  @override
  String get phone => '电话';

  @override
  String get registrationTitle => '加入Meka';

  @override
  String get registrationSubtitle => '输入您的电话号码以开始';

  @override
  String get coupons => '优惠券';

  @override
  String get loginTitle => '会员登录';

  @override
  String get newMemberRegister => '新会员？立即注册';
}
