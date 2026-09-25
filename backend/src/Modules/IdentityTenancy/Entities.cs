namespace FeePlatform.Modules.IdentityTenancy;

/// <summary>Every tenant-owned row carries TenantId. Platform tables are the exception.</summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}

public sealed class Tenant
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public TenantBranding? Branding { get; set; }
}

/// <summary>
/// Tenant colour is identity only (logo chip / 3px band) — never button fill.
/// </summary>
public sealed class TenantBranding
{
    public Guid TenantId { get; set; }
    public string? LogoObjectKey { get; set; }
    /// <summary>Hex brand colour for identity chrome only.</summary>
    public string BrandColourHex { get; set; } = "#1D4ED8";
    public Tenant? Tenant { get; set; }
}

public sealed class User : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? TotpSecretEncrypted { get; set; }
    public bool TotpEnabled { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class Role : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class Permission
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

/// <summary>Parent/student OTP challenge (plan §13). Rate limits applied at API layer.</summary>
public sealed class OtpChallenge
{
    public Guid Id { get; set; }
    /// <summary>Null for cross-tenant parent accounts before link; set when scoped.</summary>
    public Guid? TenantId { get; set; }
    public string MobileE164 { get; set; } = string.Empty;
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? IpAddress { get; set; }
    public string? DeviceFingerprint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class Session
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PayerProfileId { get; set; }
    public Guid? TenantId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Parent/guardian profile — CNIC/NTN encrypted at rest (column-level later).</summary>
public sealed class PayerProfile
{
    public Guid Id { get; set; }
    public string MobileE164 { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public byte[]? CnicEncrypted { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
