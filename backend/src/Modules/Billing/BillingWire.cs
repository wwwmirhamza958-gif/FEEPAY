using FeePlatform.Modules.FeeEngine.Money;

namespace FeePlatform.Modules.Billing;

/// <summary>Wire helpers on top of FeeEngine MoneyMath (CONTRACT §1.4).</summary>
public static class BillingWire
{
    public static string ToWire(decimal amount) => MoneyMath.ToWire(amount);

    public static decimal RoundHalfUp(decimal amount) => MoneyMath.RoundHalfUp(amount);

    public static bool TryParseWire(string value, out decimal amount)
    {
        try
        {
            amount = MoneyMath.FromWire(value);
            return true;
        }
        catch
        {
            amount = 0;
            return false;
        }
    }
}
