namespace FeePlatform.Modules.FeeEngine.Models;

/// <summary>Prior-term snapshot for term-over-term diff (plan §7.6).</summary>
public sealed record StatementSnapshot(
    string TermCode,
    IReadOnlyList<SnapshotHeadAmount> Heads);

public sealed record SnapshotHeadAmount(string FeeHeadCode, decimal Amount);

public sealed record HeadDiff(
    string FeeHeadCode,
    decimal PreviousAmount,
    decimal CurrentAmount,
    decimal AbsoluteChange,
    decimal? PercentChange);

public sealed record TermDiffResult(
    IReadOnlyList<HeadDiff> PerHead,
    decimal PreviousTotal,
    decimal CurrentTotal,
    decimal AbsoluteChange,
    decimal? TotalPercentChange);
