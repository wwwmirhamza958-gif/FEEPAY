using Xunit;

namespace FeePlatform.Api.Tests;

public class SmokeTests
{
    [Fact]
    public async Task Health_returns_ok_without_postgres_or_redis()
    {
        await using var factory = new FeeApiFactory();
        var client = factory.CreateClient();
        var res = await client.GetAsync("/health");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("ok", body, StringComparison.OrdinalIgnoreCase);
    }
}
