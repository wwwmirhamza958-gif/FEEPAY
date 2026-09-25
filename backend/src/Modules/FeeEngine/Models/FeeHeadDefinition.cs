namespace FeePlatform.Modules.FeeEngine.Models;

/// <summary>Fee head flags per plan §7.1 / CONTRACT §5.5.</summary>
public sealed record FeeHeadDefinition(
    string Code,
    string Name,
    bool Refundable,
    bool TaxableFor236I,
    bool Discountable,
    bool OneTime);
