using FeePlatform.Modules.IdentityTenancy;

namespace FeePlatform.Modules.Payments;

public sealed class PaymentAttempt : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string PayableType { get; set; } = PayableTypes.Invoice;
    public Guid PayableId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid StudentId { get; set; }
    /// <summary>Snapshot of invoice version at attempt creation (CONTRACT §1.5).</summary>
    public int InvoiceVersion { get; set; }
    /// <summary>numeric(18,2) — CONTRACT §1.1.</summary>
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string Status { get; set; } = PaymentAttemptStatuses.Created;
    public string AdapterCode { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? GatewayRef { get; set; }
    public string? GatewayTxnId { get; set; }
    public string? FailureReasonCode { get; set; }
    public Guid? ReceiptId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
}

public sealed class PaymentReceipt : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PaymentAttemptId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid StudentId { get; set; }
    /// <summary>Gap-free per-tenant sequence (plan §6.8).</summary>
    public long ReceiptNumber { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PKR";
    public string Channel { get; set; } = string.Empty;
    public string? GatewayTxnId { get; set; }
    public string StudentInitials { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string LinesJson { get; set; } = "[]";
    public byte[]? PdfBytes { get; set; }
    public string VerifyToken { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Per-tenant gap-free receipt number allocator.</summary>
public sealed class ReceiptSequence : ITenantScoped
{
    public Guid TenantId { get; set; }
    public long NextNumber { get; set; } = 1;
}

public sealed class PayLink : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public sealed class TenantAdapterConfig : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string AdapterCode { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public int Tier { get; set; }
    /// <summary>Secret store reference — never store secret values (CONTRACT §5.2).</summary>
    public string SecretRef { get; set; } = string.Empty;
}

public sealed class ReconciliationRun : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string AdapterCode { get; set; } = string.Empty;
    public DateOnly ReportDate { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public List<ReconciliationException> Exceptions { get; set; } = [];
}

public sealed class ReconciliationException : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ReconciliationRunId { get; set; }
    /// <summary>One of ReconciliationExceptionTypes.</summary>
    public string ExceptionType { get; set; } = string.Empty;
    public string? GatewayTxnId { get; set; }
    public Guid? PaymentAttemptId { get; set; }
    public decimal? GatewayAmount { get; set; }
    public decimal? PlatformAmount { get; set; }
    public string Detail { get; set; } = string.Empty;
    public string? ResolutionNote { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
