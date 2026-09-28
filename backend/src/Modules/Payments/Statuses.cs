namespace FeePlatform.Modules.Payments;

/// <summary>CONTRACT §2.1 — do not invent synonyms.</summary>
public static class PaymentAttemptStatuses
{
    public const string Created = "Created";
    public const string Pending = "Pending";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
    public const string Cancelled = "Cancelled";
    public const string Reversed = "Reversed";

    public static bool IsActive(string status) =>
        status is Created or Pending;

    public static bool IsTerminal(string status) =>
        status is Succeeded or Failed or Expired or Cancelled or Reversed;
}

public static class PayableTypes
{
    public const string Invoice = "Invoice";
    public const string Instalment = "Instalment";
}

/// <summary>Plan §6.7 reconciliation exception types.</summary>
public static class ReconciliationExceptionTypes
{
    public const string PaidAtGatewayNotInPlatform = "PaidAtGatewayNotInPlatform";
    public const string InPlatformNotSettled = "InPlatformNotSettled";
    public const string AmountMismatch = "AmountMismatch";
}
