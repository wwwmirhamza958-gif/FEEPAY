using System.Globalization;
using System.Text;
using System.Text.Json;
using FeePlatform.Infrastructure.Outbox;
using FeePlatform.Infrastructure.Persistence;
using FeePlatform.Infrastructure.Tenancy;
using FeePlatform.Modules.Academic;
using FeePlatform.Modules.Integrations;
using Microsoft.EntityFrameworkCore;

namespace FeePlatform.Infrastructure.Integrations;

public sealed class ImportWizardService(AppDbContext db, ITenantContext tenantContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static readonly Dictionary<string, ImportTemplateDto> Templates = new(StringComparer.OrdinalIgnoreCase)
    {
        [ImportEntities.Students] = new(
            ImportEntities.Students,
            ["external_id", "display_name", "program_code"],
            ["intake_code"],
            "external_id,display_name,program_code,intake_code\nSTU-PILOT-0099,Student Ninety Nine,BSCS,BSCS-2024\n"),
        [ImportEntities.Enrolments] = new(
            ImportEntities.Enrolments,
            ["student_external_id", "course_code", "term_code"],
            ["is_repeat"],
            "student_external_id,course_code,term_code,is_repeat\nSTU-PILOT-0001,CS101,FALL-2026,false\n"),
        [ImportEntities.Guardians] = new(
            ImportEntities.Guardians,
            ["external_id", "display_name", "mobile_e164", "student_external_id"],
            ["relation"],
            "external_id,display_name,mobile_e164,student_external_id,relation\nGRD-PILOT-0099,Guardian Ninety Nine,+923001111099,STU-PILOT-0001,Parent\n"),
        [ImportEntities.Programs] = new(
            ImportEntities.Programs,
            ["code", "name"],
            ["external_id"],
            "code,name,external_id\nBSXX,BS Example,ERP-PROG-BSXX\n"),
    };

    public ImportTemplateDto GetTemplate(string entity)
    {
        if (!Templates.TryGetValue(entity, out var template))
            throw new ImportDomainException("UNKNOWN_IMPORT_ENTITY", $"Unknown import entity '{entity}'.", "errors.unknown_import_entity");
        return template;
    }

    public async Task<ImportJobDto> UploadAsync(
        string entity,
        string fileName,
        string contentType,
        string rawContent,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        _ = GetTemplate(entity);

        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Entity = entity.ToLowerInvariant(),
            Status = ImportJobStatuses.Uploaded,
            OriginalFileName = fileName,
            ContentType = contentType,
            RawContent = rawContent,
            ColumnMappingJson = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        db.ImportJobs.Add(job);
        await db.SaveChangesAsync(ct);
        return ToDto(job);
    }

    public async Task<ImportJobDto> SetMappingAsync(Guid jobId, IReadOnlyDictionary<string, string> mapping, CancellationToken ct = default)
    {
        RequireTenant();
        var job = await GetJobAsync(jobId, ct);
        if (job.Status is not (ImportJobStatuses.Uploaded or ImportJobStatuses.Validated or ImportJobStatuses.FailedValidation))
            throw new ImportDomainException("IMPORT_INVALID_STATE", "Column mapping can only be set before commit.", "errors.import_invalid_state");

        job.ColumnMappingJson = JsonSerializer.Serialize(mapping, JsonOptions);
        await db.SaveChangesAsync(ct);
        return ToDto(job);
    }

    public async Task<ImportDryRunResultDto> DryRunAsync(Guid jobId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var job = await GetJobAsync(jobId, ct);
        job.Status = ImportJobStatuses.Validating;
        await db.SaveChangesAsync(ct);

        try
        {
            var template = GetTemplate(job.Entity);
            var mapping = ResolveMapping(job, template);
            var (rows, headerError) = ParseCsv(job.RawContent);
            var errors = new List<ImportRowError>();

            if (headerError is not null)
            {
                headerError.Id = Guid.NewGuid();
                headerError.TenantId = tenantId;
                headerError.ImportJobId = job.Id;
                errors.Add(headerError);
                return await FailValidationAsync(job, errors, ct);
            }

            var wouldCreate = 0;
            var wouldUpdate = 0;
            var wouldSkip = 0;

            for (var i = 0; i < rows.Count; i++)
            {
                var rowNumber = i + 2; // 1-based data rows after header
                var row = rows[i];
                var rowErrors = ValidateRow(job.Entity, row, mapping, template, rowNumber);
                if (rowErrors.Count > 0)
                {
                    foreach (var err in rowErrors)
                    {
                        err.Id = Guid.NewGuid();
                        err.TenantId = tenantId;
                        err.ImportJobId = job.Id;
                        errors.Add(err);
                    }
                    continue;
                }

                var action = await ClassifyRowAsync(job.Entity, row, mapping, ct);
                switch (action)
                {
                    case RowAction.Create: wouldCreate++; break;
                    case RowAction.Update: wouldUpdate++; break;
                    default: wouldSkip++; break;
                }
            }

            // Replace prior errors
            var existing = await db.ImportRowErrors.Where(e => e.ImportJobId == job.Id).ToListAsync(ct);
            db.ImportRowErrors.RemoveRange(existing);
            db.ImportRowErrors.AddRange(errors);

            job.CreatedCount = wouldCreate;
            job.UpdatedCount = wouldUpdate;
            job.SkippedCount = wouldSkip;
            job.ErrorCount = errors.Count;
            job.ValidatedAtUtc = DateTimeOffset.UtcNow;
            job.Status = ImportJobStatuses.Validated;
            await db.SaveChangesAsync(ct);

            return new ImportDryRunResultDto(
                job.Id,
                job.Status,
                wouldCreate,
                wouldUpdate,
                wouldSkip,
                errors.Count,
                errors.Select(e => new ImportRowErrorDto(e.RowNumber, e.Field, e.Code, e.Message)).ToList());
        }
        catch (Exception ex) when (ex is not ImportDomainException)
        {
            job.Status = ImportJobStatuses.FailedValidation;
            job.ErrorCount = 1;
            await db.SaveChangesAsync(ct);
            throw new ImportDomainException("FAILED_VALIDATION", "Dry-run aborted: unreadable or invalid file.", "errors.failed_validation");
        }
    }

    public async Task<ImportCommitResultDto> CommitAsync(Guid jobId, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var job = await GetJobAsync(jobId, ct);
        if (job.Status != ImportJobStatuses.Validated)
            throw new ImportDomainException("IMPORT_NOT_VALIDATED", "Commit requires a Validated dry-run.", "errors.import_not_validated");

        job.Status = ImportJobStatuses.Committing;
        await db.SaveChangesAsync(ct);

        try
        {
            var template = GetTemplate(job.Entity);
            var mapping = ResolveMapping(job, template);
            var (rows, _) = ParseCsv(job.RawContent);
            var created = 0;
            var updated = 0;
            var skipped = 0;
            var errorCount = 0;

            foreach (var row in rows)
            {
                var rowErrors = ValidateRow(job.Entity, row, mapping, template, 0);
                if (rowErrors.Count > 0)
                {
                    errorCount++;
                    continue;
                }

                var action = await ApplyRowAsync(job.Entity, row, mapping, tenantId, ct);
                switch (action)
                {
                    case RowAction.Create: created++; break;
                    case RowAction.Update: updated++; break;
                    default: skipped++; break;
                }
            }

            job.CreatedCount = created;
            job.UpdatedCount = updated;
            job.SkippedCount = skipped;
            job.ErrorCount = errorCount;
            job.Status = ImportJobStatuses.Succeeded;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;

            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventName = DomainEvents.ImportJobCompleted,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    tenantId,
                    jobId = job.Id,
                    entity = job.Entity,
                    status = job.Status,
                    createdCount = created,
                    updatedCount = updated,
                    skippedCount = skipped,
                    errorCount,
                    occurredAt = job.CompletedAtUtc,
                }, JsonOptions),
                OccurredAtUtc = job.CompletedAtUtc.Value,
            });

            await db.SaveChangesAsync(ct);
            return new ImportCommitResultDto(job.Id, job.Status, created, updated, skipped, errorCount);
        }
        catch (Exception)
        {
            job.Status = ImportJobStatuses.Failed;
            job.CompletedAtUtc = DateTimeOffset.UtcNow;
            db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventName = DomainEvents.ImportJobCompleted,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    tenantId,
                    jobId = job.Id,
                    entity = job.Entity,
                    status = job.Status,
                    createdCount = job.CreatedCount,
                    updatedCount = job.UpdatedCount,
                    skippedCount = job.SkippedCount,
                    errorCount = job.ErrorCount,
                    occurredAt = job.CompletedAtUtc,
                }, JsonOptions),
                OccurredAtUtc = job.CompletedAtUtc.Value,
            });
            await db.SaveChangesAsync();
            throw;
        }
    }

    public async Task<ImportJobDto> GetAsync(Guid jobId, CancellationToken ct = default)
    {
        RequireTenant();
        return ToDto(await GetJobAsync(jobId, ct));
    }

    public async Task<IReadOnlyList<ImportRowErrorDto>> GetErrorsAsync(Guid jobId, CancellationToken ct = default)
    {
        RequireTenant();
        _ = await GetJobAsync(jobId, ct);
        var errors = await db.ImportRowErrors.AsNoTracking()
            .Where(e => e.ImportJobId == jobId)
            .OrderBy(e => e.RowNumber)
            .ToListAsync(ct);
        return errors.Select(e => new ImportRowErrorDto(e.RowNumber, e.Field, e.Code, e.Message)).ToList();
    }

    private async Task<ImportDryRunResultDto> FailValidationAsync(ImportJob job, List<ImportRowError> errors, CancellationToken ct)
    {
        var existing = await db.ImportRowErrors.Where(e => e.ImportJobId == job.Id).ToListAsync(ct);
        db.ImportRowErrors.RemoveRange(existing);
        db.ImportRowErrors.AddRange(errors);
        job.Status = ImportJobStatuses.FailedValidation;
        job.ErrorCount = errors.Count;
        job.ValidatedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return new ImportDryRunResultDto(
            job.Id,
            job.Status,
            0, 0, 0,
            errors.Count,
            errors.Select(e => new ImportRowErrorDto(e.RowNumber, e.Field, e.Code, e.Message)).ToList());
    }

    private async Task<ImportJob> GetJobAsync(Guid jobId, CancellationToken ct) =>
        await db.ImportJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct)
        ?? throw new ImportDomainException("IMPORT_JOB_NOT_FOUND", "Import job was not found.", "errors.import_job_not_found");

    private Guid RequireTenant()
    {
        if (tenantContext.TenantId is not Guid tid)
            throw new ImportDomainException("TENANT_REQUIRED", "Tenant context is required.", "errors.tenant_required");
        return tid;
    }

    private static IReadOnlyDictionary<string, string> ResolveMapping(ImportJob job, ImportTemplateDto template)
    {
        if (!string.IsNullOrWhiteSpace(job.ColumnMappingJson))
        {
            var mapped = JsonSerializer.Deserialize<Dictionary<string, string>>(job.ColumnMappingJson, JsonOptions);
            if (mapped is { Count: > 0 }) return mapped;
        }

        // Auto-map: canonical column → same header name
        return template.RequiredColumns.Concat(template.OptionalColumns)
            .ToDictionary(c => c, c => c, StringComparer.OrdinalIgnoreCase);
    }

    private static (List<Dictionary<string, string>> Rows, ImportRowError? HeaderError) ParseCsv(string content)
    {
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
            return ([], new ImportRowError { RowNumber = 1, Field = "file", Code = "EMPTY", Message = "File is empty." });

        var headers = SplitCsvLine(lines[0]);
        if (headers.Count == 0)
            return ([], new ImportRowError { RowNumber = 1, Field = "header", Code = "EMPTY_HEADER", Message = "Header row is empty." });

        var rows = new List<Dictionary<string, string>>();
        for (var i = 1; i < lines.Length; i++)
        {
            var cells = SplitCsvLine(lines[i]);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < headers.Count; c++)
                dict[headers[c]] = c < cells.Count ? cells[c] : string.Empty;
            rows.Add(dict);
        }

        return (rows, null);
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(ch);
        }

        result.Add(sb.ToString().Trim());
        return result;
    }

    private static List<ImportRowError> ValidateRow(
        string entity,
        Dictionary<string, string> row,
        IReadOnlyDictionary<string, string> mapping,
        ImportTemplateDto template,
        int rowNumber)
    {
        var errors = new List<ImportRowError>();
        foreach (var required in template.RequiredColumns)
        {
            if (!mapping.TryGetValue(required, out var header) || !row.TryGetValue(header, out var value) || string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new ImportRowError
                {
                    RowNumber = rowNumber,
                    Field = required,
                    Code = "REQUIRED",
                    Message = $"Missing required field '{required}'.",
                });
            }
        }

        if (entity == ImportEntities.Guardians
            && mapping.TryGetValue("mobile_e164", out var mobileCol)
            && row.TryGetValue(mobileCol, out var mobile)
            && !string.IsNullOrWhiteSpace(mobile)
            && !mobile.StartsWith("+92", StringComparison.Ordinal))
        {
            errors.Add(new ImportRowError
            {
                RowNumber = rowNumber,
                Field = "mobile_e164",
                Code = "INVALID_FORMAT",
                Message = "Mobile must be E.164 starting with +92.",
            });
        }

        return errors;
    }

    private async Task<RowAction> ClassifyRowAsync(
        string entity,
        Dictionary<string, string> row,
        IReadOnlyDictionary<string, string> mapping,
        CancellationToken ct)
    {
        string Map(string key) => mapping.TryGetValue(key, out var h) && row.TryGetValue(h, out var v) ? v.Trim() : string.Empty;

        switch (entity)
        {
            case ImportEntities.Students:
            {
                var externalId = Map("external_id");
                return await db.Students.AnyAsync(s => s.ExternalId == externalId, ct)
                    ? RowAction.Update
                    : RowAction.Create;
            }
            case ImportEntities.Programs:
            {
                var code = Map("code");
                return await db.Programs.AnyAsync(p => p.Code == code, ct)
                    ? RowAction.Update
                    : RowAction.Create;
            }
            case ImportEntities.Guardians:
            {
                var externalId = Map("external_id");
                return await db.Guardians.AnyAsync(g => g.ExternalId == externalId, ct)
                    ? RowAction.Update
                    : RowAction.Create;
            }
            case ImportEntities.Enrolments:
                return await ExistsEnrolmentAsync(Map("student_external_id"), Map("course_code"), Map("term_code"), ct)
                    ? RowAction.Skip
                    : RowAction.Create;
            default:
                return RowAction.Skip;
        }
    }

    private async Task<RowAction> ApplyRowAsync(
        string entity,
        Dictionary<string, string> row,
        IReadOnlyDictionary<string, string> mapping,
        Guid tenantId,
        CancellationToken ct)
    {
        string Map(string key) => mapping.TryGetValue(key, out var h) && row.TryGetValue(h, out var v) ? v.Trim() : string.Empty;
        var now = DateTimeOffset.UtcNow;

        switch (entity)
        {
            case ImportEntities.Students:
            {
                var externalId = Map("external_id");
                var programCode = Map("program_code");
                var program = await db.Programs.FirstOrDefaultAsync(p => p.Code == programCode, ct);
                if (program is null) return RowAction.Skip;

                Guid? intakeId = null;
                var intakeCode = Map("intake_code");
                if (!string.IsNullOrWhiteSpace(intakeCode))
                {
                    var intake = await db.Intakes.FirstOrDefaultAsync(i => i.Code == intakeCode, ct);
                    intakeId = intake?.Id;
                }

                var existing = await db.Students.FirstOrDefaultAsync(s => s.ExternalId == externalId, ct);
                if (existing is null)
                {
                    db.Students.Add(new Student
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ExternalId = externalId,
                        DisplayName = Map("display_name"),
                        ProgramId = program.Id,
                        IntakeId = intakeId,
                        CreatedAtUtc = now,
                    });
                    return RowAction.Create;
                }

                existing.DisplayName = Map("display_name");
                existing.ProgramId = program.Id;
                existing.IntakeId = intakeId;
                return RowAction.Update;
            }
            case ImportEntities.Programs:
            {
                var code = Map("code");
                var existing = await db.Programs.FirstOrDefaultAsync(p => p.Code == code, ct);
                if (existing is null)
                {
                    db.Programs.Add(new Program
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        Code = code,
                        Name = Map("name"),
                        ExternalId = string.IsNullOrWhiteSpace(Map("external_id")) ? null : Map("external_id"),
                        CreatedAtUtc = now,
                    });
                    return RowAction.Create;
                }

                existing.Name = Map("name");
                if (!string.IsNullOrWhiteSpace(Map("external_id")))
                    existing.ExternalId = Map("external_id");
                return RowAction.Update;
            }
            case ImportEntities.Guardians:
            {
                var externalId = Map("external_id");
                var studentExt = Map("student_external_id");
                var student = await db.Students.FirstOrDefaultAsync(s => s.ExternalId == studentExt, ct);
                if (student is null) return RowAction.Skip;

                var existing = await db.Guardians.FirstOrDefaultAsync(g => g.ExternalId == externalId, ct);
                Guardian guardian;
                RowAction action;
                if (existing is null)
                {
                    guardian = new Guardian
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        ExternalId = externalId,
                        DisplayName = Map("display_name"),
                        MobileE164 = Map("mobile_e164"),
                        CreatedAtUtc = now,
                    };
                    db.Guardians.Add(guardian);
                    action = RowAction.Create;
                }
                else
                {
                    existing.DisplayName = Map("display_name");
                    existing.MobileE164 = Map("mobile_e164");
                    guardian = existing;
                    action = RowAction.Update;
                }

                var linkExists = await db.StudentGuardians.AnyAsync(
                    sg => sg.StudentId == student.Id && sg.GuardianId == guardian.Id, ct);
                if (!linkExists)
                {
                    db.StudentGuardians.Add(new StudentGuardian
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        StudentId = student.Id,
                        GuardianId = guardian.Id,
                        Relation = string.IsNullOrWhiteSpace(Map("relation")) ? "Parent" : Map("relation"),
                        Verified = false,
                    });
                }

                return action;
            }
            case ImportEntities.Enrolments:
            {
                var studentExt = Map("student_external_id");
                var courseCode = Map("course_code");
                var termCode = Map("term_code");
                var student = await db.Students.FirstOrDefaultAsync(s => s.ExternalId == studentExt, ct);
                var course = await db.Courses.FirstOrDefaultAsync(c => c.Code == courseCode, ct);
                var term = await db.Terms.FirstOrDefaultAsync(t => t.Code == termCode, ct);
                if (student is null || course is null || term is null) return RowAction.Skip;

                if (await db.Enrolments.AnyAsync(e =>
                        e.StudentId == student.Id && e.CourseId == course.Id && e.TermId == term.Id, ct))
                    return RowAction.Skip;

                var isRepeat = bool.TryParse(Map("is_repeat"), out var r) && r;
                db.Enrolments.Add(new Enrolment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    StudentId = student.Id,
                    CourseId = course.Id,
                    TermId = term.Id,
                    IsRepeat = isRepeat,
                    Status = EnrolmentStatuses.Enrolled,
                    AddedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    CreatedAtUtc = now,
                });
                return RowAction.Create;
            }
            default:
                return RowAction.Skip;
        }
    }

    private async Task<bool> ExistsEnrolmentAsync(string studentExt, string courseCode, string termCode, CancellationToken ct)
    {
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.ExternalId == studentExt, ct);
        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Code == courseCode, ct);
        var term = await db.Terms.AsNoTracking().FirstOrDefaultAsync(t => t.Code == termCode, ct);
        if (student is null || course is null || term is null) return false;
        return await db.Enrolments.AnyAsync(e =>
            e.StudentId == student.Id && e.CourseId == course.Id && e.TermId == term.Id, ct);
    }

    private static ImportJobDto ToDto(ImportJob job)
    {
        Dictionary<string, string>? mapping = null;
        if (!string.IsNullOrWhiteSpace(job.ColumnMappingJson))
            mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(job.ColumnMappingJson, JsonOptions);

        return new ImportJobDto(
            job.Id,
            job.Entity,
            job.Status,
            job.OriginalFileName,
            job.CreatedCount,
            job.UpdatedCount,
            job.SkippedCount,
            job.ErrorCount,
            mapping);
    }

    private enum RowAction { Create, Update, Skip }
}
