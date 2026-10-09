using FeePlatform.Api.Auth;
using FeePlatform.Infrastructure.Parent;
using FeePlatform.Infrastructure.Payments;
using FeePlatform.Modules.Payments;

namespace FeePlatform.Api.Parent;

public static class ParentEndpoints
{
    public static IEndpointRouteBuilder MapParentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/parent").WithTags("Parent");

        group.MapGet("/dashboard", GetDashboard)
            .WithName("ParentDashboard")
            .Produces<ParentDashboardDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        group.MapGet("/students/{studentId:guid}/fee", GetStudentFee)
            .WithName("ParentStudentFee")
            .Produces<ParentDashboardDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status403Forbidden);

        group.MapGet("/students/{studentId:guid}/pay-context", GetPayContext)
            .WithName("ParentPayContext")
            .Produces<ParentPayContextDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status403Forbidden)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        group.MapPost("/payments", CreatePayment)
            .WithName("ParentCreatePayment")
            .Produces<PaymentAttemptDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status403Forbidden)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        group.MapGet("/payments/attempts/{attemptId:guid}", GetAttempt)
            .WithName("ParentGetPaymentAttempt")
            .Produces<PaymentAttemptDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status403Forbidden)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        group.MapGet("/students/{studentId:guid}/payments/history", GetPaymentHistory)
            .WithName("ParentPaymentHistory")
            .Produces<IReadOnlyList<ParentPaymentHistoryItemDto>>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status403Forbidden);

        return endpoints;
    }

    private static async Task<IResult> GetDashboard(
        HttpRequest http,
        ParentAccessService access,
        ParentDashboardService dashboard,
        CancellationToken ct)
    {
        var session = await RequireSessionAsync(http, access, ct);
        if (session is IResult error) return error;

        var dto = await dashboard.GetDashboardAsync((ParentSession)session, ct);
        if (dto is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status404NotFound,
                "NO_LINKED_STUDENTS",
                "This number isn't registered with any institution yet. Check with your university's finance office, or ask your child to send you a link code.",
                "errors.no_linked_students",
                retryable: false);
        }

        return Results.Ok(dto);
    }

    private static async Task<IResult> GetStudentFee(
        Guid studentId,
        HttpRequest http,
        ParentAccessService access,
        ParentDashboardService dashboard,
        CancellationToken ct)
    {
        var session = await RequireSessionAsync(http, access, ct);
        if (session is IResult error) return error;

        var dto = await dashboard.GetStudentFeeAsync((ParentSession)session, studentId, ct);
        if (dto is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                "You are not authorised to view this student.",
                "errors.forbidden",
                retryable: false);
        }

        return Results.Ok(dto);
    }

    private static async Task<IResult> GetPayContext(
        Guid studentId,
        HttpRequest http,
        ParentAccessService access,
        ParentDashboardService dashboard,
        CancellationToken ct)
    {
        var session = await RequireSessionAsync(http, access, ct);
        if (session is IResult error) return error;

        var link = await access.RequireStudentAccessAsync(((ParentSession)session).MobileE164, studentId, ct);
        if (link is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                "You are not authorised to pay for this student.",
                "errors.forbidden",
                retryable: false);
        }

        var dto = await dashboard.GetPayContextAsync((ParentSession)session, studentId, ct);
        if (dto is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status400BadRequest,
                "NOTHING_DUE",
                "This student has no outstanding fee.",
                "errors.nothing_due",
                retryable: false);
        }

        return Results.Ok(dto);
    }

    private static async Task<IResult> CreatePayment(
        ParentCreatePaymentRequest request,
        HttpRequest http,
        ParentAccessService access,
        ParentDashboardService dashboard,
        CancellationToken ct)
    {
        var session = await RequireSessionAsync(http, access, ct);
        if (session is IResult error) return error;

        try
        {
            var attempt = await dashboard.CreatePaymentAsync((ParentSession)session, request, ct);
            return Results.Ok(attempt);
        }
        catch (ParentAccessException ex)
        {
            return ApiErrors.Result(ex.StatusCode, ex.Code, ex.Message, ex.MessageKey, retryable: ex.Retryable);
        }
        catch (PaymentsDomainException ex)
        {
            var status = ex.Code switch
            {
                "RESOURCE_NOT_FOUND" => StatusCodes.Status404NotFound,
                "NOTHING_DUE" => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status400BadRequest,
            };
            return ApiErrors.Result(status, ex.Code, ex.Message, ex.MessageKey, retryable: ex.Retryable);
        }
    }

    private static async Task<IResult> GetAttempt(
        Guid attemptId,
        HttpRequest http,
        ParentAccessService access,
        PaymentService payments,
        CancellationToken ct)
    {
        var session = await RequireSessionAsync(http, access, ct);
        if (session is IResult error) return error;

        try
        {
            var attempt = await payments.GetAttemptAsync(attemptId, ct);
            var link = await access.RequireStudentAccessAsync(
                ((ParentSession)session).MobileE164,
                attempt.StudentId,
                ct);
            if (link is null)
            {
                return ApiErrors.Result(
                    StatusCodes.Status403Forbidden,
                    "FORBIDDEN",
                    "You are not authorised to view this payment.",
                    "errors.forbidden",
                    retryable: false);
            }

            return Results.Ok(attempt);
        }
        catch (PaymentsDomainException ex)
        {
            return ApiErrors.Result(
                StatusCodes.Status404NotFound,
                ex.Code,
                ex.Message,
                ex.MessageKey,
                retryable: ex.Retryable);
        }
    }

    private static async Task<IResult> GetPaymentHistory(
        Guid studentId,
        HttpRequest http,
        ParentAccessService access,
        ParentDashboardService dashboard,
        CancellationToken ct)
    {
        var session = await RequireSessionAsync(http, access, ct);
        if (session is IResult error) return error;

        var items = await dashboard.ListPaymentHistoryAsync((ParentSession)session, studentId, ct);
        if (items is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status403Forbidden,
                "FORBIDDEN",
                "You are not authorised to view this student.",
                "errors.forbidden",
                retryable: false);
        }

        return Results.Ok(items);
    }

    private static async Task<object> RequireSessionAsync(
        HttpRequest http,
        ParentAccessService access,
        CancellationToken ct)
    {
        Guid? sessionId = null;
        if (http.Headers.TryGetValue(ParentAccessService.SessionHeaderName, out var header)
            && Guid.TryParse(header.FirstOrDefault(), out var fromHeader))
        {
            sessionId = fromHeader;
        }
        else if (http.Headers.TryGetValue("Authorization", out var auth)
                 && auth.FirstOrDefault() is { } bearer
                 && bearer.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                 && Guid.TryParse(bearer["Bearer ".Length..].Trim(), out var fromBearer))
        {
            // Dev stub: access token is the session id until real JWT lands.
            sessionId = fromBearer;
        }

        if (sessionId is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status401Unauthorized,
                "UNAUTHENTICATED",
                "Sign in again to continue.",
                "errors.unauthenticated",
                retryable: false);
        }

        var session = await access.ResolveSessionAsync(sessionId.Value, ct);
        if (session is null)
        {
            return ApiErrors.Result(
                StatusCodes.Status401Unauthorized,
                "UNAUTHENTICATED",
                "Sign in again to continue.",
                "errors.unauthenticated",
                retryable: false);
        }

        return session;
    }
}
