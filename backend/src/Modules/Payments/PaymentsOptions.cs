namespace FeePlatform.Modules.Payments;

public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>
    /// When true AND host environment is Development, Tier-1 adapters may complete without
    /// real merchant credentials (FakeTier1 path). Must never be enabled in Production.
    /// </summary>
    public bool AllowSandboxFake { get; set; }

    /// <summary>HMAC key for pay-link tokens. Dev default is non-secret placeholder.</summary>
    public string PayLinkSigningKey { get; set; } = "dev-pay-link-signing-key-change-me";

    /// <summary>Public base URL used in receipt QR verify links.</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5137";

    /// <summary>Per-adapter credential/config keyed by adapter code. Secrets via config/secret refs only.</summary>
    public Dictionary<string, AdapterCredentialOptions> Adapters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AdapterCredentialOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>Secret store reference name (not the secret value).</summary>
    public string? SecretRef { get; set; }
    /// <summary>Optional HMAC / API key loaded from configuration or secret provider — never hard-code.</summary>
    public string? CallbackHmacSecret { get; set; }
    public string? MerchantId { get; set; }
    public string? ApiBaseUrl { get; set; }
}
