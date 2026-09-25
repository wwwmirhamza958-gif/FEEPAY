namespace FeePlatform.Modules.Audit;

/// <summary>Append-only audit event. Never update or delete rows in application code.</summary>
public sealed class AuditEvent
{
    public long Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public Guid? ActorId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? TraceId { get; set; }
}

public interface IAuditWriter
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
