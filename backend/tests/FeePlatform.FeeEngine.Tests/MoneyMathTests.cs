using FeePlatform.Modules.FeeEngine.Money;

namespace FeePlatform.FeeEngine.Tests;

public class MoneyMathTests
{
    [Theory]
    [InlineData(1.005, "1.01")]
    [InlineData(1.004, "1.00")]
    [InlineData(-1.005, "-1.01")]
    [InlineData(142500.0, "142500.00")]
    public void RoundHalfUp_MatchesContract(decimal input, string expectedWire)
    {
        Assert.Equal(expectedWire, MoneyMath.ToWire(MoneyMath.RoundHalfUp(input)));
    }

    [Fact]
    public void FormatPkr_UsesThinSpaceAndTrueMinus()
    {
        Assert.Equal($"Rs{MoneyMath.ThinSpace}142,500", MoneyMath.FormatPkr(142500m));
        Assert.Equal($"{MoneyMath.TrueMinus}Rs{MoneyMath.ThinSpace}35,625", MoneyMath.FormatPkr(-35625m));
        Assert.Equal($"Rs{MoneyMath.ThinSpace}0", MoneyMath.FormatPkr(0m));
    }
}
