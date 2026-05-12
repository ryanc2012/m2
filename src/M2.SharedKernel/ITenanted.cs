namespace M2.SharedKernel;

public interface ITenanted
{
    Guid TenantId { get; }
}
