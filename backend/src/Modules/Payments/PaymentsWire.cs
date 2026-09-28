using FeePlatform.Modules.FeeEngine.Money;

namespace FeePlatform.Modules.Payments;

/// <summary>Wire helpers — CONTRACT §1.4.</summary>
public static class PaymentsWire
{
    public static string ToWire(decimal amount) => MoneyMath.ToWire(amount);

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
