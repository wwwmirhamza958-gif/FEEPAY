namespace FeePlatform.Modules.Payments;

public sealed class PaymentsDomainException(string code, string message, string messageKey, bool retryable = false)
    : Exception(message)
{
    public string Code { get; } = code;
    public string MessageKey { get; } = messageKey;
    public bool Retryable { get; } = retryable;
}

public sealed record CreateAttemptRequest(
    string PayableType,
    Guid PayableId,
    string AdapterCode,
    string IdempotencyKey,
    Guid? PreferResumeAttemptId = null);

public sealed record CreateOfflinePaymentRequest(
    Guid InvoiceId,
    string Amount,
    string Reference,
    Guid RecordedByUserId,
    string? ChannelNote = null);

public sealed record CreatePayLinkRequest(
    Guid InvoiceId,
    int ExpiresInHours = 72,
    Guid? CreatedByUserId = null);

public sealed record PaymentChannelDto(
    string AdapterCode,
    string DisplayName,
    int Tier,
    bool Enabled,
    bool IsDefault);

public sealed record PaymentChannelsResponse(
    int HighestEnabledTier,
    PaymentChannelDto? DefaultChannel,
    IReadOnlyList<PaymentChannelDto> OtherChannels,
    IReadOnlyList<PaymentChannelDto> AllEnabled);

public sealed record PaymentAttemptDto(
    Guid AttemptId,
    string Status,
    string PayableType,
    Guid PayableId,
    Guid InvoiceId,
    Guid StudentId,
    string Amount,
    string Currency,
    string AdapterCode,
    string IdempotencyKey,
    string? CheckoutUrl,
    string? GatewayRef,
    string? GatewayTxnId,
    Guid? ReceiptId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PaymentReceiptDto(
    Guid ReceiptId,
    long ReceiptNumber,
    Guid AttemptId,
    Guid InvoiceId,
    string Amount,
    string Currency,
    string Channel,
    string? GatewayTxnId,
    string StudentInitials,
    string InstitutionName,
    string VerifyUrl,
    DateTimeOffset CreatedAtUtc,
    bool HasPdf);

public sealed record PayLinkDto(
    Guid PayLinkId,
    Guid InvoiceId,
    string Token,
    string Url,
    DateTimeOffset ExpiresAtUtc);

public sealed record ReceiptVerifyDto(
    bool Valid,
    string Amount,
    DateOnly PaymentDate,
    string StudentInitials,
    string InstitutionName,
    long? ReceiptNumber);

public sealed record ReconciliationRunDto(
    Guid RunId,
    string AdapterCode,
    DateOnly ReportDate,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    IReadOnlyList<ReconciliationExceptionDto> Exceptions);

public sealed record ReconciliationExceptionDto(
    Guid ExceptionId,
    string ExceptionType,
    string? GatewayTxnId,
    Guid? PaymentAttemptId,
    string? GatewayAmount,
    string? PlatformAmount,
    string Detail);

/// <summary>
/// Narrow interface owned by Payments. Billing (Agent C) should implement ApplyPaidAmount
/// against invoice/instalment tables — see docs/supervisor/REQUESTS_TO_C_FROM_D.md.
/// </summary>
public interface IInvoicePaymentApplier
{
    Task<PaymentApplicationResult> ApplyPaidAmountAsync(
        PaymentApplicationRequest request,
        CancellationToken ct = default);
}

public sealed record PaymentApplicationRequest(
    Guid TenantId,
    Guid InvoiceId,
    Guid? InstalmentId,
    Guid StudentId,
    decimal Amount,
    Guid SourceAttemptId,
    bool AllowOverpaymentCredit);

public sealed record PaymentApplicationResult(
    decimal AppliedToInvoice,
    decimal OverpaymentCredited,
    Guid? CreditId,
    string InvoiceStatus,
    decimal RemainingBalance);

public sealed record PayableSnapshot(
    Guid PayableId,
    string PayableType,
    Guid InvoiceId,
    Guid StudentId,
    int InvoiceVersion,
    decimal AmountDue,
    string InvoiceStatus,
    IReadOnlyList<PayableLineSnapshot> Lines,
    string? StudentDisplayName);

public sealed record PayableLineSnapshot(string FeeHeadCode, decimal Amount, string Trace);

/// <summary>Look up payable amount/lines without Payments writing invoice rows.</summary>
public interface IPayableLookup
{
    Task<PayableSnapshot?> GetPayableAsync(string payableType, Guid payableId, CancellationToken ct = default);
}
