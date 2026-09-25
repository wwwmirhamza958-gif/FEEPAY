namespace FeePlatform.Modules.FeeEngine.Models.Instalments;

/// <summary>Percent shares that must sum to 100 (e.g. 50/50 or 40/30/30).</summary>
public sealed record InstalmentPlanDefinition(
    string Id,
    IReadOnlyList<decimal> PercentShares,
    IReadOnlyList<DateOnly>? DueDates = null,
    int SchemaVersion = 1)
{
    public void Validate()
    {
        if (PercentShares.Count == 0)
            throw new ArgumentException("Instalment plan requires at least one share.");
        var sum = PercentShares.Sum();
        if (sum != 100m)
            throw new ArgumentException($"Instalment shares must sum to 100; got {sum}.");
        if (DueDates is not null && DueDates.Count != PercentShares.Count)
            throw new ArgumentException("DueDates length must match PercentShares.");
    }
}
