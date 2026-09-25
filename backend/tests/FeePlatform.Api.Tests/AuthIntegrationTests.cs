using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FeePlatform.Modules.IdentityTenancy.Seed;

namespace FeePlatform.Api.Tests;

public class AuthIntegrationTests : IClassFixture<FeeApiFactory>
{
    private readonly FeeApiFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public AuthIntegrationTests(FeeApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Parent_otp_happy_path_returns_session()
    {
        var client = _factory.CreateClient();

        var challengeRes = await client.PostAsJsonAsync("/api/v1/auth/otp/challenge", new
        {
            mobile = PilotSeed.GuardianMobileFake,
        });
        Assert.Equal(HttpStatusCode.OK, challengeRes.StatusCode);

        var challenge = await challengeRes.Content.ReadFromJsonAsync<OtpChallengeDto>(JsonOptions);
        Assert.NotNull(challenge);
        Assert.False(string.IsNullOrWhiteSpace(challenge.DevCode));

        var verifyRes = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = challenge.DevCode,
        });
        Assert.Equal(HttpStatusCode.OK, verifyRes.StatusCode);

        var session = await verifyRes.Content.ReadFromJsonAsync<SessionDto>(JsonOptions);
        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session.SessionId);
        Assert.False(string.IsNullOrWhiteSpace(session.AccessToken));
    }

    [Fact]
    public async Task Parent_otp_wrong_code_returns_section3_envelope()
    {
        var client = _factory.CreateClient();

        var challengeRes = await client.PostAsJsonAsync("/api/v1/auth/otp/challenge", new
        {
            mobile = "+923001111002",
        });
        challengeRes.EnsureSuccessStatusCode();
        var challenge = await challengeRes.Content.ReadFromJsonAsync<OtpChallengeDto>(JsonOptions);
        Assert.NotNull(challenge);

        var verifyRes = await client.PostAsJsonAsync("/api/v1/auth/otp/verify", new
        {
            challengeId = challenge.ChallengeId,
            code = "000000",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, verifyRes.StatusCode);

        using var doc = await JsonDocument.ParseAsync(await verifyRes.Content.ReadAsStreamAsync());
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal("OTP_MISMATCH", error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("messageKey").GetString()));
        Assert.True(error.TryGetProperty("correlationId", out _));
        Assert.True(error.TryGetProperty("details", out var details));
        Assert.Equal(JsonValueKind.Array, details.ValueKind);
        Assert.True(error.GetProperty("retryable").GetBoolean());
    }

    [Fact]
    public async Task Portal_login_with_password_returns_session()
    {
        var email = $"officer-{Guid.NewGuid():N}@pilot.test";
        const string password = "Password1!";
        await _factory.SeedPortalUserAsync(PilotSeed.TenantId, email, password);

        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/portal/login", new
        {
            email,
            password,
            totpCode = (string?)null,
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var session = await res.Content.ReadFromJsonAsync<SessionDto>(JsonOptions);
        Assert.NotNull(session);
        Assert.NotEqual(Guid.Empty, session.SessionId);
    }

    [Fact]
    public async Task Portal_login_wrong_password_returns_section3_envelope()
    {
        var email = $"officer-{Guid.NewGuid():N}@pilot.test";
        await _factory.SeedPortalUserAsync(PilotSeed.TenantId, email, "Password1!");

        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/auth/portal/login", new
        {
            email,
            password = "wrong-password",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);

        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        var error = doc.RootElement.GetProperty("error");
        Assert.Equal("INVALID_CREDENTIALS", error.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(error.GetProperty("message").GetString()));
    }

    private sealed record OtpChallengeDto(Guid ChallengeId, DateTimeOffset ExpiresAtUtc, string? DevCode);
    private sealed record SessionDto(Guid SessionId, string AccessToken, string RefreshToken);
}
