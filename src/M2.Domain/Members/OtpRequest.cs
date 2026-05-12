namespace M2.Domain.Members;

/// <summary>
/// Transient OTP verification record. Not a full BaseEntity (no tenant/shop scoping required).
/// </summary>
public class OtpRequest
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid MemberId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    protected OtpRequest() { }

    public OtpRequest(Guid memberId, string code, TimeSpan ttl)
    {
        MemberId = memberId;
        Code = code;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiresAt = CreatedAt.Add(ttl);
        IsUsed = false;
    }

    public bool IsValid() => !IsUsed && DateTimeOffset.UtcNow < ExpiresAt;

    public void MarkUsed() => IsUsed = true;
}
