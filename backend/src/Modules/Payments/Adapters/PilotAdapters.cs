using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FeePlatform.Modules.Payments.Adapters;

public sealed class RaastP2mPilotAdapter(IOptions<PaymentsOptions> options, IHostEnvironment env)
    : HttpProviderAdapterBase(options, env)
{
    public override string Code => AdapterCodes.RaastP2mPilot;
    public override int Tier => 1;
    public override string DisplayName => AdapterCodes.DisplayNameFor(Code);
    public override PaymentAdapterCapabilities Capabilities =>
        PaymentAdapterCapabilities.HostedCheckout
        | PaymentAdapterCapabilities.RequestToPay
        | PaymentAdapterCapabilities.StatusQuery
        | PaymentAdapterCapabilities.SettlementReport;
}

public sealed class JazzCashPilotAdapter(IOptions<PaymentsOptions> options, IHostEnvironment env)
    : HttpProviderAdapterBase(options, env)
{
    public override string Code => AdapterCodes.JazzCashPilot;
    public override int Tier => 1;
    public override string DisplayName => AdapterCodes.DisplayNameFor(Code);
    public override PaymentAdapterCapabilities Capabilities =>
        PaymentAdapterCapabilities.HostedCheckout
        | PaymentAdapterCapabilities.StatusQuery
        | PaymentAdapterCapabilities.SettlementReport;
}

public sealed class EasypaisaPilotAdapter(IOptions<PaymentsOptions> options, IHostEnvironment env)
    : HttpProviderAdapterBase(options, env)
{
    public override string Code => AdapterCodes.EasypaisaPilot;
    public override int Tier => 1;
    public override string DisplayName => AdapterCodes.DisplayNameFor(Code);
    public override PaymentAdapterCapabilities Capabilities =>
        PaymentAdapterCapabilities.HostedCheckout
        | PaymentAdapterCapabilities.StatusQuery
        | PaymentAdapterCapabilities.SettlementReport;
}

/// <summary>Hosted redirect URL only — never PAN/CVV fields or payloads.</summary>
public sealed class CardPspPilotAdapter(IOptions<PaymentsOptions> options, IHostEnvironment env)
    : HttpProviderAdapterBase(options, env)
{
    public override string Code => AdapterCodes.CardPspPilot;
    public override int Tier => 2;
    public override string DisplayName => AdapterCodes.DisplayNameFor(Code);
    public override PaymentAdapterCapabilities Capabilities =>
        PaymentAdapterCapabilities.HostedCheckout
        | PaymentAdapterCapabilities.StatusQuery
        | PaymentAdapterCapabilities.SettlementReport;

    public override Task<AdapterCheckoutResult> CreateCheckoutAsync(
        AdapterCheckoutRequest request,
        CancellationToken ct = default)
    {
        // Card is Tier 2 — FakeTier1 does not apply. Hosted redirect only when credentials exist.
        if (!HasRealCredentials)
        {
            throw new PaymentsDomainException(
                "PAYMENTS_SANDBOX_NOT_CONFIGURED",
                "Card PSP sandbox credentials are not configured. Adapter returns a hosted redirect URL only (no card fields).",
                "errors.payments_sandbox_not_configured");
        }

        throw new PaymentsDomainException(
            "PAYMENTS_SANDBOX_NOT_CONFIGURED",
            "Card PSP merchant sandbox is not reachable. When configured, CreateCheckout returns a hosted redirect URL only — never PAN/CVV.",
            "errors.payments_sandbox_not_configured");
    }
}

public sealed class BillerCnPilotAdapter(IOptions<PaymentsOptions> options, IHostEnvironment env)
    : HttpProviderAdapterBase(options, env)
{
    public override string Code => AdapterCodes.BillerCnPilot;
    public override int Tier => 3;
    public override string DisplayName => AdapterCodes.DisplayNameFor(Code);
    public override PaymentAdapterCapabilities Capabilities =>
        PaymentAdapterCapabilities.ConsumerNumber
        | PaymentAdapterCapabilities.StatusQuery
        | PaymentAdapterCapabilities.SettlementReport;

    public override Task<AdapterCheckoutResult> CreateCheckoutAsync(
        AdapterCheckoutRequest request,
        CancellationToken ct = default)
    {
        if (!HasRealCredentials)
        {
            // Consumer number can be generated locally as a voucher stub for UX, but settlement
            // still needs the aggregator — refuse pretending paid.
            throw new PaymentsDomainException(
                "PAYMENTS_SANDBOX_NOT_CONFIGURED",
                "Biller/consumer-number sandbox credentials are not configured.",
                "errors.payments_sandbox_not_configured");
        }

        throw new PaymentsDomainException(
            "PAYMENTS_SANDBOX_NOT_CONFIGURED",
            "Biller aggregator sandbox is not reachable from this environment.",
            "errors.payments_sandbox_not_configured");
    }
}
