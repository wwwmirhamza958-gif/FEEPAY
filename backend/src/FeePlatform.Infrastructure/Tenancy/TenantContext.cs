namespace FeePlatform.Infrastructure.Tenancy;

/// <summary>
/// Ambient tenant for the current request/job. Jobs without a tenant must fail closed (§5.3).
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }
    bool IsPlatformAdminBypass { get; }
    void SetTenant(Guid tenantId);
    void SetPlatformAdminBypass(bool enabled);
    void Clear();
}

public sealed class TenantContext : ITenantContext
{
    private static readonly AsyncLocal<Guid?> CurrentTenant = new();
    private static readonly AsyncLocal<bool> PlatformBypass = new();

    public Guid? TenantId => CurrentTenant.Value;
    public bool IsPlatformAdminBypass => PlatformBypass.Value;

    public void SetTenant(Guid tenantId) => CurrentTenant.Value = tenantId;

    public void SetPlatformAdminBypass(bool enabled) => PlatformBypass.Value = enabled;

    public void Clear()
    {
        CurrentTenant.Value = null;
        PlatformBypass.Value = false;
    }
}
