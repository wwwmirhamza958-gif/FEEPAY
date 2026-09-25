using FeePlatform.Modules.Audit;
using FeePlatform.Infrastructure.Persistence;

namespace FeePlatform.Infrastructure.Audit;

public sealed class AuditWriterStub(AppDbContext db) : IAuditWriter
{
    public async Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        // Append-only: never update existing rows.
        if (auditEvent.OccurredAtUtc == default)
        {
            auditEvent.OccurredAtUtc = DateTimeOffset.UtcNow;
        }

        db.AuditEvents.Add(auditEvent);
        await db.SaveChangesAsync(cancellationToken);
    }
}
