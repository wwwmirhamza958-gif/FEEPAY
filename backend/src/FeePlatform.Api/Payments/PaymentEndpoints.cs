using System.Text;
using FeePlatform.Api.Auth;
using FeePlatform.Infrastructure.Payments;
using FeePlatform.Modules.Payments;

namespace FeePlatform.Api.Payments;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var payments = endpoints.MapGroup("/api/v1/payments").WithTags("Payments");

        payments.MapGet("/channels", ListChannels)
            .WithName("ListPaymentChannels")
            .Produces<PaymentChannelsResponse>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        payments.MapPost("/attempts", CreateAttempt)
            .WithName("CreatePaymentAttempt")
            .Produces<PaymentAttemptDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        payments.MapGet("/attempts/{id:guid}", GetAttempt)
            .WithName("GetPaymentAttempt")
            .Produces<PaymentAttemptDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        payments.MapPost("/callbacks/{adapterCode}", HandleCallback)
            .WithName("PaymentCallback")
            .Produces<PaymentAttemptDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status401Unauthorized);

        payments.MapGet("/receipts/{id:guid}", GetReceipt)
            .WithName("GetPaymentReceipt")
            .Produces<PaymentReceiptDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        payments.MapGet("/receipts/{id:guid}/pdf", GetReceiptPdf)
            .WithName("GetPaymentReceiptPdf")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        payments.MapGet("/receipts/verify/{token}", VerifyReceipt)
            .WithName("VerifyPaymentReceipt")
            .Produces<ReceiptVerifyDto>(StatusCodes.Status200OK);

        payments.MapPost("/pay-links", CreatePayLink)
            .WithName("CreatePayLink")
            .Produces<PayLinkDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        payments.MapPost("/offline", RecordOffline)
            .WithName("RecordOfflinePayment")
            .Produces<PaymentAttemptDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        payments.MapPost("/reconciliation/runs", RunReconciliation)
            .WithName("RunPaymentReconciliation")
            .Produces<ReconciliationRunDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        // Dev-only FakeTier1 complete (gated inside service).
        payments.MapPost("/sandbox/fake-complete/{attemptId:guid}", FakeComplete)
            .WithName("SandboxFakeComplete");

        return endpoints;
    }

    private static IResult ListChannels(Guid? tenantId, PaymentService payments)
    {
        try
        {
            return Results.Ok(payments.ListChannels(tenantId));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> CreateAttempt(
        CreateAttemptRequest request,
        PaymentService payments,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await payments.CreateOrResumeAttemptAsync(request, ct));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> GetAttempt(Guid id, PaymentService payments, CancellationToken ct)
    {
        try
        {
            // Return URL is not proof — client must call this status endpoint.
            return Results.Ok(await payments.GetAttemptAsync(id, ct));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> HandleCallback(
        string adapterCode,
        HttpRequest httpRequest,
        PaymentService payments,
        CancellationToken ct)
    {
        try
        {
            using var reader = new StreamReader(httpRequest.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync(ct);
            var headers = httpRequest.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            return Results.Ok(await payments.HandleCallbackAsync(adapterCode, headers, body, ct));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> GetReceipt(
        Guid id,
        bool? pdf,
        PaymentService payments,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await payments.GetReceiptAsync(id, pdf == true, ct));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> GetReceiptPdf(Guid id, PaymentService payments, CancellationToken ct)
    {
        try
        {
            var bytes = await payments.GetReceiptPdfAsync(id, ct);
            return Results.File(bytes, "application/pdf", $"receipt-{id:N}.pdf");
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> VerifyReceipt(string token, PaymentService payments, CancellationToken ct) =>
        Results.Ok(await payments.VerifyReceiptAsync(token, ct));

    private static async Task<IResult> CreatePayLink(
        CreatePayLinkRequest request,
        PaymentService payments,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await payments.CreatePayLinkAsync(request, ct));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> RecordOffline(
        CreateOfflinePaymentRequest request,
        PaymentService payments,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await payments.RecordOfflinePaymentAsync(request, ct));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> RunReconciliation(
        ReconciliationRunRequest request,
        PaymentService payments,
        CancellationToken ct)
    {
        try
        {
            return Results.Ok(await payments.RunReconciliationStubAsync(request.AdapterCode, request.ReportDate, ct));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> FakeComplete(Guid attemptId, PaymentService payments, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await payments.CompleteSandboxFakeAsync(attemptId, ct));
        }
        catch (PaymentsDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static IResult DomainError(PaymentsDomainException ex)
    {
        var status = ex.Code switch
        {
            "RESOURCE_NOT_FOUND" => StatusCodes.Status404NotFound,
            "CALLBACK_SIGNATURE_INVALID" => StatusCodes.Status401Unauthorized,
            "PAYMENT_ATTEMPT_IN_PROGRESS" => StatusCodes.Status409Conflict,
            "TENANT_REQUIRED" => StatusCodes.Status400BadRequest,
            "PAYMENTS_SANDBOX_NOT_CONFIGURED" => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status400BadRequest,
        };

        return ApiErrors.Result(status, ex.Code, ex.Message, ex.MessageKey, retryable: ex.Retryable);
    }

    private sealed record ReconciliationRunRequest(string AdapterCode, DateOnly ReportDate);
}
