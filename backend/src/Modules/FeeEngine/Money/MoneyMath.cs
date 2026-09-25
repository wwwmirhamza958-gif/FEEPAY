namespace FeePlatform.Modules.FeeEngine.Money;

/// <summary>
/// PKR money helpers aligned with CONTRACT.md §1 (same semantics as packages/money).
/// </summary>
public static class MoneyMath
{
    public const char TrueMinus = '\u2212';
    public const char ThinSpace = '\u2009';

    /// <summary>Half-up to 2 decimal places (per line / per instalment part).</summary>
    public static decimal RoundHalfUp(decimal amount)
    {
        // MidpointRounding.AwayFromZero is half-up for positive; for negatives it rounds
        // away from zero (e.g. -1.005 → -1.01), matching CONTRACT §1.2 bankers-avoiding half-up.
        return Math.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>Wire format: exactly two fractional digits.</summary>
    public static string ToWire(decimal amount)
    {
        var rounded = RoundHalfUp(amount);
        return rounded.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Parse CONTRACT wire string ("142500.00") to decimal.</summary>
    public static decimal FromWire(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Money wire string is required.", nameof(value));
        if (!decimal.TryParse(
                value,
                System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed))
            throw new FormatException($"Invalid money decimal string: {value}");
        return RoundHalfUp(parsed);
    }

    /// <summary>UI / trace format: <c>Rs 142,500</c> / <c>−Rs 35,625</c> (whole rupees).</summary>
    public static string FormatPkr(decimal amount)
    {
        var rounded = RoundHalfUp(amount);
        var negative = rounded < 0;
        var whole = (long)Math.Truncate(Math.Abs(rounded));
        var grouped = whole.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
        var body = $"Rs{ThinSpace}{grouped}";
        return negative ? $"{TrueMinus}{body}" : body;
    }
}
