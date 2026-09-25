using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Modules.IdentityTenancy;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Api.Auth;

/// <summary>P0 auth stubs — OTP for parents; portal email+password+TOTP hook points.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthStubs(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/otp/challenge", async (
            OtpChallengeRequest request,
            AppDbContext db,
            IValidator<OtpChallengeRequest> validator,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(request, ct);
            if (!validation.IsValid)
            {
                var details = validation.Errors.Select(e => new ApiErrorDetail(
                    e.PropertyName,
                    "INVALID_FORMAT",
                    e.ErrorMessage,
                    "errors.field.invalid")).ToList();
                return ApiErrors.Result(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_FAILED",
                    "One or more fields are invalid.",
                    "errors.validation_failed",
                    details,
                    retryable: false);
            }

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var challenge = new OtpChallenge
            {
                Id = Guid.NewGuid(),
                MobileE164 = NormalizeMobile(request.Mobile),
                CodeHash = Hash(code),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };

            db.OtpChallenges.Add(challenge);
            await db.SaveChangesAsync(ct);

            var isDev = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

            return Results.Ok(new OtpChallengeResponse(
                challenge.Id,
                challenge.ExpiresAtUtc,
                isDev ? code : null));
        });

        group.MapPost("/otp/verify", async (
            OtpVerifyRequest request,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var challenge = await db.OtpChallenges.FirstOrDefaultAsync(x => x.Id == request.ChallengeId, ct);
            if (challenge is null || challenge.ConsumedAtUtc is not null || challenge.ExpiresAtUtc < DateTimeOffset.UtcNow)
            {
                return ApiErrors.Result(
                    StatusCodes.Status401Unauthorized,
                    "OTP_INVALID",
                    "Code expired or not found.",
                    "errors.otp_invalid",
                    retryable: false);
            }

            challenge.AttemptCount += 1;
            if (!FixedTimeEquals(challenge.CodeHash, Hash(request.Code)))
            {
                await db.SaveChangesAsync(ct);
                return ApiErrors.Result(
                    StatusCodes.Status401Unauthorized,
                    "OTP_MISMATCH",
                    "Incorrect code. Try again.",
                    "errors.otp_mismatch",
                    retryable: true);
            }

            challenge.ConsumedAtUtc = DateTimeOffset.UtcNow;

            var payer = await db.PayerProfiles.FirstOrDefaultAsync(p => p.MobileE164 == challenge.MobileE164, ct);
            if (payer is null)
            {
                payer = new PayerProfile
                {
                    Id = Guid.NewGuid(),
                    MobileE164 = challenge.MobileE164,
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                };
                db.PayerProfiles.Add(payer);
            }

            var session = new Session
            {
                Id = Guid.NewGuid(),
                PayerProfileId = payer.Id,
                RefreshTokenHash = Hash(Guid.NewGuid().ToString("N")),
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
                CreatedAtUtc = DateTimeOffset.UtcNow,
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync(ct);

            // Tokens must not go to localStorage — httpOnly cookie / secure store later.
            return Results.Ok(new AuthSessionResponse(session.Id, "access-token-stub", "refresh-token-stub"));
        });

        group.MapPost("/portal/login", (PortalLoginRequest request) =>
        {
            _ = request;
            return ApiErrors.Result(
                StatusCodes.Status501NotImplemented,
                "PORTAL_LOGIN_STUB",
                "Portal login + TOTP 2FA hook is registered; full auth lands next.",
                "errors.portal_login_stub",
                retryable: false);
        });

        group.MapPost("/portal/totp/verify", () =>
            ApiErrors.Result(
                StatusCodes.Status501NotImplemented,
                "TOTP_STUB",
                "TOTP verify hook point — Finance Admin 2FA mandatory.",
                "errors.totp_stub",
                retryable: false));

        return endpoints;
    }

    private static string NormalizeMobile(string mobile)
    {
        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("92", StringComparison.Ordinal) && digits.Length == 12) return "+" + digits;
        if (digits.StartsWith('0') && digits.Length == 11) return "+92" + digits[1..];
        if (digits.Length == 10 && digits.StartsWith('3')) return "+92" + digits;
        return mobile.Trim();
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}

public sealed record OtpChallengeRequest(string Mobile);
public sealed record OtpChallengeResponse(Guid ChallengeId, DateTimeOffset ExpiresAtUtc, string? DevCode);
public sealed record OtpVerifyRequest(Guid ChallengeId, string Code);
public sealed record PortalLoginRequest(string Email, string Password, string? TotpCode);
public sealed record AuthSessionResponse(Guid SessionId, string AccessToken, string RefreshToken);

/// <summary>CONTRACT §3 error envelope.</summary>
public sealed record ApiErrorEnvelope(ApiErrorBody Error);

public sealed record ApiErrorBody(
    string Code,
    string Message,
    string MessageKey,
    Guid CorrelationId,
    IReadOnlyList<ApiErrorDetail> Details,
    bool Retryable);

public sealed record ApiErrorDetail(string? Field, string Code, string Message, string MessageKey);

public static class ApiErrors
{
    public static IResult Result(
        int statusCode,
        string code,
        string message,
        string messageKey,
        IReadOnlyList<ApiErrorDetail>? details = null,
        bool retryable = false)
    {
        var correlationId = Guid.TryParse(Activity.Current?.Id, out var fromActivity)
            ? fromActivity
            : Guid.NewGuid();

        // Prefer TraceId-shaped correlation when Activity has a valid W3C id fragment.
        if (Activity.Current?.TraceId is { } traceId && traceId != default)
        {
            var hex = traceId.ToHexString();
            if (Guid.TryParseExact(hex.PadRight(32, '0')[..32], "N", out var g))
                correlationId = g;
        }

        var body = new ApiErrorEnvelope(
            new ApiErrorBody(
                code,
                message,
                messageKey,
                correlationId,
                details ?? Array.Empty<ApiErrorDetail>(),
                retryable));

        return Results.Json(body, statusCode: statusCode);
    }
}

public sealed class OtpChallengeRequestValidator : AbstractValidator<OtpChallengeRequest>
{
    public OtpChallengeRequestValidator()
    {
        RuleFor(x => x.Mobile).NotEmpty().Must(m =>
        {
            var d = new string(m.Where(char.IsDigit).ToArray());
            return d.Length is >= 10 and <= 12;
        }).WithMessage("Enter a Pakistani mobile number starting with 3.");
    }
}
