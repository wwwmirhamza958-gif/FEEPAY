using FeePlatform.Modules.IdentityTenancy;

namespace FeePlatform.Modules.Integrations;

/// <summary>CONTRACT §2.4 — do not invent synonyms.</summary>
public static class ImportJobStatuses
{
    public const string Uploaded = "Uploaded";
    public const string Validating = "Validating";
    public const string Validated = "Validated";
    public const string FailedValidation = "FailedValidation";
    public const string Committing = "Committing";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Reverted = "Reverted";
}

public static class ImportEntities
{
    public const string Students = "students";
    public const string Enrolments = "enrolments";
    public const string Guardians = "guardians";
    public const string Programs = "programs";

    public static readonly IReadOnlyList<string> All = [Students, Enrolments, Guardians, Programs];
}

public sealed class ImportJob : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string Status { get; set; } = ImportJobStatuses.Uploaded;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "text/csv";
    /// <summary>Raw CSV/text payload stored for dry-run/commit (P1 in-DB; object storage later).</summary>
    public string RawContent { get; set; } = string.Empty;
    public string? ColumnMappingJson { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ValidatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public List<ImportRowError> RowErrors { get; set; } = [];
}

public sealed class ImportRowError : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ImportJobId { get; set; }
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class FieldMapping : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string MappingJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ConnectorConfig : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string ConfigJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SyncRun : ITenantScoped
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConnectorConfigId { get; set; }
    public string Direction { get; set; } = "Pull";
    public string Entity { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
}
