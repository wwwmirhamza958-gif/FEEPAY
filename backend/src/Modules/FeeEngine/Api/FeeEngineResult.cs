using FeePlatform.Modules.FeeEngine.Models;

namespace FeePlatform.Modules.FeeEngine.Api;

public enum StatementLineKind
{
    Charge,
    Discount,
    Adjustment,
    LateFee,
}

public sealed record StatementLine(
    string FeeHeadCode,
    string Description,
    decimal Amount,
    string Trace,
    StatementLineKind Kind,
    string? RuleId = null,
    string? DiscountId = null);

public sealed record InstalmentPart(
    int Sequence,
    decimal Amount,
    decimal PercentShare,
    DateOnly? DueDate,
    string Trace);

public sealed record FeeEngineResult(
    IReadOnlyList<StatementLine> Lines,
    decimal GrossTotal,
    decimal DiscountTotal,
    decimal NetTotal,
    IReadOnlyList<InstalmentPart>? Instalments,
    TermDiffResult? TermDiff,
    bool IsPreview)
{
    /// <summary>Tax lines are never present in P1 (plan §7.4) — always empty.</summary>
    public IReadOnlyList<StatementLine> TaxLines { get; } = Array.Empty<StatementLine>();
}
