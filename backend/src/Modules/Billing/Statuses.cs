namespace FeePlatform.Modules.Billing;

/// <summary>CONTRACT §2.2 — do not invent synonyms.</summary>
public static class InvoiceStatuses
{
    public const string Draft = "Draft";
    public const string Issued = "Issued";
    public const string PartiallyPaid = "PartiallyPaid";
    public const string Paid = "Paid";
    public const string Superseded = "Superseded";

    public static bool IsPayable(string status) =>
        status is Issued or PartiallyPaid;
}

/// <summary>CONTRACT §2.5.</summary>
public static class DefermentStatuses
{
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Withdrawn = "Withdrawn";
}
