using FeePlatform.Api.Auth;
using FeePlatform.Infrastructure.Integrations;
using FeePlatform.Modules.Integrations;

namespace FeePlatform.Api.Integrations;

/// <summary>Import wizard routes — REQUESTS_TO_C.md R-C-E-007…010.</summary>
public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/imports").WithTags("Imports");

        group.MapGet("/templates/{entity}", GetTemplate)
            .WithName("GetImportTemplate")
            .Produces<ImportTemplateDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        // R-C-E-007
        group.MapPost("/", UploadJob)
            .DisableAntiforgery()
            .WithName("UploadImportJob")
            .Produces<ImportJobDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        // R-C-E-008
        group.MapPut("/{jobId:guid}/mapping", SetMapping)
            .WithName("SetImportMapping")
            .Produces<ImportJobDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        // R-C-E-009
        group.MapPost("/{jobId:guid}/validate", DryRun)
            .WithName("ImportValidate")
            .Produces<ImportDryRunResultDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest);

        // R-C-E-010
        group.MapPost("/{jobId:guid}/commit", Commit)
            .WithName("ImportCommit")
            .Produces<ImportCommitResultDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status409Conflict);

        group.MapGet("/{jobId:guid}", GetJob)
            .WithName("GetImportJob")
            .Produces<ImportJobDto>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        group.MapGet("/{jobId:guid}/errors", GetErrors)
            .WithName("GetImportErrors")
            .Produces<IReadOnlyList<ImportRowErrorDto>>(StatusCodes.Status200OK)
            .Produces<ApiErrorEnvelope>(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static IResult GetTemplate(string entity, ImportWizardService imports)
    {
        try
        {
            return Results.Ok(imports.GetTemplate(entity));
        }
        catch (ImportDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> UploadJob(HttpRequest request, ImportWizardService imports, CancellationToken ct)
    {
        try
        {
            if (!request.HasFormContentType)
            {
                return ApiErrors.Result(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_FAILED",
                    "Expected multipart form with entity + file.",
                    "errors.validation_failed");
            }

            var form = await request.ReadFormAsync(ct);
            var entity = form["entity"].ToString();
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(entity) || file is null)
            {
                return ApiErrors.Result(
                    StatusCodes.Status400BadRequest,
                    "VALIDATION_FAILED",
                    "entity and file are required.",
                    "errors.validation_failed",
                    [
                        new ApiErrorDetail("entity", "REQUIRED", "entity is required.", "errors.field.required"),
                        new ApiErrorDetail("file", "REQUIRED", "file is required.", "errors.field.required"),
                    ]);
            }

            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(ct);

            var job = await imports.UploadAsync(entity, file.FileName, file.ContentType ?? "text/csv", content, ct);
            return Results.Ok(job);
        }
        catch (ImportDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> SetMapping(Guid jobId, SetColumnMappingRequest request, ImportWizardService imports, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await imports.SetMappingAsync(jobId, request.Mapping, ct));
        }
        catch (ImportDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> DryRun(Guid jobId, ImportWizardService imports, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await imports.DryRunAsync(jobId, ct));
        }
        catch (ImportDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> Commit(Guid jobId, ImportCommitRequest? request, ImportWizardService imports, CancellationToken ct)
    {
        try
        {
            _ = request?.AuditReason;
            return Results.Ok(await imports.CommitAsync(jobId, ct));
        }
        catch (ImportDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> GetJob(Guid jobId, ImportWizardService imports, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await imports.GetAsync(jobId, ct));
        }
        catch (ImportDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static async Task<IResult> GetErrors(Guid jobId, ImportWizardService imports, CancellationToken ct)
    {
        try
        {
            return Results.Ok(await imports.GetErrorsAsync(jobId, ct));
        }
        catch (ImportDomainException ex)
        {
            return DomainError(ex);
        }
    }

    private static IResult DomainError(ImportDomainException ex)
    {
        var status = ex.Code switch
        {
            "IMPORT_JOB_NOT_FOUND" => StatusCodes.Status404NotFound,
            "IMPORT_NOT_VALIDATED" or "IMPORT_INVALID_STATE" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest,
        };
        return ApiErrors.Result(status, ex.Code, ex.Message, ex.MessageKey);
    }
}

public sealed record ImportCommitRequest(string? AuditReason);
