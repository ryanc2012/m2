import '../../features/coupons/coupons_service.dart';
import '../../features/profile/profile_service.dart';
import '../../features/promotions/promotions_service.dart';

final demoProfile = MemberProfile(
  id: 'demo-member-001',
  phone: '+85291234567',
  firstNameZht: '大明',
  lastNameZht: '陳',
  firstNameEn: 'David',
  lastNameEn: 'CHAN',
  email: 'david.chan@demo.com',
  qrCode: 'DEMO-QR-001',
  memberSince: DateTime(2024, 1, 15),
);

final demoPromotions = <Promotion>[
  Promotion(
    id: 'demo-promo-001',
    nameEn: 'Summer Sale',
    nameZht: '夏季特賣',
    type: PromotionType.percentage,
    startDate: DateTime(2026, 6, 1),
    endDate: DateTime(2026, 8, 31),
    isActive: true,
    description: '20% off all items storewide.',
  ),
  Promotion(
    id: 'demo-promo-002',
    nameEn: 'Buy 2 Get 1',
    nameZht: '買二送一',
    type: PromotionType.buyXGetY,
    startDate: DateTime(2026, 5, 1),
    endDate: DateTime(2026, 7, 31),
    isActive: true,
    description: 'Buy any 2 items and get 1 free.',
  ),
  Promotion(
    id: 'demo-promo-003',
    nameEn: 'VIP Discount',
    nameZht: '會員折扣',
    type: PromotionType.fixed,
    startDate: DateTime(2026, 1, 1),
    endDate: DateTime(2026, 12, 31),
    isActive: true,
    description: '\$50 off your purchase for VIP members.',
  ),
];

final demoCoupons = <Coupon>[
  Coupon(
    id: 'demo-coupon-001',
    code: 'DEMO-SUMMER-2026',
    promotionId: 'demo-promo-001',
    promotionNameEn: 'Summer Sale',
    promotionNameZht: '夏季特賣',
    expiresAt: DateTime(2026, 12, 31),
    status: CouponStatus.active,
  ),
  Coupon(
    id: 'demo-coupon-002',
    code: 'DEMO-VIP-2026',
    promotionId: 'demo-promo-003',
    promotionNameEn: 'VIP Discount',
    promotionNameZht: '會員折扣',
    expiresAt: DateTime(2026, 6, 30),
    status: CouponStatus.redeemed,
  ),
  Coupon(
    id: 'demo-coupon-003',
    code: 'DEMO-OLD-2025',
    promotionId: 'demo-promo-legacy',
    promotionNameEn: 'Year-End Special',
    promotionNameZht: '年終優惠',
    expiresAt: DateTime(2025, 12, 31),
    status: CouponStatus.expired,
  ),
];
