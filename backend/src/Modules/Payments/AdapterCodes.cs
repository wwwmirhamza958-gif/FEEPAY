namespace FeePlatform.Modules.Payments;

/// <summary>CONTRACT §5.2 adapter codes.</summary>
public static class AdapterCodes
{
    public const string RaastP2mPilot = "raast-p2m-pilot";
    public const string JazzCashPilot = "jazzcash-pilot";
    public const string EasypaisaPilot = "easypaisa-pilot";
    public const string CardPspPilot = "card-psp-pilot";
    public const string BillerCnPilot = "biller-cn-pilot";

    public static readonly IReadOnlyList<string> All =
    [
        RaastP2mPilot,
        JazzCashPilot,
        EasypaisaPilot,
        CardPspPilot,
        BillerCnPilot,
    ];

    public static int TierFor(string code) => code switch
    {
        RaastP2mPilot or JazzCashPilot or EasypaisaPilot => 1,
        CardPspPilot => 2,
        BillerCnPilot => 3,
        _ => 99,
    };

    public static string DisplayNameFor(string code) => code switch
    {
        RaastP2mPilot => "Raast (Pilot Bank)",
        JazzCashPilot => "JazzCash",
        EasypaisaPilot => "Easypaisa",
        CardPspPilot => "Card (PSP Hosted)",
        BillerCnPilot => "Consumer Number Biller",
        _ => code,
    };

    public static bool IsTier1(string code) => TierFor(code) == 1;
}
