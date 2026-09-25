using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using FeePlatform.Infrastructure.Auth;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Modules.IdentityTenancy;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Api.Auth;

/// <summary>
/// Parent OTP + portal password login (dev/test). TOTP remains stubbed unless bypass is enabled.
/// Works without Redis (in-memory rate limit). Requires a working DbContext (InMemory OK for tests).
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthStubs(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/otp/challenge", ChallengeOtp)
            .WithName("OtpChallenge")
            .Produces<OtpChallengeResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status429TooManyRequests);

        group.MapPost("/otp/verify", VerifyOtp)
            .WithName("OtpVerify")
            .Produces<AuthSessionResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized);

        group.MapPost("/portal/login", PortalLogin)
            .WithName("PortalLogin")
            .Produces<AuthSessionResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status501NotImplemented);

        group.MapPost("/portal/totp/verify", () =>
            ApiErrors.Result(
                StatusCodes.Status501NotImplemented,
                "TOTP_STUB",
                "TOTP verify hook point — Finance Admin 2FA mandatory.",
                "errors.totp_stub",
                retryable: false))
            .WithName("PortalTotpVerify")
            .Produces<ApiErrorEnvelope>(StatusCodes.Status501NotImplemented);

        return endpoints;
    }

    private static async Task<IResult> ChallengeOtp(
        OtpChallengeRequest request,
        AppDbContext db,
        IValidator<OtpChallengeRequest> validator,
        IOtpRateLimiter rateLimiter,
        IHostEnvironment environment,
        CancellationToken ct)
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

        var mobile = NormalizeMobile(request.Mobile);
        if (!rateLimiter.TryAcquire($"otp:challenge:{mobile}", out var retryAfter))
        {
            var seconds = (int)Math.Ceiling((retryAfter ?? TimeSpan.FromMinutes(10)).TotalSeconds);
            return ApiErrors.Result(
                StatusCodes.Status429TooManyRequests,
                "OTP_RATE_LIMITED",
                "Too many tries. Try again in 10 minutes.",
                "errors.otp_rate_limited",
                [
                    new ApiErrorDetail(
                        null,
                        "RETRY_AFTER",
                        $"Retry after {seconds} seconds.",
                        "errors.retry_after"),
                ],
                retryable: true);
        }

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var challenge = new OtpChallenge
        {
            Id = Guid.NewGuid(),
            MobileE164 = mobile,
            CodeHash = Hash(code),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        // Return plaintext code only in Development/Testing (never Production).
        var exposeDevCode = environment.IsDevelopment()
            || string.Equals(environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);

        return Results.Ok(new OtpChallengeResponse(
            challenge.Id,
            challenge.ExpiresAtUtc,
            exposeDevCode ? code : null));
    }

    private static async Task<IResult> VerifyOtp(
        OtpVerifyRequest request,
        AppDbContext db,
        IOtpRateLimiter rateLimiter,
        CancellationToken ct)
    {
        if (!rateLimiter.TryAcquire($"otp:verify:{request.ChallengeId:D}", out var retryAfter))
        {
            var seconds = (int)Math.Ceiling((retryAfter ?? TimeSpan.FromMinutes(10)).TotalSeconds);
            return ApiErrors.Result(
                StatusCodes.Status429TooManyRequests,
                "OTP_RATE_LIMITED",
                "Too many tries. Try again in 10 minutes.",
                "errors.otp_rate_limited",
                [
                    new ApiErrorDetail(
                        null,
                        "RETRY_AFTER",
                        $"Retry after {seconds} seconds.",
                        "errors.retry_after"),
                ],
                retryable: true);
        }

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
    }

    private static async Task<IResult> PortalLogin(
        PortalLoginRequest request,
        AppDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ApiErrors.Result(
                StatusCodes.Status400BadRequest,
                "VALIDATION_FAILED",
                "One or more fields are invalid.",
                "errors.validation_failed",
                [
                    new ApiErrorDetail(
                        string.IsNullOrWhiteSpace(request.Email) ? "email" : "password",
                        "REQUIRED",
                        "Email and password are required.",
                        "errors.field.required"),
                ],
                retryable: false);
        }

        var email = request.Email.Trim().ToLowerInvariant();
        // Login lookup must ignore tenant filter (email is unique per tenant; find candidate then scope).
        var user = await db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive, ct);

        if (user is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status401Unauthorized,
                "INVALID_CREDENTIALS",
                "Email or password is incorrect.",
                "errors.invalid_credentials",
                retryable: false);
        }

        var hasher = new PasswordHasher<User>();
        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
        {
            return ApiErrors.Result(
                StatusCodes.Status401Unauthorized,
                "INVALID_CREDENTIALS",
                "Email or password is incorrect.",
                "errors.invalid_credentials",
                retryable: false);
        }

        var allowTotpBypass = environment.IsDevelopment()
            && configuration.GetValue("Auth:AllowTotpBypass", defaultValue: false);

        if (user.TotpEnabled && !allowTotpBypass)
        {
            if (string.IsNullOrWhiteSpace(request.TotpCode))
            {
                return ApiErrors.Result(
                    StatusCodes.Status401Unauthorized,
                    "TOTP_REQUIRED",
                    "Enter your authenticator code to continue.",
                    "errors.totp_required",
                    retryable: false);
            }

            // Full TOTP verification lands with staging secrets; P0 returns a clear §3 error.
            return ApiErrors.Result(
                StatusCodes.Status501NotImplemented,
                "TOTP_STUB",
                "TOTP verify hook point — Finance Admin 2FA mandatory.",
                "errors.totp_stub",
                retryable: false);
        }

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TenantId = user.TenantId,
            RefreshTokenHash = Hash(Guid.NewGuid().ToString("N")),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(ct);

        return Results.Ok(new AuthSessionResponse(session.Id, "access-token-stub", "refresh-token-stub"));
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
        var correlationId = Guid.NewGuid();

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
