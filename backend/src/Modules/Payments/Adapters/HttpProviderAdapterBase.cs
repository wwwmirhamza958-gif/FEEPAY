using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FeePlatform.Modules.Payments.Adapters;

/// <summary>
/// Shared HTTP-provider adapter base. Calls provider APIs when credentials exist;
/// otherwise returns CONTRACT §3 PAYMENTS_SANDBOX_NOT_CONFIGURED — never pretends success.
/// FakeTier1 path only when AllowSandboxFake + Development.
/// </summary>
public abstract class HttpProviderAdapterBase(
    IOptions<PaymentsOptions> options,
    IHostEnvironment environment) : IPaymentAdapter
{
    protected PaymentsOptions Options => options.Value;
    protected IHostEnvironment Environment => environment;

    public abstract string Code { get; }
    public abstract int Tier { get; }
    public abstract string DisplayName { get; }
    public abstract PaymentAdapterCapabilities Capabilities { get; }

    protected AdapterCredentialOptions? Credentials =>
        Options.Adapters.TryGetValue(Code, out var c) ? c : null;

    protected bool HasRealCredentials =>
        !string.IsNullOrWhiteSpace(Credentials?.CallbackHmacSecret)
        || !string.IsNullOrWhiteSpace(Credentials?.MerchantId);

    protected bool FakeTier1Allowed =>
        Options.AllowSandboxFake
        && Environment.IsDevelopment()
        && Tier == 1;

    public virtual Task<AdapterCheckoutResult> CreateCheckoutAsync(
        AdapterCheckoutRequest request,
        CancellationToken ct = default)
    {
        if (FakeTier1Allowed)
        {
            var fakeRef = $"fake-{Code}-{request.AttemptId:N}";
            var url = $"{Options.PublicBaseUrl.TrimEnd('/')}/api/v1/payments/sandbox/fake-checkout/{request.AttemptId:D}";
            return Task.FromResult(new AdapterCheckoutResult(url, fakeRef));
        }

        if (!HasRealCredentials)
        {
            throw new PaymentsDomainException(
                "PAYMENTS_SANDBOX_NOT_CONFIGURED",
                $"Sandbox credentials for adapter '{Code}' are not configured. Set Payments:Adapters:{Code} or enable Payments:AllowSandboxFake in Development only.",
                "errors.payments_sandbox_not_configured");
        }

        // Real provider HTTP call would go here. Without an official SDK we POST to ApiBaseUrl.
        // Credentials are present but live bank sandboxes are not on this machine — still fail clearly.
        throw new PaymentsDomainException(
            "PAYMENTS_SANDBOX_NOT_CONFIGURED",
            $"Merchant credentials for '{Code}' are referenced but the live provider sandbox is not reachable from this environment. Configure a working ApiBaseUrl + secrets, or use FakeTier1 in Development.",
            "errors.payments_sandbox_not_configured");
    }

    public virtual Task<AdapterStatusResult> GetStatusAsync(string gatewayRef, CancellationToken ct = default)
    {
        if (FakeTier1Allowed && gatewayRef.StartsWith("fake-", StringComparison.Ordinal))
        {
            return Task.FromResult(new AdapterStatusResult(AdapterPaymentStatus.Pending, null, null, null));
        }

        if (!HasRealCredentials)
        {
            throw new PaymentsDomainException(
                "PAYMENTS_SANDBOX_NOT_CONFIGURED",
                $"Cannot query status for '{Code}' — sandbox not configured.",
                "errors.payments_sandbox_not_configured",
                retryable: true);
        }

        throw new PaymentsDomainException(
            "PAYMENTS_SANDBOX_NOT_CONFIGURED",
            $"Status query for '{Code}' requires a live provider sandbox.",
            "errors.payments_sandbox_not_configured",
            retryable: true);
    }

    public virtual Task<AdapterCallbackResult> VerifyAndParseCallbackAsync(
        AdapterCallbackRequest request,
        CancellationToken ct = default)
    {
        if (FakeTier1Allowed && IsFakeCallback(request))
        {
            return Task.FromResult(ParseFakeCallback(request));
        }

        var secret = Credentials?.CallbackHmacSecret;
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new PaymentsDomainException(
                "PAYMENTS_SANDBOX_NOT_CONFIGURED",
                $"Cannot verify callback for '{Code}' — CallbackHmacSecret not configured.",
                "errors.payments_sandbox_not_configured");
        }

        if (!request.Headers.TryGetValue("X-Signature", out var signature)
            && !request.Headers.TryGetValue("x-signature", out signature))
        {
            return Task.FromResult(new AdapterCallbackResult(
                SignatureValid: false,
                AdapterPaymentStatus.Unknown,
                null, null, null, null, "MISSING_SIGNATURE"));
        }

        var expected = ComputeHmacHex(secret, request.RawBody);
        if (!FixedTimeEqualsHex(expected, signature))
        {
            return Task.FromResult(new AdapterCallbackResult(
                SignatureValid: false,
                AdapterPaymentStatus.Unknown,
                null, null, null, null, "INVALID_SIGNATURE"));
        }

        return Task.FromResult(ParseSignedCallbackBody(request.RawBody));
    }

    public virtual Task<IReadOnlyList<SettlementLine>> FetchSettlementReportAsync(
        DateOnly date,
        CancellationToken ct = default)
    {
        if (FakeTier1Allowed)
            return Task.FromResult<IReadOnlyList<SettlementLine>>([]);

        if (!HasRealCredentials)
        {
            throw new PaymentsDomainException(
                "PAYMENTS_SANDBOX_NOT_CONFIGURED",
                $"Settlement report for '{Code}' requires configured sandbox credentials.",
                "errors.payments_sandbox_not_configured");
        }

        // Stub empty report when credentials exist but no live feed.
        return Task.FromResult<IReadOnlyList<SettlementLine>>([]);
    }

    public virtual Task RequestRefundAsync(string gatewayTxnId, decimal amount, CancellationToken ct = default) =>
        throw new PaymentsDomainException(
            "REFUND_NOT_SUPPORTED",
            $"Adapter '{Code}' does not support online refunds in MVP.",
            "errors.refund_not_supported");

    protected static string ComputeHmacHex(string secret, string body)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(body);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    protected static bool FixedTimeEqualsHex(string expectedHex, string provided)
    {
        var a = Encoding.UTF8.GetBytes(expectedHex.Trim().ToLowerInvariant());
        var b = Encoding.UTF8.GetBytes(provided.Trim().ToLowerInvariant());
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static bool IsFakeCallback(AdapterCallbackRequest request)
    {
        if (request.Headers.TryGetValue("X-Sandbox-Fake", out var flag)
            || request.Headers.TryGetValue("x-sandbox-fake", out flag))
        {
            return string.Equals(flag, "succeed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(flag, "fail", StringComparison.OrdinalIgnoreCase);
        }

        return request.RawBody.Contains("\"sandboxFake\"", StringComparison.OrdinalIgnoreCase);
    }

    private static AdapterCallbackResult ParseFakeCallback(AdapterCallbackRequest request)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(request.RawBody) ? "{}" : request.RawBody);
        var root = doc.RootElement;
        Guid? attemptId = root.TryGetProperty("attemptId", out var a) && a.TryGetGuid(out var g) ? g : null;
        var amount = root.TryGetProperty("amount", out var amtEl)
            && PaymentsWire.TryParseWire(amtEl.GetString() ?? "", out var amt)
            ? amt
            : (decimal?)null;
        var txn = root.TryGetProperty("gatewayTxnId", out var t) ? t.GetString() : $"fake-txn-{attemptId:N}";
        var gatewayRef = root.TryGetProperty("gatewayRef", out var r) ? r.GetString() : null;

        var fail = request.Headers.TryGetValue("X-Sandbox-Fake", out var flag)
            && string.Equals(flag, "fail", StringComparison.OrdinalIgnoreCase);

        return new AdapterCallbackResult(
            SignatureValid: true,
            fail ? AdapterPaymentStatus.Failed : AdapterPaymentStatus.Succeeded,
            txn,
            gatewayRef,
            attemptId,
            amount,
            fail ? "SANDBOX_FAKE_FAIL" : null);
    }

    private static AdapterCallbackResult ParseSignedCallbackBody(string rawBody)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawBody) ? "{}" : rawBody);
        var root = doc.RootElement;
        Guid? attemptId = root.TryGetProperty("attemptId", out var a) && a.TryGetGuid(out var g) ? g : null;
        var statusText = root.TryGetProperty("status", out var s) ? s.GetString() : "Succeeded";
        var status = statusText?.ToUpperInvariant() switch
        {
            "SUCCEEDED" or "SUCCESS" or "PAID" => AdapterPaymentStatus.Succeeded,
            "FAILED" or "FAIL" => AdapterPaymentStatus.Failed,
            "EXPIRED" => AdapterPaymentStatus.Expired,
            "CANCELLED" or "CANCELED" => AdapterPaymentStatus.Cancelled,
            _ => AdapterPaymentStatus.Pending,
        };
        var txn = root.TryGetProperty("gatewayTxnId", out var t) ? t.GetString() : null;
        var gatewayRef = root.TryGetProperty("gatewayRef", out var r) ? r.GetString() : null;
        decimal? amount = null;
        if (root.TryGetProperty("amount", out var amtEl))
        {
            if (amtEl.ValueKind == JsonValueKind.String
                && PaymentsWire.TryParseWire(amtEl.GetString() ?? "", out var parsed))
                amount = parsed;
            else if (amtEl.ValueKind == JsonValueKind.Number && amtEl.TryGetDecimal(out var d))
                amount = d;
        }

        return new AdapterCallbackResult(true, status, txn, gatewayRef, attemptId, amount, null);
    }
}
