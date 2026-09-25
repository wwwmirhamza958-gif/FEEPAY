namespace FeePlatform.Modules.Billing;

public sealed class BillingDomainException(string code, string message, string messageKey) : Exception(message)
{
    public string Code { get; } = code;
    public string MessageKey { get; } = messageKey;
}

public sealed record FeeLineDto(string FeeHeadCode, string Amount, string Trace);
public sealed record PreviewStudentResultDto(
    Guid StudentId,
    string StudentExternalId,
    string TotalAmount,
    bool IsOutlier,
    string? OutlierReason,
    IReadOnlyList<FeeLineDto> Lines);
public sealed record PreviewRunResultDto(
    Guid PreviewRunId,
    Guid TermId,
    int StudentCount,
    string TotalAmount,
    int OutlierCount,
    IReadOnlyList<PreviewStudentResultDto> Results);
public sealed record InvoiceSummaryDto(
    Guid InvoiceId,
    Guid StudentId,
    Guid TermId,
    string Status,
    string TotalAmount,
    DateOnly DueDate,
    bool IsOverdue);
public sealed record InvoiceDetailDto(
    Guid InvoiceId,
    Guid StudentId,
    Guid TermId,
    int Version,
    string Status,
    string TotalAmount,
    string RemainingBalance,
    string Currency,
    DateOnly DueDate,
    bool IsOverdue,
    Guid? PreviousInvoiceId,
    Guid? AdjustmentOfInvoiceId,
    string? Reason,
    IReadOnlyList<FeeLineDto> Lines,
    IReadOnlyList<InstalmentDto> Instalments);
public sealed record InstalmentDto(
    Guid InstalmentId,
    int Sequence,
    string Label,
    string Amount,
    DateOnly DueDate,
    string PaidAmount);
public sealed record IssueBatchResultDto(Guid TermId, int IssuedCount, IReadOnlyList<InvoiceSummaryDto> Invoices);
public sealed record AdjustmentLineInput(string FeeHeadCode, string Amount, string? Trace);
public sealed record CreateInstalmentPlanRequest(
    string Code,
    string Name,
    Guid? ProgramId,
    IReadOnlyList<InstalmentPlanPartInput> Parts);
public sealed record InstalmentPlanPartInput(decimal Share, int DueOffsetDays, string? Label, int? Sequence);
public sealed record InstalmentPlanDto(
    Guid Id,
    string Code,
    string Name,
    Guid? ProgramId,
    IReadOnlyList<InstalmentPlanPartDto> Parts);
public sealed record InstalmentPlanPartDto(int Sequence, decimal Share, int DueOffsetDays, string Label);
public sealed record RequestDefermentRequest(
    Guid StudentId,
    string ItemType,
    Guid ItemId,
    string Reason,
    DateOnly ProposedDueDate);
public sealed record DefermentDto(
    Guid Id,
    Guid StudentId,
    string ItemType,
    Guid ItemId,
    string Status,
    string Reason,
    DateOnly ProposedDueDate,
    DateOnly? ApprovedDueDate);
public sealed record IssueStatementsRequest(
    Guid? TermId,
    Guid? PreviewRunId,
    string Reason,
    DateOnly? DueDate,
    Guid? InstalmentPlanId);
public sealed record PreviewRequest(Guid TermId);
public sealed record AdjustInvoiceRequest(string Reason, IReadOnlyList<AdjustmentLineInput>? Lines);
public sealed record DecideDefermentRequest(bool Approve, Guid DecidedByUserId, string? Note, DateOnly? ApprovedDueDate);
