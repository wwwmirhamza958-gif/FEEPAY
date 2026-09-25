namespace FeePlatform.Modules.FeeEngine.Models.LateFees;

public enum LateFeeMode
{
    Fixed,
    PerDay,
    PerWeek,
}

/// <summary>Pure late-fee calculation input (plan §7.5) — no job / persistence.</summary>
public sealed record LateFeeRequest(
    LateFeeMode Mode,
    decimal Rate,
    int Units,
    decimal? Cap = null,
    string FeeHeadCode = "LATE_FEE",
    string Description = "Late fee",
    int SchemaVersion = 1);
