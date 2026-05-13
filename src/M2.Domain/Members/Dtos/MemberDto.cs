namespace M2.Domain.Members.Dtos;

public record MemberDto(
    Guid Id,
    Guid TenantId,
    Guid ShopId,
    string FirstNameEn,
    string FirstNameZht,
    string LastNameEn,
    string LastNameZht,
    string Phone,
    string? Email,
    string QrCode,
    string MembershipTier,
    DateTimeOffset JoinedAt,
    bool IsActive);

public record RegisterMemberPayload(
    Guid TenantId,
    Guid ShopId,
    string FirstNameEn,
    string FirstNameZht,
    string LastNameEn,
    string LastNameZht,
    string Phone,
    string? Email,
    string MembershipTier);

public record UpdateMemberProfilePayload(
    string FirstNameEn,
    string FirstNameZht,
    string LastNameEn,
    string LastNameZht,
    string? Email);
