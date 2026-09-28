namespace FeePlatform.Modules.Payments;

[Flags]
public enum PaymentAdapterCapabilities
{
    None = 0,
    HostedCheckout = 1,
    ConsumerNumber = 2,
    RequestToPay = 4,
    Refund = 8,
    StatusQuery = 16,
    SettlementReport = 32,
}

public sealed record AdapterCheckoutRequest(
    Guid AttemptId,
    Guid TenantId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string ReturnUrl,
    string CallbackUrl,
    IReadOnlyDictionary<string, string>? Metadata = null);

public sealed record AdapterCheckoutResult(
    string RedirectUrl,
    string GatewayRef,
    string? ConsumerNumber = null);

public enum AdapterPaymentStatus
{
    Unknown = 0,
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Expired = 4,
    Cancelled = 5,
}

public sealed record AdapterStatusResult(
    AdapterPaymentStatus Status,
    string? GatewayTxnId,
    decimal? Amount,
    string? ReasonCode);

public sealed record AdapterCallbackRequest(
    string AdapterCode,
    IReadOnlyDictionary<string, string> Headers,
    string RawBody,
    IReadOnlyDictionary<string, string>? FormFields = null,
    IReadOnlyDictionary<string, string>? Query = null);

public sealed record AdapterCallbackResult(
    bool SignatureValid,
    AdapterPaymentStatus Status,
    string? GatewayTxnId,
    string? GatewayRef,
    Guid? AttemptId,
    decimal? Amount,
    string? ReasonCode);

public sealed record SettlementLine(
    string GatewayTxnId,
    decimal Amount,
    DateOnly SettlementDate,
    string? MerchantRef);

/// <summary>Plan §6.2 — every payment channel implements one interface.</summary>
public interface IPaymentAdapter
{
    string Code { get; }
    int Tier { get; }
    string DisplayName { get; }
    PaymentAdapterCapabilities Capabilities { get; }

    Task<AdapterCheckoutResult> CreateCheckoutAsync(AdapterCheckoutRequest request, CancellationToken ct = default);

    Task<AdapterStatusResult> GetStatusAsync(string gatewayRef, CancellationToken ct = default);

    Task<AdapterCallbackResult> VerifyAndParseCallbackAsync(AdapterCallbackRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<SettlementLine>> FetchSettlementReportAsync(DateOnly date, CancellationToken ct = default);

    Task RequestRefundAsync(string gatewayTxnId, decimal amount, CancellationToken ct = default);
}
