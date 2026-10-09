namespace FeePlatform.Modules.IdentityTenancy.Seed;

/// <summary>Pilot seed constants — CONTRACT.md §5. Secrets stay in secret store (secret_ref only).</summary>
public static class PilotSeed
{
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public const string Slug = "pilot-u";
    public const string DisplayName = "Pilot University (Placeholder)";
    public const string Timezone = "Asia/Karachi";
    public const string LocaleDefault = "en";
    public const string Currency = "PKR";
    /// <summary>Identity only — never Pay button fill.</summary>
    public const string BrandPrimaryHex = "#0B3D91";

    public static readonly string[] AdapterCodes =
    [
        "raast-p2m-pilot",
        "jazzcash-pilot",
        "easypaisa-pilot",
        "card-psp-pilot",
        "biller-cn-pilot",
    ];

    public static string AdapterSecretRef(string code) =>
        $"tenant/{TenantId:D}/adapters/{code}";

    public const string GuardianMobileFake = "+923001234567";
}
