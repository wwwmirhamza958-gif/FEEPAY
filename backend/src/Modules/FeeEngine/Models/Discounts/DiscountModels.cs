namespace FeePlatform.Modules.FeeEngine.Models.Discounts;

public enum DiscountKind
{
    Percentage,
    FixedAmount,
}

public enum DiscountCategory
{
    Merit,
    NeedBased,
    Sibling,
    StaffChild,
    Sports,
    ExternalSponsor,
}

public sealed record DiscountRuleDefinition(
    string Id,
    string Name,
    DiscountCategory Category,
    DiscountKind Kind,
    decimal Value,
    IReadOnlyList<string> TargetFeeHeadCodes,
    int SchemaVersion = 1)
{
    public void Validate()
    {
        if (SchemaVersion < 1)
            throw new ArgumentException("Discount schema_version must be >= 1.");
        if (Kind == DiscountKind.Percentage && (Value < 0 || Value > 100))
            throw new ArgumentException($"Percentage discount {Id} must be 0–100.");
        if (Kind == DiscountKind.FixedAmount && Value < 0)
            throw new ArgumentException($"Fixed discount {Id} must be >= 0.");
        if (TargetFeeHeadCodes.Count == 0)
            throw new ArgumentException($"Discount {Id} must target at least one fee head.");
    }
}

/// <summary>
/// Tenant stacking policy: category order, whether stacking is allowed, and optional caps.
/// </summary>
public sealed record DiscountStackingPolicy(
    IReadOnlyList<DiscountCategory> Order,
    bool AllowStacking = true,
    decimal? MaxTotalDiscountAmount = null,
    decimal? MaxTotalDiscountPercentOfEligible = null)
{
    public static DiscountStackingPolicy Default { get; } = new(
        [
            DiscountCategory.Merit,
            DiscountCategory.NeedBased,
            DiscountCategory.Sibling,
            DiscountCategory.StaffChild,
            DiscountCategory.Sports,
            DiscountCategory.ExternalSponsor,
        ],
        AllowStacking: true);
}
