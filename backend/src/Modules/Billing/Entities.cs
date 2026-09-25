using FeePlatform.Modules.IdentityTenancy;

namespace FeePlatform.Modules.Billing;

public sealed class Invoice : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public Guid TermId { get; set; }
    public int Version { get; set; } = 1;
    public string Status { get; set; } = InvoiceStatuses.Draft;
    /// <summary>numeric(18,2) — CONTRACT §1.1.</summary>
    public decimal TotalAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public string Currency { get; set; } = "PKR";
    public DateOnly DueDate { get; set; }
    public Guid? PreviousInvoiceId { get; set; }
    /// <summary>When this invoice adjusts/supersedes another.</summary>
    public Guid? AdjustmentOfInvoiceId { get; set; }
    public string? DiffJson { get; set; }
    public string? Reason { get; set; }
    public Guid? InstalmentPlanId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? IssuedAtUtc { get; set; }

    public List<InvoiceLine> Lines { get; set; } = [];
    public List<Instalment> Instalments { get; set; } = [];
}

public sealed class InvoiceLine : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public string FeeHeadCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>Human-readable calculation trace (plan §7.6).</summary>
    public string Trace { get; set; } = string.Empty;
    /// <summary>Charge | Discount | Adjustment | LateFee (FeeEngine StatementLineKind).</summary>
    public string Kind { get; set; } = "Charge";
    public string? RuleRef { get; set; }
    public int SortOrder { get; set; }
}

public sealed class InstalmentPlan : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ProgramId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<InstalmentPlanPart> Parts { get; set; } = [];
}

public sealed class InstalmentPlanPart : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid InstalmentPlanId { get; set; }
    public int Sequence { get; set; }
    /// <summary>Share of total, e.g. 0.50 for 50%.</summary>
    public decimal Share { get; set; }
    /// <summary>Days after issue (or absolute offset policy).</summary>
    public int DueOffsetDays { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class Instalment : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public int Sequence { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class Deferment : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    /// <summary>e.g. Instalment | Invoice.</summary>
    public string ItemType { get; set; } = "Instalment";
    public Guid ItemId { get; set; }
    public string Status { get; set; } = DefermentStatuses.Requested;
    public string Reason { get; set; } = string.Empty;
    public DateOnly ProposedDueDate { get; set; }
    public DateOnly? ApprovedDueDate { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? DecisionNote { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? DecidedAtUtc { get; set; }
}

public sealed class StudentCredit : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StudentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string? SourceNote { get; set; }
    public Guid? SourceAttemptId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Preview run for a term — calculate without issuing (plan §7.6).</summary>
public sealed class PreviewRun : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TermId { get; set; }
    public int StudentCount { get; set; }
    public decimal TotalAmount { get; set; }
    public int OutlierCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public List<PreviewStudentResult> Results { get; set; } = [];
}

public sealed class PreviewStudentResult : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PreviewRunId { get; set; }
    public Guid StudentId { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsOutlier { get; set; }
    public string? OutlierReason { get; set; }
    public string LinesJson { get; set; } = "[]";
}
