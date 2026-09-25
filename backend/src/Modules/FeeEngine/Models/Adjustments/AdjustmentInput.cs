namespace FeePlatform.Modules.FeeEngine.Models.Adjustments;

/// <summary>
/// Explicit add/drop (or correction) line for a new draft statement.
/// Issued invoices stay immutable; Billing links these as adjustments (Agent C).
/// </summary>
public sealed record AdjustmentInput(
    string FeeHeadCode,
    string Description,
    decimal Amount,
    string? Trace = null);
